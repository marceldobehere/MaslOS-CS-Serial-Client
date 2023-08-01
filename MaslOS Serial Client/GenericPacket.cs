using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client
{
    public enum PacketType : byte
    {
        STATE = 10,
        PING = 20,
        DATA = 30,
        INIT = 40,
        CMD = 50
    };

    public class GenericPacket
    {
        public PacketType Type;
        public int Len;
        public ushort From;
        public ushort To;
        public byte[] Data;

        public GenericPacket(PacketType type, ushort from, ushort to, int len, byte[] data)
        {
            Type = type;
            Len = len;
            From = from;
            To = to;
            Data = (byte[])data.Clone();
        }
    }
}
