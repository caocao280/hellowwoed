using System.Text;

namespace ToolMuaban
{
    public static class PacketLogger
    {
        private static readonly object _lock = new();
        private const string LogFile = "packets.log";

        public static void Log(string direction, byte[] buffer, int length)
        {
            var hex = BitConverter.ToString(buffer, 0, length).Replace("-", " ");
            var text = Encoding.ASCII.GetString(buffer, 0, length);

            lock (_lock)
            {
                File.AppendAllText(LogFile,
                    $"[{DateTime.Now:HH:mm:ss}] {direction} | HEX: {hex} | ASCII: {text}\n");
            }

            Console.WriteLine($"[Packet {direction}] {length} bytes");
        }
    }
}
