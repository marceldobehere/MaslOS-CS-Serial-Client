using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client.PacketStuff
{
    public static class SerialPacketStuff
    {
        public static GenericPacket CreateSerialPacketToClient(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            return new GenericPacket(
                PacketType.DATA,
                (ushort)Ports.ReservedHostPortsEnum.RawSerial,
                (ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient,
                data.Length,
                data);
        }

        public static GenericPacket CreateSerialPacketToMaslOS(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            return new GenericPacket(
                PacketType.DATA,
                (ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient,
                (ushort)Ports.ReservedHostPortsEnum.RawSerial,
                data.Length,
                data);
        }

        public static GenericPacket CreateSerialEnablePacket()
        {
            return new GenericPacket(
                PacketType.STATE,
                (ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient,
                (ushort)Ports.ReservedHostPortsEnum.RawSerial,
                1,
                new byte[] { 1 });
        }

        public static GenericPacket CreateSerialDisablePacket()
        {
            return new GenericPacket(
                PacketType.STATE,
                (ushort)Ports.ReservedOutClientPortsEnum.RawSerialClient,
                (ushort)Ports.ReservedHostPortsEnum.RawSerial,
                1,
                new byte[] { 0 });
        }
    }
}
