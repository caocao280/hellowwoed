using System;
using System.Collections.Generic;

namespace ToolMuaban
{
    public static class PacketParser
    {
        public static List<ParsedPacket> ParseGrouped(byte[] raw)
        {
            var packets = new List<ParsedPacket>();
            int i = 0;

            while (i < raw.Length)
            {
                byte opcode = raw[i];
                int payloadLength = Math.Min(15, raw.Length - (i + 1));
                var payload = new List<byte>();

                for (int j = 0; j < payloadLength; j++)
                {
                    payload.Add(raw[i + 1 + j]);
                }

                packets.Add(new ParsedPacket
                {
                    Offset = i,
                    Opcode = opcode,
                    Payload = payload
                });

                i += 1 + payloadLength;
            }

            return packets;
        }
    }
}
