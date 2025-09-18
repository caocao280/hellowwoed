using ToolMuaban.Models;

namespace ToolMuaban;

public class AutoLoop
{
    private readonly IGameWire _wire;

    public AutoLoop(IGameWire wire)
    {
        _wire = wire;
    }

    public async Task RunAsync()
    {
        var rnd = new Random();
        while (true)
        {
            foreach (var acc in Config.Accounts)
            {
                if (!_wire.IsConnected(acc)) continue;

                // Ping + nhúc nhích để tránh AFK
                await _wire.PingAsync(acc);
                short dx = (short)rnd.Next(-3, 4);
                short dy = (short)rnd.Next(-2, 3);
                var nx = (short)(acc.LocationX + dx);
                var ny = (short)(acc.LocationY + dy);
                await _wire.MoveToAsync(nx, ny, acc);
                // quay lại vị trí chuẩn sau 10s
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10_000);
                    await _wire.MoveToAsync(acc.LocationX, acc.LocationY, acc);
                });
            }
            await Task.Delay(30_000);
        }
    }
}
