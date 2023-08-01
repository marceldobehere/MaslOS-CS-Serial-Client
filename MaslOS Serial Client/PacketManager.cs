using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MaslOS_Serial_Client.Ports;

namespace MaslOS_Serial_Client
{
    public class PacketManager
    {
        public static char[] Signature = new char[3]
        {
            (char)6, '~', '\\'
        };
        public static string SignatureString = new string(Signature);

        public bool ClientExists;

        public bool InitPacketReceived = false;
        public bool InitPacketSent = false;

        public Queue<byte> BytesReceived;
        public Queue<byte> BytesToSend;

        public List<GenericPacket> PacketsToBeSent;
        public List<GenericPacket> PacketsReceived;

        public GenericPacket CurrentSendPacket;
        List<byte> CurrentSendPacketBuffer;

        List<byte> CurrentReceiveBuffer;
        int CurrentReceiveBufferLen;

        public bool[] WorkingOutClientPorts = new bool[]
        {
            true,
            false,
            false,
            false,
            false,
            false,
            false,
        };

        public bool[] WorkingHostPorts = new bool[]
        {
            true,
            false,
            false,
            false,
            false,
            false,
            false,
        };

        public PacketManager()
        {
            BytesReceived = new Queue<byte>();
            BytesToSend = new Queue<byte>();

            PacketsToBeSent = new List<GenericPacket>();
            PacketsReceived = new List<GenericPacket>();

            ClientExists = true;

            CurrentSendPacket = null;
            CurrentSendPacketBuffer = new List<byte>();

            CurrentReceiveBuffer = new List<byte>();
            CurrentReceiveBufferLen = 0;

            InitPacketReceived = false;
            InitPacketSent = false;
        }

        public void SendPacket(ushort from, ushort to, GenericPacket packet)
        {
            packet.From = from;
            packet.To = to;
            SendPacket(packet);
        }

        public void SendPacket(GenericPacket packet)
        {
            bool hasToBeSentOut = false;
            foreach (ushort to in Ports.ReservedHostPorts) // use host ports to send to MaslOS
                if (packet.To == to)
                {
                    hasToBeSentOut = true;
                    break;
                }

            if (packet.Type == PacketType.INIT && packet.To == (ushort)Ports.ReservedOutClientPortsEnum.InitClient)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("<GOT INIT PACKET>");
                InitPacketReceived = true;
                InitPacketSent = false;
                return;
            }

            if (packet.Type == PacketType.STATE)
            {
                bool work = packet.Data[0] == 1;

                for (int i = 0; i < Ports.ReservedOutClientPorts.Length; i++)
                    if (Ports.ReservedOutClientPorts[i] == packet.From)
                    {
                        WorkingOutClientPorts[i] = work;
                        break;
                    }

                for (int i = 0; i < Ports.ReservedHostPorts.Length; i++)
                    if (Ports.ReservedHostPorts[i] == packet.From)
                    {
                        WorkingHostPorts[i] = work;
                        break;
                    }

                if (!hasToBeSentOut)
                    return;
            }

            bool canPacketBeSent = true;
            if (packet.Type != PacketType.INIT && packet.Type != PacketType.STATE)
            {
                if (hasToBeSentOut)
                {
                    for (int i = 0; i < Ports.ReservedOutClientPorts.Length; i++)
                        if (Ports.ReservedOutClientPorts[i] == packet.To)
                        {
                            canPacketBeSent = WorkingOutClientPorts[i];
                            break;
                        }
                }
                else
                {
                    for (int i = 0; i < Ports.ReservedHostPorts.Length; i++)
                        if (Ports.ReservedHostPorts[i] == packet.To)
                        {
                            canPacketBeSent = WorkingHostPorts[i];
                            break;
                        }
                }
            }


            if (!canPacketBeSent && packet.Type != PacketType.STATE)
            {
                return;
            }

            if (hasToBeSentOut)
                PacketsToBeSent.Add(packet);
            else
                PacketsReceived.Add(packet);
        }

        public bool HasPacket(ushort to)
        {
            foreach (GenericPacket packet in PacketsReceived)
                if (packet.To == to)
                    return true;

            return false;
        }

        public GenericPacket GetPacket(ushort to)
        {
            for (int i = 0; i < PacketsReceived.Count; i++)
                if (PacketsReceived[i].To == to)
                {
                    GenericPacket packet = PacketsReceived[i];
                    PacketsReceived.RemoveAt(i);
                    return packet;
                }

            return null;
        }

        public void DoStep()
        {
            if (!ClientExists)
                return;

            for (int a = 0; a < 4; a++)
            {
                for (int i = 0; i < 50; i++)
                    if (!DoReceiveStuff())
                        break;

                for (int i = 0; i < 50; i++)
                    if (!DoSendStuff())
                        break;
            }
        }

        public static byte[] SubArray(byte[] arr, int start)
        {
            byte[] temp = new byte[arr.Length - start];
            for (int i = start; i < arr.Length; i++)
                temp[i - start] = arr[i];
            return temp;
        }

