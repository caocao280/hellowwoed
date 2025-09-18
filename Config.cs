using System.Text;
using ToolMuaban.Models;

namespace ToolMuaban;

public static class Config
{
    public static List<AccountConfig> Accounts { get; private set; } = new();
    public static decimal Rate { get; private set; } = 96m; // 1 VND = 96 xu (mặc định)
    public static string AutoReplyTemplate { get; private set; } = "";
    public static HashSet<string> Admins { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public static TelegramCfg Telegram { get; private set; } = new();
    public static Web2MCfg Web2M { get; private set; } = new();

    public record TelegramCfg
    {
        public string Token { get; init; } = "";
        public long? AdminChatId { get; init; }
    }

    public record Web2MCfg
    {
        public bool Enabled { get; init; }
        public int Port { get; init; } = 8088;
        public string Path { get; init; } = "web2m";
        public string Secret { get; init; } = "";
    }

    public static void LoadAll()
    {
        Directory.CreateDirectory("data");

        Accounts = LoadAccounts("data/Accounts.txt");
        Rate = LoadRate("data/RateConfig.txt");
        AutoReplyTemplate = LoadText("data/AutoReply.txt");

        var (token, chatId, admins) = LoadTelegramCfg("data/TelegramBot.txt");
        Admins = new HashSet<string>(admins, StringComparer.OrdinalIgnoreCase);
        Telegram = new TelegramCfg { Token = token, AdminChatId = chatId };

        Web2M = LoadWeb2M("data/Web2M.txt");
    }

    static List<AccountConfig> LoadAccounts(string path)
    {
        var list = new List<AccountConfig>();
        if (!File.Exists(path)) return list;

        // Hỗ trợ 2 định dạng: (1) Tag kiểu [SV]1[/SV]…; (2) CSV nhẹ: server,username,password,char,mapX:mapY,locX,locY
        foreach (var raw in File.ReadAllLines(path, Encoding.UTF8).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")))
        {
            if (raw.Contains("[SV]"))
            {
                var acc = new AccountConfig
                {
                    ServerId = ParseTagInt(raw, "SV"),
                    Username = ParseTag(raw, "TK"),
                    Password = ParseTag(raw, "MK"),
                    CharName = ParseTag(raw, "NV"),
                    Map = ParseTag(raw, "MAP"),
                    LocationX = ParseTagPoint(raw, "LOCATION").x,
                    LocationY = ParseTagPoint(raw, "LOCATION").y
                };
                list.Add(acc);
            }
            else
            {
                // CSV:  sv,username,password,char,mapX:mapY,locX,locY
                var p = raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (p.Length >= 7)
                {
                    list.Add(new AccountConfig
                    {
                        ServerId = int.Parse(p[0]),
                        Username = p[1],
                        Password = p[2],
                        CharName = p[3],
                        Map = p[4],
                        LocationX = short.Parse(p[5]),
                        LocationY = short.Parse(p[6]),
                    });
                }
            }
        }
        return list;
    }

    static int ParseTagInt(string text, string tag) => int.Parse(ParseTag(text, tag));
    static string ParseTag(string text, string tag)
    {
        var open = $"[{tag}]";
        var close = $"[/{tag}]";
        var i = text.IndexOf(open, StringComparison.OrdinalIgnoreCase);
        var j = text.IndexOf(close, StringComparison.OrdinalIgnoreCase);
        if (i < 0 || j < 0 || j <= i) return "";
        return text.Substring(i + open.Length, j - (i + open.Length)).Trim();
    }

    static (short x, short y) ParseTagPoint(string text, string tag)
    {
        var val = ParseTag(text, tag); // "457,216"
        var sp = val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (sp.Length == 2 && short.TryParse(sp[0], out var x) && short.TryParse(sp[1], out var y))
            return (x, y);
        return (0, 0);
    }

    static decimal LoadRate(string path)
    {
        if (!File.Exists(path)) return 96m;
        foreach (var l in File.ReadAllLines(path, Encoding.UTF8))
        {
            var line = l.Trim();
            if (line.StartsWith("#") || line.Length == 0) continue;
            // ví dụ: RATE=96
            var kv = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length == 2 && kv[0].Equals("RATE", StringComparison.OrdinalIgnoreCase))
                if (decimal.TryParse(kv[1], out var r)) return r;
        }
        return 96m;
    }

    static string LoadText(string path) => File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "";

    static (string token, long? adminChatId, List<string> admins) LoadTelegramCfg(string path)
    {
        string token = "";
        long? chatId = null;
        var admins = new List<string>();

        if (!File.Exists(path)) return (token, chatId, admins);
        foreach (var l in File.ReadAllLines(path, Encoding.UTF8).Select(s => s.Trim()))
        {
            if (l.Length == 0 || l.StartsWith("#")) continue;
            var kv = l.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;

            var key = kv[0].ToUpperInvariant();
            var val = kv[1];

            switch (key)
            {
                case "BOT_TOKEN": token = val; break;
                case "ADMIN_CHAT_ID": if (long.TryParse(val, out var id)) chatId = id; break;
                case "ADMIN":
                    foreach (var a in val.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        admins.Add(a);
                    break;
            }
        }
        return (token, chatId, admins);
    }

    static Web2MCfg LoadWeb2M(string path)
    {
        if (!File.Exists(path)) return new Web2MCfg();
        bool enabled = false; int port = 8088; string p = "web2m"; string sec = "";
        foreach (var l in File.ReadAllLines(path, Encoding.UTF8).Select(s => s.Trim()))
        {
            if (l.Length == 0 || l.StartsWith("#")) continue;
            var kv = l.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;
            switch (kv[0].ToUpperInvariant())
            {
                case "ENABLED": enabled = kv[1].Equals("1") || kv[1].Equals("true", StringComparison.OrdinalIgnoreCase); break;
                case "PORT": int.TryParse(kv[1], out port); break;
                case "PATH": p = kv[1]; break;
                case "SECRET": sec = kv[1]; break;
            }
        }
        return new Web2MCfg { Enabled = enabled, Port = port, Path = p, Secret = sec };
    }
}
