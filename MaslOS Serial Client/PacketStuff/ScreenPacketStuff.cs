using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client.PacketStuff
{
    public static class ScreenPacketStuff
    {
        public static GenericPacket CreateScreenEnablePacket()
        {
            return new GenericPacket(
                PacketType.STATE,
                (ushort)Ports.ReservedOutClientPortsEnum.VideoClient,
                (ushort)Ports.ReservedHostPortsEnum.VideoHost,
                1,
                new byte[] { 1 });
        }

        public static GenericPacket CreateScreenDisablePacket()
        {
            return new GenericPacket(
                PacketType.STATE,
                (ushort)Ports.ReservedOutClientPortsEnum.VideoClient,
                (ushort)Ports.ReservedHostPortsEnum.VideoHost,
                1,
                new byte[] { 0 });
        }
    }
}
