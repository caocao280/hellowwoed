using System.Collections.Concurrent;
using ToolMuaban.Models;

namespace ToolMuaban;

// Giao diện trừu tượng “nói chuyện với game”
public interface IGameWire
{
    Task<bool> ConnectAsync(AccountConfig acc, CancellationToken ct = default);
    Task<bool> LoginAsync(AccountConfig acc, CancellationToken ct = default);

    Task SendChatAsync(string text, AccountConfig acc, CancellationToken ct = default);
    Task SendPrivateChatAsync(string toName, string text, AccountConfig acc, CancellationToken ct = default);

    Task MoveToAsync(short x, short y, AccountConfig acc, CancellationToken ct = default);
    Task SelectZoneHighestAsync(AccountConfig acc, CancellationToken ct = default);

    Task<bool> TradeInviteAsync(string targetName, AccountConfig acc, CancellationToken ct = default);
    Task<bool> TradeAddXuAsync(int xu, AccountConfig acc, CancellationToken ct = default);
    Task<bool> TradeAcceptAsync(AccountConfig acc, CancellationToken ct = default);
    Task TradeCancelAsync(AccountConfig acc, CancellationToken ct = default);

    bool IsConnected(AccountConfig acc);
    Task PingAsync(AccountConfig acc, CancellationToken ct = default);
}

// Bản “log” – hoạt động hoàn chỉnh logic, nhưng không gửi gói ra ngoài.
// Khi đã sẵn proxy thật, bạn viết class ProxyGameWire : IGameWire và thay thế nơi khởi tạo trong Program.cs
public class DummyGameWire : IGameWire
{
    private readonly ConcurrentDictionary<string, bool> _connected = new();

    public Task<bool> ConnectAsync(AccountConfig acc, CancellationToken ct = default)
    {
        _connected[acc.Username] = true;
        Console.WriteLine($"[WIRE] (dummy) Connected as {acc.Username}@sv{acc.ServerId}");
        return Task.FromResult(true);
    }

    public Task<bool> LoginAsync(AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[WIRE] (dummy) Login {acc.Username}/{acc.Password} → Char:{acc.CharName} Map:{acc.Map} Loc:{acc.LocationX},{acc.LocationY}");
        return Task.FromResult(true);
    }

    public Task SendChatAsync(string text, AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[CHAT] {acc.CharName}: {text}");
        return Task.CompletedTask;
    }

    public Task SendPrivateChatAsync(string toName, string text, AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[PM] {acc.CharName} → {toName}: {text}");
        return Task.CompletedTask;
    }

    public Task MoveToAsync(short x, short y, AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[MOVE] {acc.CharName} → ({x},{y})");
        return Task.CompletedTask;
    }

    public Task SelectZoneHighestAsync(AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[ZONE] {acc.CharName} chọn khu cao nhất");
        return Task.CompletedTask;
    }

    public bool IsConnected(AccountConfig acc) => _connected.TryGetValue(acc.Username, out var ok) && ok;

    public Task PingAsync(AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[PING] {acc.CharName}");
        return Task.CompletedTask;
    }

    public Task<bool> TradeInviteAsync(string targetName, AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[TRADE] invite {targetName}");
        return Task.FromResult(true);
    }

    public Task<bool> TradeAddXuAsync(int xu, AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine($"[TRADE] add xu = {xu:N0}");
        return Task.FromResult(true);
    }

    public Task<bool> TradeAcceptAsync(AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine("[TRADE] accept");
        return Task.FromResult(true);
    }

    public Task TradeCancelAsync(AccountConfig acc, CancellationToken ct = default)
    {
        Console.WriteLine("[TRADE] cancel");
        return Task.CompletedTask;
    }
}
