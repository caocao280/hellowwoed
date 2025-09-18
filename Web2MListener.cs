using System.Net;
using System.Text.Json;

namespace ToolMuaban;

public class Web2MListener
{
    private readonly HttpListener _http = new();
    private readonly AutoTrade _trade;
    private readonly TransactionManager _tx;

    public Web2MListener(AutoTrade trade, TransactionManager txMgr)
    {
        _trade = trade;
        _tx = txMgr;
    }

    public async Task StartAsync()
    {
        if (!HttpListener.IsSupported)
        {
            Console.WriteLine("[WEB2M] HttpListener not supported on this platform.");
            return;
        }

        var prefix = $"http://+:{Config.Web2M.Port}/{Config.Web2M.Path}/";
        _http.Prefixes.Add(prefix);
        _http.Start();
        Console.WriteLine($"[WEB2M] Listening at {prefix}");

        while (true)
        {
            try
            {
                var ctx = await _http.GetContextAsync();
                _ = Handle(ctx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEB2M] {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    private async Task Handle(HttpListenerContext ctx)
    {
        try
        {
            if (ctx.Request.HttpMethod != "POST")
            {
                ctx.Response.StatusCode = 405; ctx.Response.Close(); return;
            }

            // check secret
            var secret = ctx.Request.Headers["X-Secret"];
            if (!string.Equals(secret, Config.Web2M.Secret, StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = 401; ctx.Response.Close(); return;
            }

            using var sr = new StreamReader(ctx.Request.InputStream);
            var raw = await sr.ReadToEndAsync();

            // Kỳ vọng JSON như:
            // { "transactionId":"...", "from":"...", "amountVnd":100000, "note":"mx tuanig 1" }
            var json = JsonDocument.Parse(raw).RootElement;

            string txId = json.TryGetProperty("transactionId", out var p1) ? p1.GetString() ?? Guid.NewGuid().ToString("N") : Guid.NewGuid().ToString("N");
            int amount = json.TryGetProperty("amountVnd", out var p2) ? p2.GetInt32() : 0;
            string note = json.TryGetProperty("note", out var p3) ? p3.GetString() ?? "" : "";
            string from = json.TryGetProperty("from", out var p4) ? p4.GetString() ?? "unknown" : "unknown";

            // Parse note “mx tuanig 1”
            var tele = new TelegramHandler(null!, null!, _tx); // chỉ mượn parser – không dùng network
            var txTmp = typeof(TelegramHandler)
                .GetMethod("ParseTransactionFromText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(tele, new object[] { $"Mã giao dịch: {txId}\nSố tiền: {amount}đ\nLời nhắn: {note}" }) as Models.Transaction;

            if (txTmp == null)
            {
                ctx.Response.StatusCode = 400; await Write(ctx, "Bad note"); return;
            }

            var order = new Models.TradeOrder
            {
                TransactionId = txId,
                From = from,
                AmountVnd = amount,
                ServerId = txTmp.ServerId,
                CharName = txTmp.CharName
            };

            _trade.Enqueue(order);
            ctx.Response.StatusCode = 200; await Write(ctx, "OK");
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            await Write(ctx, ex.Message);
        }
    }

    static Task Write(HttpListenerContext ctx, string message)
    {
        using var sw = new StreamWriter(ctx.Response.OutputStream);
        return sw.WriteAsync(message);
    }
}
