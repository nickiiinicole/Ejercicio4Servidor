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
        const int defaultPort = 31416;
        const int portFinal = 65535;
        const int portInitial = 1024;
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
            LoadWaitQueue("waitQueue.txt");
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

            bool isExit = false;
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

                    //revisar el ref de isAdmin 
                    if (string.IsNullOrEmpty(username) || !CheckInList(username, ref isAdmin))
                    {
                        sw.WriteLine("Unknown user disconnecting");
                        sw.Flush();
                        Console.WriteLine("unkown user disconnecting...");
                        clientSocket.Close();
                        return;
                    }



                    //si es admin , se le pide un pin 
                    timeConnection = DateTime.Now.ToString("HH:mm:ss");
                    // Si es admin, solicitar PIN
                    if (isAdmin)
                    {
                        sw.WriteLine("Enter PIN:");
                        sw.Flush();
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
                            sw.Flush();
                            clientSocket.Close();
                            return;
                        }
                    }
                    timeConnection = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    // Procesar comandos mediante switch
                    if (isAdmin)
                    {
                        bool exitAdmin = false;
                        while (!exitAdmin)
                        {
                            sw.WriteLine("Enter command: (add,list,del pos, chpin pin, exit, shutdown)");
                            sw.Flush();
                            string command = sr.ReadLine();
                            if (string.IsNullOrEmpty(command))
                            {
                                clientSocket.Close();
                                return;
                            }
                            string[] commandParts = command.Split(' ');

                            switch (command)
                            {
                                case "list":
                                    sw.WriteLine("Wait Queue:");
                                    sw.Flush();
                                    lock (this)
                                    {
                                        for (int i = 0; i < waitQueue.Count; i++)
                                        {
                                            sw.WriteLine($"{i} - {waitQueue[i]}");
                                            sw.Flush();
                                        }
                                    }
                                    break;

                                case "add":
                                    lock (this)
                                    {
                                        if (!waitQueue.Any(item => item.StartsWith(username + "-")))
                                        {
                                            string entry = $"{username}-{timeConnection}";
                                            waitQueue.Add(entry);
                                            sw.WriteLine("OK");
                                            sw.Flush();
                                        }
                                        else
                                        {
                                            sw.WriteLine("User already exists in the wait queue.");
                                            sw.Flush();
                                        }
                                    }
                                    break;

                                // pattern matching en el case para comandos con parámetros, como del tryparse
                                case string s when s.StartsWith("del"):
                                    if (commandParts.Length == 2 && int.TryParse(commandParts[1], out int pos))
                                    {
                                        lock (this)
                                        {
                                            if (pos >= 0 && pos < waitQueue.Count)
                                            {
                                                waitQueue.RemoveAt(pos);
                                                sw.WriteLine("User deleted.");
                                                sw.Flush();
                                            }
                                            else
                                            {
                                                sw.WriteLine("delete error");
                                                sw.Flush();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        sw.WriteLine("delete error");
                                        sw.Flush();
                                    }
                                    break;

                                case string s when s.StartsWith("chpin"):
                                    if (commandParts.Length == 2 &&
                                        int.TryParse(commandParts[1], out int newPin) && newPin >= 1000)
                                    {
                                        if (SavePin("pin.bin", newPin))
                                        {
                                            sw.WriteLine("PIN saved.");
                                            sw.Flush();
                                        }
                                        else
                                        {
                                            sw.WriteLine("error saving PIN");
                                            sw.Flush();
                                        }
                                    }
                                    else
                                    {
                                        sw.WriteLine("error saving PIN");
                                        sw.Flush();
                                    }
                                    break;

                                case "exit":
                                    sw.WriteLine("Disconnecting admin.");
                                    sw.Flush();
                                    exitAdmin = true;
                                    break;

                                case "shutdown":
                                    SaveWaitQueue("waitQueue.txt");
                                    sw.WriteLine("Shutting down server.");
                                    sw.Flush();
                                    clientSocket.Close();
                                    serverSocket.Close();
                                    return;

                                default:
                                    sw.WriteLine("Unknown command.");
                                    sw.Flush();
                                    break;
                            }
                        }
                    }
                    else // Usuario normal
                    {
                        sw.WriteLine("Enter command:");
                        string command = sr.ReadLine();
                        switch (command)
                        {
                            case "list":
                                sw.WriteLine("Wait Queue:");
                                lock (this)
                                {
                                    for (int i = 0; i < waitQueue.Count; i++)
                                    {
                                        sw.WriteLine($"{i} - {waitQueue[i]}");
                                        sw.Flush();
                                    }
                                }
                                break;

                            case "add":
                                lock (this)
                                {
                                    if (!waitQueue.Any(item => item.StartsWith(username + "-")))
                                    {
                                        string entry = $"{username}-{timeConnection}";
                                        waitQueue.Add(entry);
                                        sw.WriteLine("OK");
                                        sw.Flush();
                                    }
                                    else
                                    {
                                        sw.WriteLine("User already exists in the wait queue.");
                                        sw.Flush();
                                    }
                                }
                                break;

                            default:
                                sw.WriteLine("Unknown command.");
                                sw.Flush();
                                break;
                        }
                    }
                }
                clientSocket.Close();

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
            if (CheckPort(defaultPort))
            {
                return defaultPort;
            }

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