        public bool DoReceiveStuff()
        {
            if (!ClientExists)
                return false;
            if (BytesReceived.Count == 0)
                return false;


            byte c = BytesReceived.Dequeue();
            CurrentReceiveBuffer.Add(c);

            //if (CurrentReceiveBuffer.Count < Signature.Length)
            //    return true;

            if (CurrentReceiveBuffer.Count > 0 && CurrentReceiveBuffer.Count <= Signature.Length)
            {
                int recLen = CurrentReceiveBuffer.Count;
                bool signatureOk = true;
                for (int i = 0; i < recLen; i++)
                    if (CurrentReceiveBuffer[i] != Signature[i])
                        signatureOk = false;

                if (!signatureOk)
                {
                    // we create a send packet and route it to the serial port

                    byte[] tBuff = new byte[recLen];
                    for (int i = 0; i < recLen; i++)
                        tBuff[i] = CurrentReceiveBuffer[i];

                    GenericPacket packet = new GenericPacket(
                        PacketType.DATA,
                        (ushort)Ports.ReservedHostPortsEnum.RawSerial,
                        (ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient,
                        recLen,
                        tBuff
                    );

                    SendPacket(packet);

                    CurrentReceiveBuffer.Clear();
                }
                return true;
            }


            if (CurrentReceiveBuffer.Count < Signature.Length + 5)
                return true;


            if (CurrentReceiveBuffer.Count == Signature.Length + 5)
            {
                byte[] tempBuff = new byte[4];
                for (int i = 0; i < 4; i++)
                    tempBuff[i] = CurrentReceiveBuffer[Signature.Length + i + 1];


                int tempLen = BitConverter.ToInt32(tempBuff, 0) + 9 + Signature.Length;

                //Panic("LEN: {}", to_string(tempLen), true);
                if (tempLen > 10000000) // for now
                    tempLen = 9 + Signature.Length;
                if (tempLen < 9 + Signature.Length)
                    tempLen = 9 + Signature.Length;

                CurrentReceiveBufferLen = tempLen;
                return true;
            }


            if (CurrentReceiveBuffer.Count < CurrentReceiveBufferLen)
                return true;



            if (CurrentReceiveBuffer.Count == CurrentReceiveBufferLen)
            {
                byte[] tBuff = new byte[CurrentReceiveBufferLen];
                for (int i = 0; i < CurrentReceiveBufferLen; i++)
                    tBuff[i] = CurrentReceiveBuffer[i];

                GenericPacket packet = new GenericPacket(
                    (PacketType)tBuff[Signature.Length],
                    BitConverter.ToUInt16(tBuff, Signature.Length + 5),
                    BitConverter.ToUInt16(tBuff, Signature.Length + 7),
                    CurrentReceiveBufferLen - 9 - Signature.Length,
                    SubArray(tBuff, 12)
                );

                SendPacket(packet);

                CurrentReceiveBuffer.Clear();

                return true;
            }


            return true;
        }

        public bool DoSendStuff()
        {
            if (!ClientExists)
                return false;


            if (InitPacketReceived && !InitPacketSent)
            {
                InitPacketSent = true;

                GenericPacket packet = new GenericPacket(
                         PacketType.INIT,
                         (ushort)Ports.ReservedOutClientPortsEnum.InitClient,
                         (ushort)Ports.ReservedHostPortsEnum.InitHost,
                         1,
                         new byte[1] { 1 }
                     );

                SendPacket(packet);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("<SENT INIT DONE PACKET>");
            }

            if (CurrentSendPacket == null && PacketsToBeSent.Count > 0)
            {
                CurrentSendPacket = PacketsToBeSent[0];
                PacketsToBeSent.RemoveAt(0);
                CurrentSendPacketBuffer.Clear();

                for (int i = 0; i < Signature.Length; i++)
                    CurrentSendPacketBuffer.Add((byte)Signature[i]);

                CurrentSendPacketBuffer.Add((byte)CurrentSendPacket.Type);

                {
                    byte[] len = BitConverter.GetBytes(CurrentSendPacket.Len);
                    for (int i = 0; i < 4; i++)
                        CurrentSendPacketBuffer.Add(len[i]);
                }

                {
                    byte[] from = BitConverter.GetBytes(CurrentSendPacket.From);
                    for (int i = 0; i < 2; i++)
                        CurrentSendPacketBuffer.Add(from[i]);
                }

                {
                    byte[] to = BitConverter.GetBytes(CurrentSendPacket.To);
                    for (int i = 0; i < 2; i++)
                        CurrentSendPacketBuffer.Add(to[i]);
                }

                for (int i = 0; i < CurrentSendPacket.Len; i++)
                    CurrentSendPacketBuffer.Add(CurrentSendPacket.Data[i]);

            }
            if (CurrentSendPacket == null)
                return false;

            if (CurrentSendPacketBuffer.Count > 0)
            {
                byte c = CurrentSendPacketBuffer[0];
                CurrentSendPacketBuffer.RemoveAt(0);
                BytesToSend.Enqueue(c);
            }
            else
            {
                CurrentSendPacket = null;
            }

            return true;
        }

    }
}
