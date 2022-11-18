using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LAN_Game
{
    public class LAN_Player
    {
        public LAN_Member Player_Data => _my_data;
        public string Name
        {
            get => Player_Data.Name;
            set
            {
                if (value != "")
                    _my_data.Name = value;
            }
        }
        public LAN_Member[] Rooms => _rooms.ToArray();
        public LAN_Member? Room_Host => _room_host;
        public LAN_Member? Curent_Room => _cur_room;
        public LAN_Member[] Room_Players => _room_players.ToArray();

        private LAN_Member _my_data; 
        private TCP_Listener _listener;

        private List<LAN_Member> _rooms;

        private LAN_Member? _room_host;
        private LAN_Member? _cur_room;
        private List<LAN_Member> _room_players;

        public event Action<LAN_Message>? Room_Get_Info;
        public event Action<LAN_Message>? Room_Update_Info;
        public event Action<LAN_Message>? Rject_Enter_In_Room;
        public event Action? Me_Enter_In_Room;
        public event Action? Me_Leave_Room;
        public event Action<LAN_Member>? Other_Enter_In_Room;
        public event Action<LAN_Member>? Other_Leave_Room;

        public enum Message_Type: byte
        {
            Get_Info,
            Connect,
            Disconect
        }

        public LAN_Player()
        {
            Name = Dns.GetHostName();

            _rooms = new List<LAN_Member>();
            _room_players = new List<LAN_Member>();

            _listener = new();
            _listener.Message_Received += On_Message_Received;
            _my_data.Port = (ushort)_listener.Start();
            _my_data.Address = Dns.GetHostAddresses(Name, AddressFamily.InterNetwork).First(ip => ip.ToString().Split('.').Last() != "1");
        }

        public void Search_Servers()
        {
            _rooms.Clear();
            byte[] data = new LAN_Message((byte)Message_Type.Get_Info, Name, _listener.Port, Array.Empty<byte>()).Encode().ToArray();
            UDP_Sender.SendBroadcast(data, LAN_Room.PORT);
        }

        public bool Send_Message(byte type, IPEndPoint endPoint, params object[] data)
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
                LAN_Message message = new(type, _my_data.Name, _my_data.Port, encoded_data);
                byte[] respons_bytes = message.Encode();
                client.Client.Send(respons_bytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void TryConect(IPEndPoint server, string password="")
        {
            if (_cur_room is not null)
                throw new Exception("You just connected!");


            Send_Message((byte)Message_Type.Connect, server,  password);
        }

        private void On_Message_Received(LAN_Message message)
        {
            switch ((LAN_Room.Message_Type)message.MessageType)
            {
                case LAN_Room.Message_Type.Room_Info:
                    On_Room_Info(message);
                    break;

                case LAN_Room.Message_Type.Connection_Rejected:
                    On_Connection_Rejected(message);
                    break;
                case LAN_Room.Message_Type.Connection_Accepted:
                    On_Connection_Accepted(message);
                    break;
                case LAN_Room.Message_Type.Connection_Aborted:
                    On_Connection_Aborted(message);
                    break;
                case LAN_Room.Message_Type.Player_Enter:
                    On_Player_Enter(message);
                    break;
                case LAN_Room.Message_Type.Player_Leave:
                    On_Player_Leave(message);
                    break;
            }
        }

        private void On_Room_Info(LAN_Message message)
        {
            if (_rooms.FirstOrDefault(r => r.Address == message.address) is LAN_Member room)
            {
                if (room.Name != message.Hostname)
                    room.Name = message.Hostname;

                Room_Update_Info?.Invoke(message);
                return;
            }

            _rooms.Add(message.Sender);
            Room_Get_Info?.Invoke(message);
        }

        private void On_Connection_Rejected(LAN_Message message)
        {
            Debug.WriteLine($"{message.Data.Decode_String().Result}");
            Rject_Enter_In_Room?.Invoke(message);
        }

        private void On_Connection_Accepted(LAN_Message message)
        {
            Debug.WriteLine($"I`m enter in {message.Sender}");

            _cur_room = message.Sender;
            var host = message.Data.Decode_LAN_Member();
            _room_host = host;
            _room_players.Clear();
            _room_players.AddRange(message.Data.Decode_LAN_Members(host.EndIndex).Result);

            Me_Enter_In_Room?.Invoke();
        }

        private void On_Connection_Aborted(LAN_Message message)
        {
            Debug.WriteLine($"I`m disconected from {message.Sender}. Reason: '{message.Data.Decode_String().Result}'");

            _cur_room = null;
            _room_players.Clear();
            Me_Leave_Room?.Invoke();
        }

        private void On_Player_Enter(LAN_Message message)
        {
            LAN_Member member = message.Data.Decode_LAN_Member();
            Debug.WriteLine($"Player {member} enter in the room");

            _room_players.Add(member);

            Other_Enter_In_Room?.Invoke(member);
        }

        private void On_Player_Leave(LAN_Message message)
        {
            LAN_Member member = message.Data.Decode_LAN_Member();
            Debug.WriteLine($"Player {member} leave room");

            _room_players.Remove(member);

            Other_Leave_Room?.Invoke(member);
        }

    }
}
