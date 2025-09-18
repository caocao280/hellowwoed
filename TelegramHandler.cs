using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using ToolMuaban.Models;

namespace ToolMuaban;

public class TelegramHandler
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly AutoTrade _trade;
    private readonly AutoChat _chat;
    private readonly TransactionManager _txMgr;

    private long _offset = 0;

    // regex parse “mx tuanig 1”
    private readonly Regex _mx = new(@"mx[_\s\-]*([a-zA-Z0-9_]+)[_\s\-]+(\d+)", RegexOptions.IgnoreCase);

    public TelegramHandler(AutoTrade trade, AutoChat chat, TransactionManager txMgr)
    {
        _trade = trade;
        _chat = chat;
        _txMgr = txMgr;
    }

    string ApiBase => $"https://api.telegram.org/bot{Config.Telegram.Token}";

    public async Task StartPollingAsync()
    {
        if (string.IsNullOrWhiteSpace(Config.Telegram.Token))
        {
            Console.WriteLine("[TELE] Bỏ qua (chưa cấu hình BOT_TOKEN)");
            return;
        }

        Console.WriteLine("[TELE] Polling…");
        _ = _trade.RunAsync(); // bật luôn trade worker

        while (true)
        {
            try
            {
                var url = $"{ApiBase}/getUpdates?timeout=30&allowed_updates=%5B%22message%22%5D";
                if (_offset > 0) url += $"&offset={_offset}";
                var resp = await _http.GetFromJsonAsync<TelegramUpdates>(url);
                if (resp?.ok == true && resp.result is { Count: > 0 })
                {
                    foreach (var up in resp.result)
                    {
                        _offset = up.update_id + 1;
                        await HandleUpdate(up);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TELE] Poll error: {ex.Message}");
                await Task.Delay(2000);
            }
        }
    }

    private async Task HandleUpdate(TelegramUpdate up)
    {
        var msg = up.message;
        if (msg == null) return;

        var text = msg.text ?? "";
        var fromName = msg.from?.username ?? msg.from?.first_name ?? "unknown";
        var chatId = msg.chat?.id ?? 0;

        // Admin lệnh
        if (Config.Admins.Contains(fromName))
        {
            if (text.StartsWith("/rate", StringComparison.OrdinalIgnoreCase))
            {
                var sp = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (sp.Length == 2 && decimal.TryParse(sp[1], out var r))
                {
                    // cập nhật rate runtime + file
                    await File.WriteAllTextAsync("data/RateConfig.txt", $"RATE={r}");
                    Config.LoadAll();
                    await SendText(chatId, $"Rate cập nhật: {Config.Rate} xu/1đ");
                    return;
                }
                await SendText(chatId, $"Rate hiện tại: {Config.Rate} xu/1đ");
                return;
            }
            if (text.StartsWith("/stock", StringComparison.OrdinalIgnoreCase))
            {
                await SendText(chatId, $"Tồn xu: {_trade.GetStock():N0}");
                return;
            }
            if (text.StartsWith("/napxu", StringComparison.OrdinalIgnoreCase))
            {
                var sp = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (sp.Length == 2 && int.TryParse(sp[1], out var v) && v > 0)
                {
                    _trade.AddXuStock(v);
                    await SendText(chatId, $"+{v:N0} xu → tồn: {_trade.GetStock():N0}");
                }
                return;
            }
            if (text.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase))
            {
                var acc = Config.Accounts.First();
                await _chat.HandleAdminPmAsync(text, acc);
                await SendText(chatId, "Đã gửi PM.");
                return;
            }
        }

        // Parse tin nhận tiền forward về (từ Web2M/Bank/MoMo) – tìm “mx name sv” + số tiền + mã gd
        var tx = ParseTransactionFromText(text);
        if (tx != null)
        {
            if (_txMgr.IsProcessed(tx.TransactionId))
            {
                await SendText(chatId, $"(bỏ qua) đã xử lý tx: {tx.TransactionId}");
                return;
            }

            var order = new TradeOrder
            {
                TransactionId = tx.TransactionId,
                From = tx.From,
                AmountVnd = tx.AmountVnd,
                ServerId = tx.ServerId,
                CharName = tx.CharName
            };

            _trade.Enqueue(order);
            await SendText(chatId, $"Đã nhận lệnh: {order.CharName}@sv{order.ServerId} – {order.AmountVnd:N0}đ → ~{order.CalcXu(Config.Rate):N0} xu");
            return;
        }
    }

    private Transaction? ParseTransactionFromText(string text)
    {
        // Tìm mã giao dịch
        var txId = ExtractFirst(text, @"(Mã giao dịch|Ma GD|GD|Transaction Id)[:\s]+([A-Za-z0-9/]+)");
        if (string.IsNullOrWhiteSpace(txId))
            txId = ExtractFirst(text, @"\b([A-Za-z]{2}\d{8,})\b"); // fallback

        // Số tiền
        var moneyStr = ExtractFirst(text, @"Số tiền[:\s]+([0-9\.\,]+)\s?đ");
        if (string.IsNullOrWhiteSpace(moneyStr))
            moneyStr = ExtractFirst(text, @"\b([0-9\.\,]+)\s?VND\b");

        if (!TryParseVnd(moneyStr, out var amount)) return null;

        // Lời nhắn: “mx tuanig 1”
        var m = _mx.Match(text);
        if (!m.Success) return null;
        var charName = m.Groups[1].Value;
        var svStr = m.Groups[2].Value;
        if (!int.TryParse(svStr, out var sv)) return null;

        // From (optional – bank/momo id)
        var from = ExtractFirst(text, @"(Số điện thoại|Tài khoản|From)[:\s]+([0-9A-Za-z]+)");

        return new Transaction
        {
            TransactionId = txId ?? Guid.NewGuid().ToString("N"),
            From = from ?? "unknown",
            AmountVnd = amount,
            CharName = charName,
            ServerId = sv,
            Time = DateTime.Now
        };
    }

    static string? ExtractFirst(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (!m.Success) return null;
        return m.Groups[^1].Value.Trim();
    }

    static bool TryParseVnd(string? s, out int vnd)
    {
        vnd = 0;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Replace(".", "").Replace(",", "");
        return int.TryParse(s, out vnd);
    }

    private async Task SendText(long chatId, string text)
    {
        try
        {
            var url = $"{ApiBase}/sendMessage";
            var payload = new { chat_id = chatId, text = text };
            var resp = await _http.PostAsJsonAsync(url, payload);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TELE] send msg error: {ex.Message}");
        }
    }

    // Telegram DTOs (tối giản)
    public record TelegramUpdates(bool ok, List<TelegramUpdate> result);
    public record TelegramUpdate(long update_id, TelegramMessage? message);
    public record TelegramMessage(long message_id, TelegramChat chat, TelegramUser? from, string? text);
    public record TelegramChat(long id, string? type);
    public record TelegramUser(long id, string? username, string? first_name, string? last_name);
}
