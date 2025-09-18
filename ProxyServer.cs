using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ToolMuaban
{
    public class ProxyServer
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;

        public ProxyServer(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public bool Connect()
        {
            try
            {
                _client = new TcpClient(_host, _port);
                _stream = _client.GetStream();
                Console.WriteLine($"[Proxy] Đã kết nối tới server {_host}:{_port}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Proxy] Lỗi kết nối: {ex.Message}");
                return false;
            }
        }

        public void Send(string msg)
        {
            if (_stream == null) return;

            byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
            _stream.Write(data, 0, data.Length);
        }

        public string Receive()
        {
            if (_stream == null) return "";

            using var reader = new StreamReader(_stream, Encoding.UTF8, leaveOpen: true);
            return reader.ReadLine() ?? "";
        }

        public void Close()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}
