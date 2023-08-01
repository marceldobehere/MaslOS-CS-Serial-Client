using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaslOS_Serial_Client.Connectors
{
    public class CustomScreenConnector
    {
        public Process Proc;
        public TcpListener Server;
        public Socket Client;
        public int Port;
        public bool Running;
        byte[] imgData;

        public int Width = 640;
        public int Height = 480;

        public Queue<Queue<(int x, int y, byte r, byte g, byte b)>> Frames;
        //public List<(int x, int y, byte r, byte g, byte b)> PixelUpdates;

        public CustomScreenConnector()
        {
            Port = CustomSerialConnector.Rnd.Next(3600, 3700);
            Server = new TcpListener(IPAddress.Any, Port);
            Server.Start();

            Console.WriteLine($"<STARTING SCREEN SERVER AT localhost:{Port}>");
            Proc = Process.Start(AppContext.BaseDirectory + "/screen/MaslOS Serial Client Screen.exe", $"{Port}");
            Client = Server.AcceptSocket();
            Running = true;

            imgData = new byte[Width * Height * 3];
            Frames = new Queue<Queue<(int x, int y, byte r, byte g, byte b)>>();
            //Clear();

            SendData(Client, BitConverter.GetBytes(Width));
            SendData(Client, BitConverter.GetBytes(Height));

            new Thread(() => { SendLoop(); }).Start();
        }

        public void Init()
        {
            Clear();
        }

        public void Clear()
        {
            var list = new Queue<(int x, int y, byte r, byte g, byte b)>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    UpdatePixel(x, y, 0, 0, 0, list);
            Frames.Enqueue(list);
        }

        public static void SendData(Socket socket, byte[] data)
        {
            int i = 0;
            while (i < data.Length)
                i += socket.Send(data, i, data.Length - i, SocketFlags.None);
        }

        public static void ReadData(Socket socket, byte[] data)
        {
            int i = 0;
            while (i < data.Length)
                i += socket.Receive(data, i, data.Length - i, SocketFlags.None);
        }

        public void SendLoop()
        {
            while (Running)
            {
                if (Frames.Count > 0)
                {
                    SendFrame();
                }
            }
        }

        public void SendFrame()
        {
            if (!Running)
                return;

            try
            {
                Queue<(int x, int y, byte r, byte g, byte b)> temp = null;
                lock (Frames)
                {
                    if (Frames.Count == 0)
                        return;

                    temp = Frames.Dequeue();
                }

                if (temp.Count == 0)
                    return;


                SendData(Client, BitConverter.GetBytes(temp.Count));
                while (temp.Count > 0)
                {
                    var update = temp.Dequeue();
                    SendData(Client, BitConverter.GetBytes((short)update.x));
                    SendData(Client, BitConverter.GetBytes((short)update.y));
                    SendData(Client, new byte[3] { update.r, update.g, update.b });
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"ERROR: {e}");
                Running = false;
            }
        }

        public void UpdatePixel(int x, int y, byte r, byte g, byte b, Queue<(int x, int y, byte r, byte g, byte b)> updateList)
        {
            //Console.Write($"<{x},{y} {r}{g}{b}>");
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                Console.Write($"<INV {x},{y} {r}{g}{b}>");
                return;
            }

            imgData[0 + ((x + y * Width) * 3)] = r;
            imgData[1 + ((x + y * Width) * 3)] = g;
            imgData[2 + ((x + y * Width) * 3)] = b;

            updateList.Enqueue((x, y, r, g, b));
        }

        public void HandleScreenUpdate(GenericPacket packet)
        {
            var list = new Queue<(int x, int y, byte r, byte g, byte b)>();
            
            int lineCount = 0;
            int mainIndex = 0;
            while (mainIndex < packet.Data.Length)
            {
                int x = BitConverter.ToUInt16(packet.Data, mainIndex);
                mainIndex += 2;
                int y = BitConverter.ToUInt16(packet.Data, mainIndex);
                mainIndex += 2;
                int count = BitConverter.ToUInt16(packet.Data, mainIndex);
                mainIndex += 2;
                lineCount++;

                for (int temp = 0; temp < count; temp++)
                {
                    byte r = packet.Data[mainIndex];
                    mainIndex++;
                    byte g = packet.Data[mainIndex];
                    mainIndex++;
                    byte b = packet.Data[mainIndex];
                    mainIndex++;
                    
                    UpdatePixel(x, y, r, g, b, list);
                    x++;
                }
            }

            lock (Frames)
            {
                Frames.Enqueue(list);
            }

            //Console.ForegroundColor = ConsoleColor.DarkYellow;
            //Console.WriteLine($"    <SCR {packet.Len}, {lineCount}>");

        }
    }
}
