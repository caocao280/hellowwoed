namespace ToolMuaban.Models;

public class AccountConfig
{
    public int ServerId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string CharName { get; set; } = "";
    public string Map { get; set; } = "22:73";
    public short LocationX { get; set; } = 457;
    public short LocationY { get; set; } = 216;

    public override string ToString() => $"{Username}@sv{ServerId} ({CharName})";
}
