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
using System.IO;
using System.Diagnostics;

namespace ClienteGraficoServidor
{
    public partial class Form2Modal : Form
    {
        Form1 formClient;

        private string configFilePath = Path.Combine(Environment.GetEnvironmentVariable("userprofile"), "config.txt");

        public Form2Modal(Form1 form)
        {
            InitializeComponent();
            this.formClient = form;
            txtBoxIP.Text = formClient.ipServer;
            txtBoxPort.Text = formClient.port.ToString();

        }

        public bool CheckIpPort(string ip, int port)
        {
            //rango valido de puertos es 1024 para arriba

            if (port < 1024 || port > 65536)
            {
                lblPort.Text = "Not valid Port";
                lblPort.ForeColor = Color.Red;

                return false;
            }
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ip);

                return true;
            }
            catch (Exception e) when (e is SocketException || e is FormatException)
            {
                lblPort.Text = e.Message;
                lblIP.ForeColor = Color.Red;

                return false;
            }

        }

        private void SaveConfiguration()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(configFilePath))
                {
                    writer.Write($"{txtBoxIP.Text}:{txtBoxPort.Text}:{formClient.txtBoxUser.Text}");
                }
            }
            catch (Exception e) when (e is IOException | e is ArgumentException)
            {
                Debug.Print($"Error saving configuration: {e.Message}");

            }

        }
        private void btnAccept_Click(object sender, EventArgs e)
        {
            lblIP.Text = "";
            lblPort.Text = "";

            if (!int.TryParse(txtBoxPort.Text, out int port))
            {
                lblPort.Text = "Invalid Port: Must be a number";
                lblPort.ForeColor = Color.Red;

                return;
            }

            if (CheckIpPort(txtBoxIP.Text, port))
            {
                formClient.ipServer = txtBoxIP.Text;
                formClient.port = port;
                lblIP.ForeColor = Color.Green;
                lblIP.Text = "Server updated successfully!";
                SaveConfiguration();
                this.DialogResult = DialogResult.OK; // Indicar que la configuración fue guardada
                this.Close();
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
