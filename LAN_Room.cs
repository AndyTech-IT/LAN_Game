using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;

namespace LAN_Game
{
    public static class LAN_Room
    {
        public const ushort PORT = 80;

        public static LAN_Member Room_Data => My_Data;

        private static LAN_Member My_Data;
        private static string Password;
        private static int Max_Players;
        private static int Cur_Players => Players.Count;
        private static List<LAN_Member> Players;
        private static LAN_Member Host;

        private static bool Opened => _listener.Enabled;
        private static TCP_Listener _listener;

        public static event Action<LAN_Member>? Player_Enter;
        public static event Action<LAN_Message>? Player_Send;
        public static event Action<LAN_Member>? Player_Leave;


        public enum Message_Type : byte
        {
            Room_Info,
            Connection_Rejected,
            Connection_Accepted,
            Connection_Aborted,
            Player_Enter,
            Player_Leave
        }

        static LAN_Room()
        {
            string name = Dns.GetHostName();
            IPAddress adress = Dns.GetHostAddresses(name, AddressFamily.InterNetwork).First(ip => ip.ToString().Split('.').Last() != "1");
            My_Data = new LAN_Member(name, adress, PORT);
            Players = new List<LAN_Member>();
            Password = "";
            _listener = new(adress, port: My_Data.Port);

            UDP_Listener.Message_Received += On_BroadcastMessage_Received;
            _listener.Message_Received += Message_Received;
        }

        public static void Open(LAN_Member host, string name = "", string password = "", int max_players = 4)
        { 
            if (max_players < 1)
                throw new ArgumentOutOfRangeException(nameof(max_players));

            _listener.Start();
            Password = password;
            Host = host;
            if (name != "")
                My_Data.Name = name;

            Players = new() {};
            Max_Players = max_players;

            Plyaer_Join(host);
        }

        public static bool Send_Message(byte type, IPEndPoint endPoint, params object[] data)
        {
            try
            {
                TcpClient client = new();
                client.Connect(endPoint);
                byte[] encoded_data = Array.Empty<byte>();
                foreach (var item in data)
                {
                    encoded_data = encoded_data.Concat(item.Encode()).ToArray();
                }
                LAN_Message message = new(type, My_Data.Name, My_Data.Port, encoded_data);
                byte[] respons_bytes = message.Encode();
                client.Client.Send(respons_bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Kik_Player(LAN_Member member)
        {
            if (Players.Contains(member))
            {
                Send_Message((byte)Message_Type.Connection_Aborted, member.EndPoint, "Host will kik you!");
                Players.Remove(member);
                foreach (var player in Players)
                    Send_Message((byte)Message_Type.Player_Leave, player.EndPoint, member);
            }
        }

        public static void Close()
        {
            foreach(var player in Players)
            {
                Send_Message((byte)Message_Type.Connection_Aborted, player.EndPoint, "Server was closed!");
            }
            Players.Clear();
        }


        private static void On_BroadcastMessage_Received(LAN_Message message)
        {
            if (message.MessageType == (byte)LAN_Player.Message_Type.Get_Info)
            {
                On_Info_Request(message);
                return;
            }

            Debug.WriteLine("Unknown broadcast message received!");
        }


        private static void Message_Received(LAN_Message message)
        {
            switch ((LAN_Player.Message_Type)message.MessageType)
            {
                case LAN_Player.Message_Type.Get_Info:
                    On_Info_Request(message);
                    break;
                case LAN_Player.Message_Type.Connect:
                    On_Connect_Request(message);
                    break;
                case LAN_Player.Message_Type.Disconect:
                    On_Player_Disconect(message);
                    break;
            }
        }

        private static void On_Info_Request(LAN_Message message)
        {
            Send_Message((byte)Message_Type.Room_Info, message.EndPoint, Cur_Players, Max_Players);
        }

        private static void On_Connect_Request(LAN_Message message)
        {
            if (Cur_Players >= Max_Players)
            {
                Send_Message((byte)Message_Type.Connection_Rejected, message.EndPoint, "Server is full!");
                return;
            }

            if (Players.Any(p => p.Name == message.Hostname && p.EndPoint.ToString() == message.EndPoint.ToString()))
            {
                Send_Message((byte)Message_Type.Connection_Rejected, message.EndPoint, "You just on server!");
                Debug.WriteLine($"{message.Sender} try connect again!");
                return;
            }

            string password = message.Data.Decode_String().Result;
            if (password != Password)
            {
                Send_Message((byte)Message_Type.Connection_Rejected, message.EndPoint, "Wrong password!");
                return;
            }
            Plyaer_Join(message.Sender);
        }

        private static void Plyaer_Join(LAN_Member member)
        {
            // Sync new player
            Send_Message((byte)Message_Type.Connection_Accepted, member.EndPoint, Host, Players.ToArray());

            // Notify other players
            foreach (var player in Players)
                Send_Message((byte)Message_Type.Player_Enter, player.EndPoint, member);

            // Update data
            Players.Add(member);
            Player_Enter?.Invoke(member);
        }

        private static void On_Player_Disconect(LAN_Message message)
        {
            if (Players.Contains(message.Sender) == false)
            {
                Debug.WriteLine($"{message.Sender} (not in the room) try disconect!");
                return;
            }

            Players.Remove(message.Sender);

            foreach (var player in Players)
                Send_Message((byte)Message_Type.Player_Leave, player.EndPoint, message.Sender);

            Player_Leave?.Invoke(message.Sender);
        }
    }
}
