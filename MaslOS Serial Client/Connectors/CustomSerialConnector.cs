using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client
{
    public class CustomSerialConnector
    {
        public static Random Rnd = new Random();

        public Process Proc;
        public TcpListener Server;
        public Socket Client;
        public int Port;
        public bool Running;

        public List<(char cmd, char data)> ToSend;
        public List<(char cmd, char data)> Received;


        public CustomSerialConnector(string msg)
        {
            ToSend = new List<(char cmd, char data)>();
            Received = new List<(char cmd, char data)>();

            Port = Rnd.Next(3500, 3600);
            Server = new TcpListener(IPAddress.Any, Port);
            Server.Start();

            Console.WriteLine($"<STARTING SERIAL SERVER AT localhost:{Port}>");
            Proc = Process.Start(AppContext.BaseDirectory + "/mini serial terminal ig.exe", $"{Port}");
            Client = Server.AcceptSocket();
            Running = true;

            new Thread(() => { SendLoop(); }).Start();
            new Thread(() => { ReceiveLoop(); }).Start();

            Writeln(msg);
            Writeln();
        }

        public void HandleCommand(char cmd, char data)
        {
            if (cmd == 'C')
            {
                if (data == 'X')
                    InternalClose();
            }
        }

        public bool CanRead
        {
            get
            {
                bool canRead;
                lock (Received)
                {
                    canRead = Received.Count > 0;
                }
                return canRead;
            }
        }

        public char Read()
        {
            if (!CanRead)
                return '\0';

            char temp;
            lock (Received)
            {
                temp = Received[0].data;
                Received.RemoveAt(0);
            }
            return temp;
        }

        public string ReadAll()
        {
            string temp = "";
            while (CanRead)
                temp += Read();
            return temp;
        }

        public void Writeln()
        {
            Write($"\r\n");
        }

        public void Writeln(string str)
        {
            Write(str);
            Writeln();
        }

        public void Write(string str)
        {
            foreach (char chr in str)
                Write('D', chr);
        }

        public void Write(char chr)
        {
            Write('D', chr);
        }

        public void Write(char cmd, char chr)
        {
            lock (ToSend)
            {
                ToSend.Add((cmd, chr));
            }
        }

        public void Close()
        {
            if (!Running)
                return;

            ToSend.Clear();
            Write('C', 'X');
            while (ToSend.Count > 0)
                ;
        }

        private void InternalClose()
        {
            try
            {
                Running = false;
                Client.Close();
                Server.Stop();
                Proc.Kill();
            }
            catch (Exception e)
            {

            }
        }



        public void SendLoop()
        {
            byte[] sendBuff = new byte[2];
            int sendBuffIndex = 2;

            while (Running)
            {
                try
                {
                    if (sendBuffIndex == 2)
                    {
                        lock (ToSend)
                        {
                            if (ToSend.Count > 0)
                            {
                                sendBuff[0] = (byte)ToSend[0].cmd;
                                sendBuff[1] = (byte)ToSend[0].data;
                                ToSend.RemoveAt(0);
                                sendBuffIndex = 0;
                            }
                        }
                    }
                    else
                    {
                        sendBuffIndex += Client.Send(sendBuff, sendBuffIndex, 2 - sendBuffIndex, SocketFlags.None);
                    }
                }
                catch (Exception e)
                {

                }

                if (!Client.Connected && Running)
                {
                    InternalClose();
                    return;
                }
            }
        }

        public void ReceiveLoop()
        {
            byte[] recBuff = new byte[2];
            int recBuffIndex = 0;

            while (Running)
            {
                try
                {
                    recBuffIndex += Client.Receive(recBuff, recBuffIndex, 2 - recBuffIndex, SocketFlags.None);
                    if (recBuffIndex == 2)
                    {
                        lock (Received)
                        {
                            recBuffIndex = 0;
                            char cmd = (char)recBuff[0];
                            char data = (char)recBuff[1];

                            if (cmd == 'D')
                                Received.Add((cmd, data));
                            else
                                HandleCommand(cmd, data);
                        }
                    }
                }
                catch (Exception e)
                {

                }

                if (!Client.Connected && Running)
                {
                    InternalClose();
                    return;
                }
            }
        }
    }
}
