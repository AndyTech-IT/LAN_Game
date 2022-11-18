using System.Net.Sockets;
using System.Net;

namespace LAN_Game
{
    public static class UDP_Listener
    {
        public static int PORT_NUMBER { get; private set; }

        public static event Action<LAN_Message>? Message_Received;

        public static bool Is_Online => udp?.Client is not null;

        private static UdpClient? udp;

        public static void Start(int port=81)
        {
            if (Is_Online)
            {
                throw new Exception("Already started, stop first");
            }

            PORT_NUMBER = port;
            udp = new UdpClient(port);
            StartListening();
        }

        public static void Stop()
        {
            udp?.Close();
            udp = null;
        }

        private static void StartListening()
        {
            udp?.BeginReceive(Receive, new object());
        }

        private static void Receive(IAsyncResult ar)
        {
            if (Is_Online)
            {
                IPEndPoint? ip = new(IPAddress.Any, PORT_NUMBER);
                byte[] bytes = udp!.EndReceive(ar, ref ip);
                if (ip!.Address.ToString().Split('.').Last() != "1")
                {
                    LAN_Message message = bytes.Decode_LAN_Message();
                    message.address = ip.Address;
                    Message_Received?.Invoke(message);
                }
                StartListening();
            }
        }
    }
}