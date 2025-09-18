using ToolMuaban.Models;

namespace ToolMuaban;

public class AutoReconnect
{
    private readonly IGameWire _wire;
    private readonly AutoTrade _trade;
    private readonly List<AccountConfig> _accounts = new();

    public AutoReconnect(IGameWire wire, AutoTrade trade)
    {
        _wire = wire;
        _trade = trade;
    }

    public void TrackAccount(AccountConfig acc) => _accounts.Add(acc);

    public async Task RunAsync()
    {
        while (true)
        {
            foreach (var acc in _accounts)
            {
                try
                {
                    if (!_wire.IsConnected(acc))
                    {
                        await _wire.ConnectAsync(acc);
                        var ok = await _wire.LoginAsync(acc);
                        if (ok)
                        {
                            // đưa về vị trí cấu hình
                            await _wire.MoveToAsync(acc.LocationX, acc.LocationY, acc);
                            await _wire.SelectZoneHighestAsync(acc);
                            // thông báo
                            await _wire.SendChatAsync($"[BOT] Online bán xu – Rate {Config.Rate} xu/1đ – PM để mua", acc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RECONNECT] {acc.Username} error: {ex.Message}");
                }
            }
            await Task.Delay(5000);
        }
    }
}
