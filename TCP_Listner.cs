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
    public class TCP_Listener
    {
        public event Action<LAN_Message>? Message_Received;
        public ushort Port { get; private set; }
        public bool Enabled => _endbled;

        private TcpListener _listener;
        private bool _endbled;

        public TCP_Listener(IPAddress? address = null, ushort port = 0)
        {
            _endbled = false;
            _listener = new TcpListener(address ?? IPAddress.Any, port);
        }

        public int Start()
        {
            if (Enabled)
            {
                throw new Exception("Just enabled!");
            }
            _listener.Start();
            _listener.BeginAcceptTcpClient(On_Connect, _listener);
            Port = (ushort)((IPEndPoint)_listener.LocalEndpoint).Port;
            _endbled = true;

            return Port;
        }

        public void Stop()
        {
            if (Enabled == false)
            {
                throw new Exception("Just stopped!");
            }
            if (_listener.Pending())
                Debug.WriteLine("Oups.. Someone respons before closing -_-");
            _listener.Stop();
            _endbled = false;
        }

        private void On_Connect(IAsyncResult result)
        {
            try
            {
                TcpClient client = _listener.EndAcceptTcpClient(result);
                byte[] data = new byte[client.ReceiveBufferSize];
                client.Client.Receive(data);
                LAN_Message message = data.Decode_LAN_Message();
                message.address = IPEndPoint.Parse(client.Client.LocalEndPoint!.ToString()!).Address;
                Message_Received?.Invoke(message);
                client.Close();
                _listener.BeginAcceptTcpClient(On_Connect, _listener);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
