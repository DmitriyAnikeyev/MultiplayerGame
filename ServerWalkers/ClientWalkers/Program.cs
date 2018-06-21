using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Client
{
    class Program
    {
        class Player
        {
            public int X { get; set; }
            public int Y { get; set; }
            public char Sprite { get; private set; }
            public ConsoleColor Color { get; private set; }
            public int ID { get; private set; }

            public Player(int x, int y, char sprite, ConsoleColor color, int id)
            {
                X = x;
                Y = y;
                Sprite = sprite;
                Color = color;
                ID = id;
            }

            public void Draw()
            {
                Console.ForegroundColor = Color;
                Console.SetCursorPosition(X, Y);
                Console.Write(Sprite);
                field[X, Y] = ID;
            }

            public void Remove()
            {
                Console.SetCursorPosition(X, Y);
                Console.Write(" ");
                field[X, Y] = 0;
            }
        }

        static int[,] field = new int[20,20];

        static Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        static MemoryStream ms = new MemoryStream(new byte[256], 0, 256, true, true);
        static BinaryWriter writer = new BinaryWriter(ms);
        static BinaryReader reader = new BinaryReader(ms);

        static List<Player> players = new List<Player>();

        static Random random = new Random();

        static Player player;

        enum PacketInfo
        {
            ID, Position
        }

        static void Main(string[] args)
        {
            Console.Title = "Client";
            Console.WriteLine("EnterIp");
            var a = Console.ReadLine();
            Console.WriteLine("Подключение к серверу...");
            socket.Connect(a, 2048);
            Console.WriteLine("Подключено");
            Thread.Sleep(1000);
            Console.Clear();

            Console.WriteLine("Введите спрайт");
            char spr = Convert.ToChar(Console.ReadLine());
            Console.Clear();

            Console.WriteLine("Выберите цвет");
            for (int i = 0; i <= 14; i++)
            {
                Console.ForegroundColor = (ConsoleColor)i;
                Console.WriteLine(i);
            }
            Console.ResetColor();
            ConsoleColor clr = (ConsoleColor)int.Parse(Console.ReadLine());
            Console.Clear();

            int x = random.Next(1, 19);
            int y = random.Next(1, 19);

            Console.WriteLine("Получение идентификатора");
            SendPacket(PacketInfo.ID);
            int id = ReceivePacket();
            Console.WriteLine("Получен ID :" + id);
            Thread.Sleep(1000);
            Console.Clear();

            player = new Player(x, y, spr, clr, id);
            SendPacket(PacketInfo.Position);

            Task.Run(() => { while (true) ReceivePacket(); });

            while (true)
            {
                switch (Console.ReadKey(true).Key)
                {
                    case ConsoleKey.LeftArrow: player.Remove(); player.X = Math.Max(player.X-1,0); player.Draw();
                        SendPacket(PacketInfo.Position);
                        break;
                    case ConsoleKey.RightArrow: player.Remove(); player.X = Math.Min(player.X+1,19); player.Draw();
                        SendPacket(PacketInfo.Position);
                        break;
                    case ConsoleKey.UpArrow: player.Remove(); player.Y = Math.Max(player.Y-1,0); player.Draw();
                        SendPacket(PacketInfo.Position);
                        break;
                    case ConsoleKey.DownArrow: player.Remove(); player.Y = Math.Min(player.Y+1,19); player.Draw();
                        SendPacket(PacketInfo.Position);
                        break;
                        
                }

                if (check() != -1)
                {
                    Console.SetCursorPosition(1, 22);
                    Console.Write("Last Winner: Player" + id);
                }
            }
        }

        static void SendPacket(PacketInfo info)
        {
            ms.Position = 0;
            switch (info)
            {
                case PacketInfo.ID:
                    writer.Write(0);
                    socket.Send(ms.GetBuffer());
                    break;

                case PacketInfo.Position:
                    writer.Write(1);
                    writer.Write(player.ID);
                    writer.Write(player.X);
                    writer.Write(player.Y);
                    writer.Write(player.Sprite);
                    writer.Write((int)player.Color);
                    socket.Send(ms.GetBuffer());
                    break;
            }
        }

        static int ReceivePacket()
        {
            ms.Position = 0;
            socket.Receive(ms.GetBuffer());
            int code = reader.ReadInt32();

            int id;
            int x;
            int y;
            char sprite;
            ConsoleColor color;

            switch (code)
            {
                case 0: return reader.ReadInt32();

                case 1:
                    id = reader.ReadInt32();
                    x = reader.ReadInt32();
                    y = reader.ReadInt32();

                    Player plr = players.Find(p => p.ID == id);

                    if (plr != null)
                    {
                        plr.Remove();
                        plr.X = x;
                        plr.Y = y;
                        plr.Draw();
                        if (check() != -1)
                        {
                            Console.SetCursorPosition(1, 22);
                            Console.Write("Last Winner: Player" + plr.ID);
                        }
                    }
                    else
                    {
                        sprite = reader.ReadChar();
                        color = (ConsoleColor)reader.ReadInt32();
                        plr = new Player(x, y, sprite, color, id);
                        players.Add(plr);
                        plr.Draw();
                    }
                    break;
            }

            return -1;
        }

        public static int check()
        {
            var fl = 0;
            var id = -1;
            for (int i = 0; i < 20; i++)
                for (int j = 0; j < 20; j++)
                {
                    if (field[i, j] != 0)
                        if (fl == 1) return -1;
                        else
                        {
                            fl = 1;
                            id = field[i, j];
                        }
                }
            return (fl == 1) ? id : -1;
        }
    }
}