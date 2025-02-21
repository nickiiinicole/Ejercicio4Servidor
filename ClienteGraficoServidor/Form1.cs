﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClienteGraficoServidor
{
    public partial class Form1 : Form
    {
        public int port = 31416;
        public string ipServer = "127.0.0.1";
        public Form1()
        {
            InitializeComponent();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (Form2Modal form2 = new Form2Modal(this))
            {
                form2.ShowDialog();
            }

        }

        public void SendCommandServer(string ipServer, int port, string command)
        {


            //indicar servido al que nos queremes conectar

            IPEndPoint ie = new IPEndPoint(IPAddress.Parse(ipServer), port);
            Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                server.Connect(ie);
                // Información del servidor
                IPEndPoint ieServer = (IPEndPoint)server.RemoteEndPoint;
                Console.WriteLine("Server on IP:{0} at port {1}", ieServer.Address, ieServer.Port);
                //si todo esta bien entoces se crean los stream
                using (NetworkStream ns = new NetworkStream(server))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    //mensaje de bienivendia
                    lblServer.Text = $"{sr.ReadLine()}";
                    //mandas el nombre del usaurio
                    sw.WriteLine(txtBoxUser.Text);
                    sw.Flush();
                    //leo respuesta verificar usyario
                    lblServer.Text = $"{sr.ReadLine()}";
                    //mando comando
                    sw.WriteLine(command);
                    sw.Flush();

                    //leo el mesnaje del comando 
               
                    //leo la respuesta de waitQueue
                    lblServer.Text = $"{sr.ReadToEnd()}";
                    //leo final 
                    
                }
            }
            catch (Exception e) when (e is IOException | e is SocketException | e is ArgumentException)
            {
                Console.WriteLine(e.Message);
            }


        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            lblServer.Text = "";
            Button btn = sender as Button;
            SendCommandServer(ipServer, port, btn.Text.ToLower());

        }
    }
}
