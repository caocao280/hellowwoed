namespace ToolMuaban.Models;

public class TradeOrder
{
    public string TransactionId { get; set; } = "";
    public string From { get; set; } = "";
    public int AmountVnd { get; set; }
    public string CharName { get; set; } = "";
    public int ServerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int CalcXu(decimal rate)
    {
        var xu = (int)Math.Floor(AmountVnd * rate);
        return xu;
    }
}
