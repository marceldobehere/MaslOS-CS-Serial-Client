using MaslOS_Serial_Client.PacketStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client.Connectors
{
    public class CustomTcpConnector
    {
        public Queue<GenericPacket> ToSend;
        public Dictionary<ushort, Socket> Connections;

        public CustomTcpConnector()
        {
            ToSend = new Queue<GenericPacket>();
            Connections = new Dictionary<ushort, Socket>();
        }

        public void HandlePacket(GenericPacket packet)
        {
            //Console.ForegroundColor = ConsoleColor.DarkCyan;
            //Console.WriteLine($"<TCP PACKET>");

            if (packet == null)
                return;

            if (packet.Type == PacketType.CMD)
            {
                HandleCmdPacket(packet);
                return;
            }

            if (!IsPacketValid(packet))
                return;

            HandleDataPacket(packet);
        }

        public void HandleDataPacket(GenericPacket packet)
        {
            if (packet.Data[PACKET_H_CS] == 1)
                throw new Exception("TCP SERVER NOT SUPPORTED YET");

            ushort port = BitConverter.ToUInt16(packet.Data, PACKET_H_PORT);

            if (!Connections.ContainsKey(port))
                return;

            Socket temp = Connections[port];

            byte[] data = new byte[packet.Len - PACKET_DATA];
            Array.Copy(packet.Data, PACKET_DATA, data, 0, data.Length);

            new Thread(() =>
            {
                SocketSendLoop(port, temp, data);
            }).Start();
        }

        public const int PACKET_H_CS = 0;
        public const int PACKET_H_PORT = 1;
        public const int PACKET_C_IP = 3;
        public const int PACKET_C_PORT = 7;
        public const int PACKET_LEN = 9;
        public const int PACKET_DATA = 13;


        public const int CMD_PACKET_TYPE = 0;
        public const int CMD_PACKET_PORT = 1;
        public const int CMD_PACKET_EXT_IP = 3;
        public const int CMD_PACKET_EXT_PORT = 7;
        public const int CMD_PACKET_DATA = 9;

        public const int CMD_PACKET_TYPE_CONNECTION = 10;

        public bool IsPacketValid(GenericPacket packet)
        {
            if (packet.Type != PacketType.DATA)
                return false;
            if (packet.Len <= 13)
                return false;
            if (packet.Data[PACKET_H_CS] != 0 && packet.Data[PACKET_H_CS] != 1)
                return false;

            return true;
        }

        public bool IsPacketForPort(GenericPacket packet, ushort port)
        {
            if (!IsPacketValid(packet))
                return false;

            return BitConverter.ToUInt16(packet.Data, PACKET_H_PORT) == port;
        }

        public void HandleCmdPacket(GenericPacket packet)
        {
            if (packet.Len < 9)
                return;

            if (packet.Data[CMD_PACKET_TYPE] == CMD_PACKET_TYPE_CONNECTION)
            {
                if (packet.Len < 10)
                    return;

                ushort port = BitConverter.ToUInt16(packet.Data, CMD_PACKET_PORT);
                bool wantToConnect = packet.Data[CMD_PACKET_DATA] != 0;

                uint ip = BitConverter.ToUInt32(packet.Data, CMD_PACKET_EXT_IP);
                ushort extPort = BitConverter.ToUInt16(packet.Data, CMD_PACKET_EXT_PORT);

                if (wantToConnect)
                    TryCreateConnection(port, PacketManager.SubArray(packet.Data, CMD_PACKET_EXT_IP), extPort);
                else
                    TryDisconnect(port);
            }
        }

        public void TryDisconnect(ushort port)
        {
            Socket temp;

            lock (Connections)
            {
                if (!Connections.ContainsKey(port))
                    return;
                temp = Connections[port];
                Connections.Remove(port);
            }

            new Thread(() =>
            {
                try
                {
                    temp.Disconnect(false);
                    temp.Close();
                }
                catch (Exception e)
                {
                }
            });

            lock (ToSend)
            {
                ToSend.Enqueue(TcpPacketStuff.CreateTcpConnectionPacket(port, false));
            }
        }

        public void TryCreateConnection(ushort port, byte[] ipArr, ushort extPort)
        {
            new Thread(() =>
            {
                try
                {
                    string ipStr = $"{ipArr[0]}.{ipArr[1]}.{ipArr[2]}.{ipArr[3]}";
                    Socket tempSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    tempSocket.Connect(IPAddress.Parse(ipStr), extPort);
                    Connections[port] = tempSocket;

                    lock (ToSend)
                    {
                        ToSend.Enqueue(TcpPacketStuff.CreateTcpConnectionPacket(port, true));
                    }

                    new Thread(() =>
                    {
                        SocketReceiveLoop(port, tempSocket);
                    }).Start();
                }
                catch (Exception e)
                {

                }
            }).Start();
        }

        public void SocketSendLoop(ushort port, Socket socket, byte[] data)
        {
            //int extIp;
            //ushort extPort;
            lock (Connections)
            {
                if (!Connections.ContainsKey(port))
                    return;
                //extIp = (int)((IPEndPoint)socket.RemoteEndPoint).Address.Address;
                //extPort = (ushort)((IPEndPoint)socket.RemoteEndPoint).Port;
            }

            try
            {
                int tIndex = 0;
                Stopwatch timeWatch = new Stopwatch();
                timeWatch.Start();

                while (true)
                {
                    lock (Connections)
                    {
                        if (!Connections.ContainsKey(port))
                            return;
                    }

                    try
                    {
                        lock (socket)
                        {
                            tIndex += socket.Send(data, tIndex, data.Length - tIndex, SocketFlags.None);
                        }
                    }
                    catch (SocketException e)
                    {

                    }

                    if (tIndex == data.Length || timeWatch.ElapsedMilliseconds >= 500)
                    {
                        if (tIndex != 0)
                        {
                            return;
                        }
                        tIndex = 0;
                        timeWatch.Restart();
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {

            }
        }

        public void SocketReceiveLoop(ushort port, Socket socket)
        {
            //int extIp;
            //ushort extPort;
            lock (Connections)
            {
                if (!Connections.ContainsKey(port))
                    return;
                //extIp = (int)((IPEndPoint)socket.RemoteEndPoint).Address.Address;
                //extPort = (ushort)((IPEndPoint)socket.RemoteEndPoint).Port;
            }

            try
            {
                byte[] receiveBuffer = new byte[256];
                int tIndex = 0;
                Stopwatch timeWatch = new Stopwatch();
                timeWatch.Start();
                socket.ReceiveTimeout = 500;

                while (true)
                {
                    lock (Connections)
                    {
                        if (!Connections.ContainsKey(port))
                            return;
                    }

                    try
                    {
                        lock (socket)
                        {
                            tIndex += socket.Receive(receiveBuffer, tIndex, receiveBuffer.Length - tIndex, SocketFlags.None);
                        }
                    }
                    catch (SocketException e)
                    {

                    }

                    if (tIndex == receiveBuffer.Length || timeWatch.ElapsedMilliseconds >= 500)
                    {
                        if (tIndex != 0)
                        {
                            lock (ToSend)
                            {
                                ToSend.Enqueue(TcpPacketStuff.CreateTcpDataPacket(receiveBuffer, tIndex, port, 0, 0));
                            }
                        }    
                        tIndex = 0;
                        timeWatch.Restart();
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception e)
            {

            }

            TryDisconnect(port);
        }
    }
}
