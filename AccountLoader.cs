using System;
using System.Collections.Generic;
using System.IO;
using ToolMuaban.Models;

namespace ToolMuaban
{
    public static class AccountLoader
    {
        public static List<AccountConfig> LoadAccounts(string filePath)
        {
            var accounts = new List<AccountConfig>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Không tìm thấy file {filePath}");
                return accounts;
            }

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var acc = ParseAccount(line);
                    accounts.Add(acc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Không thể parse account từ dòng: {line}");
                    Console.WriteLine(ex.Message);
                }
            }

            return accounts;
        }

        private static AccountConfig ParseAccount(string line)
        {
            return new AccountConfig
            {
                Server = Extract(line, "[SV]", "[/SV]"),
                Username = Extract(line, "[TK]", "[/TK]"),
                Password = Extract(line, "[MK]", "[/MK]"),
                Character = Extract(line, "[NV]", "[/NV]"),
                Map = Extract(line, "[MAP]", "[/MAP]"),
                Location = Extract(line, "[LOCATION]", "[/LOCATION]"),
                Atc = Extract(line, "[ATC]", "[/ATC]"),
                Sms = Extract(line, "[SMS]", "[/SMS]"),
                Ktg = Extract(line, "[KTG]", "[/KTG]"),
                Spam = Extract(line, "[SPAM]", "[/SPAM]")
            };
        }

        private static string Extract(string text, string start, string end)
        {
            int i1 = text.IndexOf(start) + start.Length;
            int i2 = text.IndexOf(end);
            if (i1 < start.Length || i2 < 0 || i2 <= i1) return "";
            return text.Substring(i1, i2 - i1);
        }

        public static void PrintAccounts(List<AccountConfig> accounts)
        {
            Console.WriteLine("====== DANH SÁCH TÀI KHOẢN ======");
            foreach (var acc in accounts)
            {
                Console.WriteLine($"- {acc.Username} | Map: {acc.Map} | Location: {acc.Location}");
            }
            Console.WriteLine("================================");
        }
    }
}
