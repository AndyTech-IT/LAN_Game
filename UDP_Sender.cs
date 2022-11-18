using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LAN_Game
{
    public static class UDP_Sender
    {
        public static void SendBroadcast(byte[] data, int port)
        {
            foreach (var adress in Dns.GetHostEntry(Dns.GetHostName(), AddressFamily.InterNetwork).AddressList)
            {
                string[] splited = adress.ToString().Split('.');
                string broadcast_adress = string.Join(".", splited.Take(splited.Length - 1).Append("255"));
                Send(data, broadcast_adress, port);
            }
        }

        public static void Send(byte[] data, string adress, int port)
        {
            using UdpClient client = new();
            client.Send(data, data.Length, adress, port);
        }
    }
}
