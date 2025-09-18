using System.Text.RegularExpressions;
using ToolMuaban.Models;

namespace ToolMuaban;

public class AutoChat
{
    private readonly IGameWire _wire;
    private readonly Regex _pmTrigger = new(@"^/pm\s+(.+)$", RegexOptions.IgnoreCase);

    public AutoChat(IGameWire wire) { _wire = wire; }

    public async Task RunSpamLoopAsync()
    {
        var template = Config.AutoReplyTemplate;
        if (string.IsNullOrWhiteSpace(template))
        {
            template = "Bán xu auto. Giá hiện tại: {RATE} xu/1đ. CK ghi: mx_{char}_{sv}. STK: 1014636966 VCB (Nguyen Huu Tuan) / MoMo: 0394229789.";
        }

        while (true)
        {
            foreach (var acc in Config.Accounts)
            {
                if (!_wire.IsConnected(acc)) continue;
                var msg = template
                    .Replace("{RATE}", Config.Rate.ToString("0"))
                    .Replace("{char}", acc.CharName)
                    .Replace("{sv}", acc.ServerId.ToString());

                await _wire.SendChatAsync(msg, acc);
            }
            await Task.Delay(TimeSpan.FromMinutes(2));
        }
    }

    // Cho phép bạn chủ động PM ai đó từ Telegram bằng “/pm tenNV noi-dung…”
    public async Task HandleAdminPmAsync(string raw, AccountConfig anyAcc)
    {
        var m = _pmTrigger.Match(raw);
        if (!m.Success) return;
        var payload = m.Groups[1].Value;
        var sp = payload.Split(' ', 2, StringSplitOptions.TrimEntries);
        if (sp.Length < 2) return;

        string to = sp[0];
        string body = sp[1];

        await _wire.SendPrivateChatAsync(to, body, anyAcc);
    }
}
