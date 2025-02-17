using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ejercicio4Servidor
{
    internal class ShiftServer
    {
        string[] users;
        List<string> waitQueue = new List<string>();// rEVISAR PUERTO OCUPADO. Admon se queda conectado. isadmin noppuede ser global. No puede haber usuarios repetidos en wait. Elinia de mas en la cola 
        int port;
        const int portFinal = 65535;
        const int portInitial = 49664;
        Socket serverSocket;
        Socket clientSocket;
        int pinUser = 1;
        int pinPassword = 1;


        string[] commandParts;
        string timeConnection;


        public void Init()
        {
            //1ºComprobar puertos
            port = GetPortAvailable();
            if (port == -1)
            {
                Console.WriteLine("No available ports found. Exiting.");
                return;
            }
            Console.WriteLine($"Using port: {port}");
            ReadNames("usuarios.txt");

            //2ºcreo la ip-puerto
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, port);
            //3ºcreo sockey
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                //4ºenlanzar el socket
                serverSocket.Bind(ie);
                //5ºse queda en la escuccha
                serverSocket.Listen(5); //pongo en espera a 5 clientes como max
                Console.WriteLine("Server waiting");
                while (true)
                {
                    clientSocket = serverSocket.Accept();
                    Console.WriteLine($"Client connected from {clientSocket.RemoteEndPoint}: {port}");

                    Thread clientThread = new Thread(() => handlerClient(clientSocket));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
            }
            catch (Exception e) when (e is IOException | e is SocketException | e is ArgumentException)
            {
                Console.WriteLine(e.Message);
            }

        }

        public void handlerClient(object socket)
        {
            //1º Socket, coger la ip:Port
            Socket clientSocket = socket as Socket;
            IPEndPoint ieClient = clientSocket.RemoteEndPoint as IPEndPoint;
            bool isAdmin = false;
            string command = null;
            string username = "";


            try
            {
                //INTERMEDIARIO network
                using (NetworkStream ns = new NetworkStream(clientSocket))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    isAdmin = false;
                    sw.WriteLine("Welcome to Nicky's Server\r\nIntroduce your username :D");
                    sw.Flush();
                    //pido el nombre de usuario
                    username = sr.ReadLine();

                    if (string.IsNullOrEmpty(username) || !CheckInList(username, ref isAdmin))
                    {
                        sw.WriteLine("Unknown user disconnecting");
                        sw.Flush();
                        Console.WriteLine("unkown user disconnecting...");
                        clientSocket.Close();
                    }

                    //si es admin , se le pide un pin 
                    timeConnection = DateTime.Now.ToString("HH:mm:ss");
                    if (isAdmin)
                    {
                        sw.WriteLine("Enter PIN:");
                        if (!int.TryParse(sr.ReadLine(), out int pinUser))
                        {
                            sw.WriteLine("Invalid PIN format, disconnecting.");
                            sw.Flush();
                            clientSocket.Close();
                            return;
                        }
                        int pinPassword = ReadPin("pin.bin");
                        if (pinPassword == -1)
                        {
                            pinPassword = 1234;
                        }
                        if (pinUser != pinPassword)
                        {
                            sw.WriteLine("Wrong PIN, disconnecting.");
                            clientSocket.Close();
                            return;
                        }
                    }

                    command = sr.ReadLine();
                    while (isAdmin)
                    {
                        if (command != null)
                        {
                            //para que no sea null
                            commandParts = command.Split(' ');
                            switch (command)
                            {
                                case "list":
                                    //listado de alumnos en espera 
                                    sw.WriteLine($"Wait Queue:");
                                    sw.Flush();
                                    lock (this)
                                    {
                                        foreach (string student in waitQueue)
                                        {
                                            sw.WriteLine($"-{student}");
                                            sw.Flush();
                                        }
                                    }
                                    break;
                                case "add":
                                    //añade al usuario actual al final de la lista waitQueue
                                    lock (this)
                                    {
                                        if (!waitQueue.Any(userWait => userWait.StartsWith(username + "-")))
                                        {
                                            StringBuilder userQueue = new StringBuilder();
                                            userQueue.Append($"{username}-{DateTime.Now}, {timeConnection}");

                                            waitQueue.Add(userQueue.ToString());

                                            sw.WriteLine("OK user added");
                                            sw.Flush();
                                        }
                                        else
                                        {
                                            sw.WriteLine("user already exist");
                                            sw.Flush();
                                        }
                                    }
                                    break;
                                // ponemos string _ porque es una varibale auxiliar como haycemos con el tryparse 
                                case string _ when isAdmin && commandParts.Length == 2 && command.StartsWith("del"):
                                    int pos;
                                    lock (this)
                                    {
                                        if (int.TryParse(commandParts[1], out pos) && pos < waitQueue.Count && pos > 0)
                                        {
                                            waitQueue.RemoveAt(pos);
                                            sw.WriteLine("user deleted");
                                            sw.Flush();
                                        }
                                        else
                                        {
                                            sw.WriteLine("delete error");
                                            sw.Flush();
                                        }
                                    }
                                    break;
                                case string _ when isAdmin && commandParts.Length == 2 && command.StartsWith("chpin"):
                                    if (!int.TryParse(commandParts[1], out pinUser) || !SavePin("pin.bin", pinUser))
                                    {
                                        sw.WriteLine("error saving PIN");
                                        sw.Flush();
                                    }
                                    else
                                    {
                                        sw.WriteLine("PIN saved");
                                        sw.Flush();
                                    }
                                    break;
                                case "exit" when isAdmin:
                                    isAdmin = false;
                                    clientSocket.Close();
                                    break;

                                case "shutdown" when isAdmin:
                                    SaveWaitQueue("waitQueue.txt");
                                    clientSocket.Close();
                                    serverSocket.Close();
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

            }
            catch (Exception e) when (e is SocketException | e is IOException | e is ArgumentException)
            {
                Console.WriteLine(e.Message);
                clientSocket.Close();
            }
        }

        public void ReadNames(string pathFile)
        {
            //lee un archivo de texto
            //nombres separados por . y ;
            string lines = "";
            if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("userprofile"), pathFile)))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(Path.Combine(Environment.GetEnvironmentVariable("userprofile"), pathFile)))
                    {
                        lines = reader.ReadToEnd();
                        //con la sobrecarga puedo mandarle un aray, con el ptro booroo los lyugares vacios
                        users = lines.Split(new char[] { '.', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(user => user.Trim()).ToArray();
                    }
                }
                catch (Exception e) when (e is IOException | e is ArgumentException)
                {
                    Console.WriteLine(e.Message);
                    users = new string[0]; // pomgo esto?¿ para evitar el null
                }
            }
            else
            {
                Console.WriteLine("User file not found.");
                users = new string[0]; // pomgo esto?¿ para evitar el null
            }
        }

        public int ReadPin(string pathFile)
        {
            string fullPath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), pathFile);
            if (!File.Exists(fullPath))
            {
                return -1;
            }

            try
            {
                using (BinaryReader reader = new BinaryReader(new FileStream(fullPath, FileMode.Open)))
                {
                    if (reader.BaseStream.Length < 4)
                    {
                        return -1;
                    }
                    int pin = reader.ReadInt32();
                    return (pin >= 0 && pin <= 9999) ? pin : -1;
                }
            }
            catch (Exception e) when (e is IOException | e is ArgumentException)
            {
                Console.WriteLine($"Error reading PIN: {e.Message}");
                return -1;
            }
        }

        public int GetPortAvailable()
        {

            for (int i = portInitial; i < portFinal; i++)
            {
                if (CheckPort(i))
                {
                    return i;
                }
            }
            return -1;
        }
        public bool CheckPort(int port)
        {
            try
            {
                using (Socket testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    //intento conectar al puerto 
                    testSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                    return true;
                }
            }
            catch (Exception e) when (e is SocketException | e is IOException)
            {
                return false;
            }

        }

        public bool CheckInList(string userConnected, ref bool isAdmin)
        {
            if (userConnected == "admin")
            {
                isAdmin = true;
                return true;
            }

            return users.Contains(userConnected);
        }

        public bool SavePin(string pathFile, int pinSave)
        {
            if (pinSave < 0 || pinSave > 9999) // pin 4 difitods
            {
                return false;
            }
            try
            {
                using (BinaryWriter writer = new BinaryWriter(new FileStream(Path.Combine(Environment.GetEnvironmentVariable("userprofile"), pathFile), FileMode.Create)))
                {
                    writer.Write(pinSave);
                }
                Console.WriteLine($"[DEBUG] PIN guardado correctamente: {pinSave}");

                return true;
            }
            catch (Exception e) when (e is IOException | e is ArgumentException)
            {
                Console.WriteLine($"Error saving PIN: {e.Message}");

                return false;
            }
        }
        public void SaveWaitQueue(string pathFile)
        {
            string fullPath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), pathFile);

            try
            {
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    foreach (string user in waitQueue)
                    {
                        writer.WriteLine(user);  // guardo cada usuyarui en una linea
                    }
                }
                Console.WriteLine("[DEBUG] Lista de espera guardada correctamente.");
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                Console.WriteLine($"Error al guardar la lista de espera: {e.Message}");
            }
        }

        public void LoadWaitQueue(string pathFile)
        {
            string fullPath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), pathFile);

            if (!File.Exists(fullPath))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(fullPath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            waitQueue.Add(line);
                        }
                    }
                }
                Console.WriteLine("[DEBUG] Lista de espera cargada correctamente.");
            }
            catch (Exception e) when (e is IOException || e is ArgumentException)
            {
                Console.WriteLine($"Error al cargar la lista de espera: {e.Message}");
            }
        }

    }
}
