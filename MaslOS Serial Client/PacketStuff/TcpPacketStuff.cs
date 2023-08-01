using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client.PacketStuff
{
    public static class TcpPacketStuff
    {
        //public static GenericPacket CreateSerialPacketToClient(string text)
        //{
        //    byte[] data = Encoding.ASCII.GetBytes(text);
        //    return new GenericPacket(
        //        PacketType.DATA,
        //        (ushort)Ports.ReservedHostPortsEnum.RawSerial,
        //        (ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient,
        //        data.Length,
        //        data);
        //}


        // Packet SEND Header 
        // CLIENT/SERVER 0-0
        // PORT TO 1-2
        // IP TO 3-6
        // PORT TO 7-8
        // LEN 9-12
        // DATA 13-...

        public static GenericPacket CreateTcpDataPacket(byte[] tcpData, ushort port, int extIp, ushort extPort)
        {
            return CreateTcpDataPacket(tcpData, tcpData.Length, port, extIp, extPort);
        }

        public static GenericPacket CreateTcpDataPacket(byte[] tcpData, int tcpDataLen, ushort port, int extIp, ushort extPort)
        {
            if (tcpDataLen > tcpData.Length)
                tcpDataLen = tcpData.Length;

            byte[] data = new byte[tcpDataLen + 13];
            data[0] = 0;

            data[1] = (byte)(port & 0xFF);
            data[2] = (byte)(port >> 8);

            data[3] = (byte)(extIp & 0xFF);
            data[4] = (byte)((extIp >> 8) & 0xFF);
            data[5] = (byte)((extIp >> 16) & 0xFF);
            data[6] = (byte)(extIp >> 24);

            data[7] = (byte)(extPort & 0xFF);
            data[8] = (byte)(extPort >> 8);

            data[9] = (byte)(tcpDataLen & 0xFF);
            data[10] = (byte)((tcpDataLen >> 8) & 0xFF);
            data[11] = (byte)((tcpDataLen >> 16) & 0xFF);
            data[12] = (byte)(tcpDataLen >> 24);
            
            Array.Copy(tcpData, 0, data, 13, tcpDataLen);

            return new GenericPacket(
                PacketType.DATA,
                (ushort)Ports.ReservedOutClientPortsEnum.TCPClient,
                (ushort)Ports.ReservedHostPortsEnum.TCPHost,
                data.Length,
                data);
        }

        public static GenericPacket CreateTcpConnectionPacket(ushort port, bool connected)
        {
            byte[] data = new byte[10];
            data[0] = 10;

            data[1] = (byte)(port & 0xFF);
            data[2] = (byte)(port >> 8);

            data[3] = 0;
            data[4] = 0;
            data[5] = 0;
            data[6] = 0;

            data[7] = 0;
            data[8] = 0;

            data[9] = (byte)(connected ? 1 : 0);

            return new GenericPacket(
                PacketType.CMD,
                (ushort)Ports.ReservedOutClientPortsEnum.TCPClient,
                (ushort)Ports.ReservedHostPortsEnum.TCPHost,
                data.Length,
                data);
        }



        public static GenericPacket CreateTcpEnablePacket()
        {
            return new GenericPacket(
                PacketType.STATE,
                (ushort)Ports.ReservedOutClientPortsEnum.TCPClient,
                (ushort)Ports.ReservedHostPortsEnum.TCPHost,
                1,
                new byte[] { 1 });
        }

        public static GenericPacket CreateTcpDisablePacket()
        {
            return new GenericPacket(
                PacketType.STATE,
                (ushort)Ports.ReservedOutClientPortsEnum.TCPClient,
                (ushort)Ports.ReservedHostPortsEnum.TCPHost,
                1,
                new byte[] { 0 });
        }
    }
}
