using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClienteGraficoServidor
{
    public partial class Form2Modal : Form
    {
        Form1 formClient;


        public Form2Modal(Form1 form)
        {
            InitializeComponent();
            this.formClient = form;

        }

        public bool CheckIpPort(string ip, int port)
        {
            if (port < 0 || port > 65536)
            {
                lblPort.Text = "Not valid Port";
                return false;
            }
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ip);
                IPEndPoint endPoint = new IPEndPoint(ipAddress, port);

                return true;
            }
            catch (Exception e) when (e is SocketException || e is FormatException)
            {
                lblPort.Text = e.Message;
                return false;
            }

        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            lblIP.Text = "";
            lblPort.Text = "";
            if (!int.TryParse(txtBoxPort.Text, out int port))
            {
                lblPort.Text = "Invalid Port: Must be a number";
                return;
            }

            if (CheckIpPort(txtBoxIP.Text, port))
            {
                formClient.ipServer = txtBoxIP.Text;
                formClient.port = port;
                lblIP.ForeColor = Color.Green;
                lblIP.Text = "Server updated successfully!";
            }
        }

        private void Form2Modal_Load(object sender, EventArgs e)
        {

        }

        private void lblIP_Click(object sender, EventArgs e)
        {

        }

        private void lblPort_Click(object sender, EventArgs e)
        {

        }
    }
}
