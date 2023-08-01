using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client
{
    public static class Ports
    {
        public enum ReservedHostPortsEnum : ushort
        {
            InitHost = 001,
            RawSerial = 101,
            VideoHost = 201,
            AudioHost = 301,
            HIDClient = 401,
            TCPHost = 501,
            FileShareClient = 601,
        };

        public static ushort[] ReservedHostPorts = new ushort[]
        {
            (ushort)ReservedHostPortsEnum.InitHost,
            (ushort)ReservedHostPortsEnum.RawSerial,
            (ushort)ReservedHostPortsEnum.VideoHost,
            (ushort)ReservedHostPortsEnum.AudioHost,
            (ushort)ReservedHostPortsEnum.HIDClient,
            (ushort)ReservedHostPortsEnum.TCPHost,
            (ushort)ReservedHostPortsEnum.FileShareClient
        };

        public enum ReservedOutClientPortsEnum : ushort
        {
            InitClient = 002,
            RawSerialClient = 102,
            VideoClient = 202,
            AudioClient = 302,
            HIDHost = 402,
            TCPClient = 502,
            FileShareHost = 602,
        };

        public static ushort[] ReservedOutClientPorts = new ushort[]
        {
            (ushort)ReservedOutClientPortsEnum.InitClient,
            (ushort)ReservedOutClientPortsEnum.RawSerialClient,
            (ushort)ReservedOutClientPortsEnum.VideoClient,
            (ushort)ReservedOutClientPortsEnum.AudioClient,
            (ushort)ReservedOutClientPortsEnum.HIDHost,
            (ushort)ReservedOutClientPortsEnum.TCPClient,
            (ushort)ReservedOutClientPortsEnum.FileShareHost
        };

    }
}
