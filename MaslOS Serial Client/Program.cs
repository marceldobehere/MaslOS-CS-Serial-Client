using MaslOS_Serial_Client.Connectors;
using MaslOS_Serial_Client.PacketStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SerialPort serialPort = new SerialPort("COM4");

            serialPort.BaudRate = 115200;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;
            serialPort.RtsEnable = true;
            serialPort.WriteBufferSize = 1000000;
            serialPort.ReadBufferSize = 1000000;


            Console.WriteLine($"STATS:");
            Console.WriteLine($" - PORT: {serialPort.PortName}");
            Console.WriteLine($" - BAUD RATE: {serialPort.BaudRate}");
            Console.WriteLine($" - PARITY: {serialPort.Parity}");
            Console.WriteLine($" - STOP BITS: {serialPort.StopBits}");
            Console.WriteLine($" - DATA BITS: {serialPort.DataBits}");
            Console.WriteLine($" - RTS ENABLED: {serialPort.RtsEnable}");
            Console.WriteLine($" - READ BUFFER SIZE: {serialPort.ReadBufferSize}");
            Console.WriteLine($" - WRITE BUFFER SIZE: {serialPort.WriteBufferSize}");
            Console.WriteLine();

            Console.OutputEncoding = Encoding.Unicode;


            Console.WriteLine($"MaslOS Serial Client v0.1 (SIG: {PacketManager.SignatureString})");
            Console.WriteLine();


            bool serialEnabled = true;
            bool screenEnabled = false;
            bool tcpEnabled = true;

            Console.WriteLine("> Press Enter to connect");
            bool printRawData = false;
            string inputStr = Console.ReadLine();
            if (inputStr.StartsWith("<"))
            {
                screenEnabled = true;
                inputStr = inputStr.Substring(1);
            }
            if (inputStr.StartsWith("_"))
            {
                printRawData = true;
                inputStr = inputStr.Substring(1);
            }
            bool skip = inputStr.ToLower() == "skip";
            bool skip2 = inputStr.ToLower() == "skip2";
            skip |= skip2;

            Console.WriteLine("> Opening Port...");
            if (!skip2)
                serialPort.Open();

            if (!skip2 && !serialPort.IsOpen)
            {
                Console.WriteLine("> ERROR OPENING PORT");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("> Trying to connect to MaslOS...");

            {
                int tempSigIndex = 0;
                bool connected = skip;
                DateTime lastSend = DateTime.Now;

                while (!connected)
                {
                    if (DateTime.Now.Subtract(lastSend).TotalMilliseconds > 1500)
                    {
                        if (tempSigIndex == 0)
                        {
                            // SEND SIG
                            if (serialPort.BytesToWrite < 1)
                            {
                                Console.ForegroundColor = ConsoleColor.Blue; // OUT
                                Console.Write($"{PacketManager.SignatureString}");
                                serialPort.Write(PacketManager.Signature, 0, 3);
                            }
                        }
                        lastSend = DateTime.Now;
                    }

                    for (int t = 0; t < 20 && Console.KeyAvailable; t++)
                    {
                        var info = Console.ReadKey(true);
                        serialPort.Write(info.KeyChar.ToString());
                        Console.ForegroundColor = ConsoleColor.Green; // USR
                        Console.Write(info.KeyChar);
                        lastSend = DateTime.Now;
                    }

                    for (int t = 0; t < 20 && serialPort.BytesToRead > 0; t++)
                    {
                        char chr = (char)serialPort.ReadByte();
                        Console.ForegroundColor = ConsoleColor.Red; // IN
                        Console.Write(chr);
                        if (chr == PacketManager.Signature[tempSigIndex])
                        {
                            tempSigIndex++;
                            if (tempSigIndex == PacketManager.Signature.Length)
                            {
                                connected = true;
                                break;
                            }
                        }
                        else
                        {
                            tempSigIndex = 0;
                            lastSend = DateTime.Now;
                        }
                    }



                }
            }

            Console.ForegroundColor = ConsoleColor.White; // DEF
            Console.WriteLine("\n> Connected!");


            Console.WriteLine("> Starting Packet Manager");
            PacketManager manager = new PacketManager();


            Console.WriteLine("> Initializing things");


            CustomSerialConnector serialConnector = null;
            CustomScreenConnector screenConnector = null;
            CustomTcpConnector tcpConnector = null;


            if (serialEnabled)
            {
                Console.WriteLine($"> Creating Serial Port Monitor");
                serialConnector = new CustomSerialConnector("SERIAL PORT MONITOR");
                manager.SendPacket(SerialPacketStuff.CreateSerialEnablePacket());
            }
            else
                manager.SendPacket(SerialPacketStuff.CreateSerialDisablePacket());


            if (screenEnabled)
            {
                Console.WriteLine($"> Creating Screen Monitor");
                screenConnector = new CustomScreenConnector();
                manager.SendPacket(ScreenPacketStuff.CreateScreenEnablePacket());
            }
            else
                manager.SendPacket(ScreenPacketStuff.CreateScreenDisablePacket());

            if (tcpEnabled)
            {
                Console.WriteLine($"> Creating TCP \"Proxy\"");
                tcpConnector = new CustomTcpConnector();
                manager.SendPacket(TcpPacketStuff.CreateTcpEnablePacket());
            }
            else
                manager.SendPacket(TcpPacketStuff.CreateTcpDisablePacket());

            {
                GenericPacket packet = new GenericPacket(
                     PacketType.INIT,
                     (ushort)Ports.ReservedOutClientPortsEnum.InitClient,
                     (ushort)Ports.ReservedHostPortsEnum.InitHost,
                     1,
                     new byte[1] { 0 } // INIT REQUEST
                 );

                manager.SendPacket(packet);
            }


            Console.WriteLine($"> Client started!");
            Console.Write($"(");
            Console.ForegroundColor = ConsoleColor.Red; // IN
            Console.Write($"IN ");
            Console.ForegroundColor = ConsoleColor.Blue; // OUT
            Console.Write($"OUT ");
            Console.ForegroundColor = ConsoleColor.Green; // OUT
            Console.Write($"USR");
            Console.ForegroundColor = ConsoleColor.White; // DEF
            Console.WriteLine($")");
            Console.WriteLine($"");
            bool exit = false;
            Stopwatch bpsWatch = new Stopwatch();
            bpsWatch.Start();
            Stopwatch textWatch = new Stopwatch();
            textWatch.Start();
            int lastSum = 1;
            int bytesSent = 0;
            int bytesReceived = 0;
            int totalBytesSent = 0;
            int totalBytesReceived = 0;
            

            while (!exit || manager.PacketsToBeSent.Count > 0 || manager.CurrentSendPacket != null|| manager.BytesToSend.Count > 0)
            {
                if (!skip2)
                    for (int i = 0; i < 100 && serialPort.BytesToRead > 0; i++)
                    {
                        byte read = (byte)serialPort.ReadByte();
                        if (printRawData)
                        {
                            Console.ForegroundColor = ConsoleColor.Red; // IN
                            Console.Write((char)read);
                        }
                        manager.BytesReceived.Enqueue(read);
                        bytesReceived++;
                    }

                manager.DoStep();

                if (!skip2)
                {
                    for (int i = 0; i < 100 && manager.BytesToSend.Count > 0 && serialPort.BytesToWrite < serialPort.WriteBufferSize; i++)
                    {
                        // write 1 byte at a time and write to console
                        byte send = manager.BytesToSend.Dequeue();

                        serialPort.Write(new byte[1] { send }, 0, 1);
                        if (printRawData)
                        {
                            Console.ForegroundColor = ConsoleColor.Blue; // OUT
                            Console.Write((char)send);
                        }

                        bytesSent++;
                    }
                }
                else
                {
                    for (int i = 0; i < 20 && manager.BytesToSend.Count > 0; i++)
                    {
                        // write 1 byte at a time and write to console
                        byte send = manager.BytesToSend.Dequeue();

                        //serialPort.Write(new byte[1] { send }, 0, 1);
                        if (printRawData)
                        {
                            Console.ForegroundColor = ConsoleColor.Blue; // OUT
                            Console.Write((char)send);
                        }

                        bytesSent++;
                    }
                }

                if (textWatch.ElapsedMilliseconds > 100)
                {
                    if (Console.KeyAvailable)
                    {
                        var info = Console.ReadKey(true);
                        char c = info.KeyChar;
                        if (c == 'x' && info.Modifiers == ConsoleModifiers.Alt)
                        {
                            manager.SendPacket(SerialPacketStuff.CreateSerialDisablePacket());
                            manager.SendPacket(ScreenPacketStuff.CreateScreenDisablePacket());
                            manager.InitPacketSent = false;
                            exit = true;
                        }
                        else
                        {
                            if (!skip2)
                            {
                                serialPort.Write(c.ToString());
                                Console.ForegroundColor = ConsoleColor.Green; // USR
                                Console.Write(c);
                            }
                            else
                            {
                                if (printRawData)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red; // IN
                                    Console.Write(c);
                                }
                                manager.BytesReceived.Enqueue((byte)c);
                                bytesReceived++;
                            }
                        }
                    }
                    else
                        textWatch.Restart();
                }
                
                if (serialConnector != null && manager.HasPacket((ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient))
                {
                    GenericPacket packet = manager.GetPacket((ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient);
                    string text = Encoding.ASCII.GetString(packet.Data);
                    serialConnector.Write(text);
                }

                if (serialConnector != null && serialConnector.CanRead)
                {
                    string temp = serialConnector.ReadAll();
                    manager.SendPacket(SerialPacketStuff.CreateSerialPacketToMaslOS(temp));
                }

                if (screenConnector != null && manager.HasPacket((ushort)Ports.ReservedOutClientPortsEnum.VideoClient))
                {
                    GenericPacket packet = manager.GetPacket((ushort)Ports.ReservedOutClientPortsEnum.VideoClient);
                    screenConnector.HandleScreenUpdate(packet);
                }

                if (tcpConnector != null && manager.HasPacket((ushort)Ports.ReservedOutClientPortsEnum.TCPClient))
                {
                    GenericPacket packet = manager.GetPacket((ushort)Ports.ReservedOutClientPortsEnum.TCPClient);
                    tcpConnector.HandlePacket(packet);
                }

                if (tcpConnector != null && tcpConnector.ToSend.Count > 0)
                {
                    GenericPacket packet = tcpConnector.ToSend.Dequeue();
                    manager.SendPacket(packet);
                }

                if (!skip2)
                    if (bpsWatch.ElapsedMilliseconds >= 1000)
                    {
                        bpsWatch.Restart();
                        totalBytesReceived += bytesReceived;
                        totalBytesSent += bytesSent;
                        int sum = bytesSent + bytesReceived;


                        if (sum != 0 || lastSum != 0)
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"<{bytesSent}/{bytesReceived}  ({totalBytesSent/1024} KB/{totalBytesReceived/1024} KB)>");
                        }

                        lastSum = sum;
                        bytesSent = 0;
                        bytesReceived = 0;
                    }
            }

            if (serialConnector != null)
                serialConnector.Running = false;
            if (screenConnector != null)
                screenConnector.Running = false;

            Console.ForegroundColor = ConsoleColor.White; // DEF
            Console.WriteLine("\n\nEnd.");
            Console.ReadLine();

            System.Environment.Exit(0);
        }
    }
}
