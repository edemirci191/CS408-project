using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace _408ProjectStep1_client
{
    public partial class Form1 : Form
    {
        bool terminating = false;
        bool connected = false;
        Socket clientSocket;
        OpenFileDialog file;

        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);
            InitializeComponent();
        }

        private void Form1_FormClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Ensures that when form is closed program terminates without crashing
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_connect_Click(object sender, EventArgs e)
        {
            // Assign a new socket to client
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            string IP = textBox_ip.Text;

            int portNum;
            if (Int32.TryParse(textBox_port.Text, out portNum))
            {
                try
                {
                    // Try to connect to ip and port from by gui
                    clientSocket.Connect(IP, portNum);

                    // After connecting send username to server
                    if(textBox_username.Text == "")
                    {
                        // Check if a username is entered if not display a message
                        logs.AppendText("Please enter username!\n");
                    }

                    else
                    {
                        // Send the username to connected server
                        Byte[] buffer = new Byte[64];
                        buffer = Encoding.Default.GetBytes(textBox_username.Text);
                        clientSocket.Send(buffer);
                    }

                    // Wait for the necessary operations at the server side
                    // Server checks if there is another client with the same username
                    System.Threading.Thread.Sleep(1000);

                    // If server doesn't close the socket at server side after getting the username
                    if (isConnected(clientSocket))
                    {
                        // It means that there was not any another client with the same username
                        connected = true;
                        logs.AppendText("Connected to the server!\n");
                        button_connect.Enabled = false;
                        button_browse.Enabled = true;

                        // Activate the thread that enables the client to recieve message from server
                        // This is used to tell if server is closed
                        Thread checkServerThread = new Thread(checkServer);
                        checkServerThread.Start();
                    }

                    else
                    {
                        // Display the necessary messages if a client with the same username exists at the server side
                        logs.AppendText("Already existing username, could not connect!\n");
                        logs.AppendText("Try again with another username.\n");
                    }
                }

                catch
                {
                    // If client can't connect
                    logs.AppendText("Could not connect to the server!\n");
                }
            }
            else
            {
                // If port number fom gui can't be parsed into an integer
                logs.AppendText("Check the port\n");
            }
        }

        private void button_browse_Click(object sender, EventArgs e)
        {
            // Enables user to choose the file that is going to be sended 
            // to the server through gui
            file = new OpenFileDialog();

            if(file.ShowDialog() == DialogResult.OK)
            {
                // If a file is selected display a message
                textBox_path.Text = file.FileName;
                logs.AppendText("File selected.\n");
                button_upload.Enabled = true;
            }
        }

        private void button_upload_Click(object sender, EventArgs e)
        {
            try
            { 
                // Get the file information 
                FileInfo info = new FileInfo(file.FileName);

                string fileName = info.Name;
                string filePath = file.FileName;

                // Create a client data which consists of file name, 
                // the length of file name and the file content
                Byte[] fileNameByte = Encoding.ASCII.GetBytes(fileName);
                Byte[] fileData = File.ReadAllBytes(filePath);
                Byte[] clientData = new Byte[4 + fileNameByte.Length + fileData.Length];
                Byte[] fileNameLen = BitConverter.GetBytes(fileNameByte.Length);

                fileNameLen.CopyTo(clientData, 0);
                fileNameByte.CopyTo(clientData, 4);
                fileData.CopyTo(clientData, 4 + fileNameByte.Length);

                // Send the client data to the server
                clientSocket.Send(clientData);

                // Display the necessary messages
                logs.AppendText("File is uploaded to server.\n");
                textBox_path.Text = "";
                button_upload.Enabled = false;
            }

            catch
            {
                // If an error occures while sending, display necessary message
                logs.AppendText("Couldn't upload the file!\n");
            }            
        }

        public static bool isConnected(Socket socket)
        {
            // This function tells if a socket is connected to the server's 
            // correcponding socket ie. if the socket is active
            try
            {
                bool part1 = socket.Poll(1000, SelectMode.SelectRead);
                bool part2 = (socket.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch (SocketException) { return false; }
        }

        private void checkServer()
        {
            // Another thread to check if server is closing
            bool connected = true;

            while (connected && !terminating)
            {
                // While connected and not terminating
                try
                {
                    // If the server is closed this will throw an error
                    Byte[] buffer = new Byte[64];
                    clientSocket.Receive(buffer);
                }
                catch
                {
                    // The error is caught here
                    if (!terminating)
                    {
                        // If client is not terminating this error means that server has closed
                        logs.AppendText("The server has closed.\n");
                    }
                    clientSocket.Close();
                    connected = false;
                    button_browse.Enabled = false;
                    button_connect.Enabled = true;
                }
            }
        }
    }
}
