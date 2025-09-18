using System.Collections.Concurrent;
using ToolMuaban.Models;

namespace ToolMuaban;

public class AutoTrade
{
    private readonly IGameWire _wire;
    private readonly TransactionManager _txMgr;
    private readonly BlockingCollection<TradeOrder> _queue = new();

    // số xu hiện có (để check đủ/thiếu) – bạn có thể thay bằng đọc in-game nếu đã có opcode trả về.
    private int _xuStock = 0;

    public AutoTrade(IGameWire wire, TransactionManager txMgr)
    {
        _wire = wire;
        _txMgr = txMgr;
    }

    public void Enqueue(TradeOrder order) => _queue.Add(order);

    public void AddXuStock(int amount)
    {
        Interlocked.Add(ref _xuStock, amount);
        Console.WriteLine($"[STOCK] +{amount:N0} xu → tồn: {_xuStock:N0}");
    }

    public int GetStock() => _xuStock;

    public async Task RunAsync()
    {
        foreach (var order in _queue.GetConsumingEnumerable())
        {
            try
            {
                // Dedupe theo transactionId
                if (_txMgr.IsProcessed(order.TransactionId))
                {
                    Console.WriteLine($"[TRADE] Bỏ qua (trùng txId) {order.TransactionId}");
                    continue;
                }

                var xu = order.CalcXu(Config.Rate); // round down
                if (xu <= 0)
                {
                    Console.WriteLine($"[TRADE] Số tiền quá nhỏ ({order.AmountVnd}đ) → 0 xu");
                    continue;
                }

                if (_xuStock < xu)
                {
                    // Báo khách thiếu xu (PM)
                    var acc = Config.Accounts.First(); // chọn acc bất kỳ để gửi PM
                    await _wire.SendPrivateChatAsync(order.CharName, "Xin lỗi, hiện bot không đủ xu. Liên hệ QTV hoặc đợi nạp xu.", acc);
                    Console.WriteLine($"[TRADE] Thiếu xu: cần {xu:N0}, tồn {_xuStock:N0}. Hàng đợi giữ lệnh.");
                    // cho lệnh quay lại queue sau 1 phút
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(60_000);
                        Enqueue(order);
                    });
                    continue;
                }

                // Giao dịch
                var seller = Config.Accounts.First(); // dùng acc 1 để trade (tuỳ bạn đổi chọn theo sv trùng order.ServerId)
                if (seller.ServerId != order.ServerId)
                {
                    // Tìm acc cùng server
                    var svAcc = Config.Accounts.FirstOrDefault(a => a.ServerId == order.ServerId) ?? seller;
                    seller = svAcc;
                }

                Console.WriteLine($"[TRADE] Bắt đầu: {order.CharName} nhận {xu:N0} xu (tx={order.TransactionId}) sv{order.ServerId}");

                // 1) mời trade
                var okInvite = await _wire.TradeInviteAsync(order.CharName, seller);
                if (!okInvite) { Console.WriteLine("[TRADE] Invite thất bại"); continue; }

                await Task.Delay(2000); // chờ cửa sổ

                // 2) add xu
                var okAdd = await _wire.TradeAddXuAsync(xu, seller);
                if (!okAdd) { Console.WriteLine("[TRADE] Add xu thất bại"); continue; }

                await Task.Delay(3000);

                // 3) accept (khóa giao dịch)
                var okAcc = await _wire.TradeAcceptAsync(seller);
                if (!okAcc)
                {
                    Console.WriteLine("[TRADE] Accept thất bại – thử lại sau");
                    // hoàn tiền xu về kho nếu fail cứng
                    continue;
                }

                // Đến đây coi như thành công, trừ kho & log
                Interlocked.Add(ref _xuStock, -xu);
                _txMgr.MarkProcessed(order);
                await _wire.SendPrivateChatAsync(order.CharName,
                    $"Đã giao {xu:N0} xu cho bạn. Cảm ơn!", seller);

                Console.WriteLine($"[TRADE] OK tx={order.TransactionId} → tồn {_xuStock:N0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRADE] Lỗi: {ex.Message}");
            }
        }
    }
}
