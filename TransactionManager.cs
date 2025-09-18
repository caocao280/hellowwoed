using System.Text;
using ToolMuaban.Models;

namespace ToolMuaban;

public class TransactionManager
{
    private readonly string _path;
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public TransactionManager(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        if (File.Exists(_path))
        {
            foreach (var line in File.ReadAllLines(_path, Encoding.UTF8))
            {
                var txId = line.Split('|', 2)[0].Trim();
                if (txId.Length > 0) _seen.Add(txId);
            }
        }
    }

    public bool IsProcessed(string txId)
    {
        lock (_lock) return _seen.Contains(txId);
    }

    public void MarkProcessed(TradeOrder order)
    {
        var line = $"{order.TransactionId}|{DateTime.Now:yyyy-MM-dd HH:mm:ss}|{order.CharName}|sv{order.ServerId}|{order.AmountVnd}|{order.CalcXu(Config.Rate)}";
        lock (_lock)
        {
            _seen.Add(order.TransactionId);
            File.AppendAllLines(_path, new[] { line }, Encoding.UTF8);
        }
    }
}
