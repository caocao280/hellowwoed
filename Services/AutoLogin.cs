using System;
using ToolMuaban.Models;

namespace ToolMuaban.Services
{
    public class AutoLogin
    {
        private readonly AccountConfig _acc;
        private readonly ProxyServer _proxy;

        public AutoLogin(AccountConfig acc)
        {
            _acc = acc;
            _proxy = new ProxyServer("127.0.0.1", 5555); // đổi IP/port thật của Ninja School
        }

        public bool Login()
        {
            if (!_proxy.Connect()) return false;

            string packet = $"LOGIN|{_acc.Username}|{_acc.Password}";
            _proxy.Send(packet);

            string response = _proxy.Receive();
            Console.WriteLine($"[AutoLogin] Server trả về: {response}");

            return response.Contains("OK");
        }
    }
}
