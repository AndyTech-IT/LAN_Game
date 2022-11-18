using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LAN_Game
{
    public struct LAN_Member
    {
        public IPEndPoint EndPoint => new(Address, Port);
        public IPAddress Address;
        public ushort Port;
        public string Name;

        public LAN_Member(string name, IPEndPoint endPoint)
        {
            Address = endPoint.Address;
            Port = (ushort)endPoint.Port;
            Name = name;
        }

        public LAN_Member(string name, IPAddress address, ushort port)
        {
            Address = address;
            Port = port;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name}:{Address}:{Port}";
        }
    }
}
