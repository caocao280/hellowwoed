using ToolMuaban.Models;

namespace ToolMuaban;

public class AutoMapSelector
{
    private readonly IGameWire _wire;
    public AutoMapSelector(IGameWire wire) { _wire = wire; }

    public async Task RunSelectZoneLoopAsync()
    {
        while (true)
        {
            foreach (var acc in Config.Accounts)
            {
                if (_wire.IsConnected(acc))
                {
                    await _wire.SelectZoneHighestAsync(acc);
                }
            }
            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}
