using System;
using ToolMuaban.Models;

namespace ToolMuaban.Services
{
    public class AutoMove
    {
        private readonly AccountConfig _account;

        public AutoMove(AccountConfig acc)
        {
            _account = acc;
        }

        public void GoToConfiguredLocation()
        {
            Console.WriteLine($"[AutoMove] {_account.Username} di chuyển tới map: {_account.Map} tại tọa độ {_account.Location}");
        }
    }
}
