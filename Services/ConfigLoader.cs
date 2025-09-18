using System;
using System.Collections.Generic;
using System.IO;
using ToolMuaban.Models;

namespace ToolMuaban
{
    public static class ConfigLoader
    {
        public static List<AccountConfig> LoadAccounts(string filePath)
        {
            var accounts = new List<AccountConfig>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Không tìm thấy {filePath}");
                return accounts;
            }

            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var acc = new AccountConfig
                    {
                        Server = Extract(line, "SV"),
                        Username = Extract(line, "TK"),
                        Password = Extract(line, "MK"),
                        Character = Extract(line, "NV"),
                        Map = Extract(line, "MAP"),
                        Location = Extract(line, "LOCATION"),
                        Atc = Extract(line, "ATC"),
                        Sms = Extract(line, "SMS"),
                        Ktg = Extract(line, "KTG"),
                        Spam = Extract(line, "SPAM")
                    };

                    accounts.Add(acc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Không thể parse dòng: {line}. Lỗi: {ex.Message}");
                }
            }

            return accounts;
        }

        private static string Extract(string input, string tag)
        {
            string openTag = $"[{tag}]";
            string closeTag = $"[/{tag}]";

            int start = input.IndexOf(openTag);
            int end = input.IndexOf(closeTag);

            if (start == -1 || end == -1 || end <= start)
                return string.Empty;

            start += openTag.Length;
            return input.Substring(start, end - start).Trim();
        }
    }
}
