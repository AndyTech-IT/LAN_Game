using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LAN_Game
{
    public struct LAN_Message
    {
        public LAN_Member Sender => new(Hostname, address ?? IPAddress.None, Port);
        public IPEndPoint EndPoint => new(address??IPAddress.None, Port);
        public readonly byte MessageType;
        public readonly string Hostname;
        public readonly ushort Port;
        public IPAddress? address;
        public readonly byte[] Data;

        public LAN_Message(byte type, string hostname, ushort port, byte[] data)
        {
            MessageType = type;
            Hostname = hostname;
            Port = port;
            Data = data;
            address = null;
        }

        public override string ToString()
        {
            return $"{MessageType}:{Data.Length} from {Sender}";
        }
    }
}
