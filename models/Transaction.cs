namespace ToolMuaban.Models;

public class Transaction
{
    public string TransactionId { get; set; } = "";
    public string From { get; set; } = "";
    public int AmountVnd { get; set; }
    public string CharName { get; set; } = "";
    public int ServerId { get; set; }
    public DateTime Time { get; set; }
}
