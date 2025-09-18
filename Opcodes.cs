namespace ToolMuaban;

public static class Opcodes
{
    // Tham chiếu từ source decompiled bạn đã gửi
    public const sbyte CHAT_NORMAL = unchecked((sbyte)-21);
    public const sbyte CHAT_GROUP = unchecked((sbyte)-20);
    public const sbyte CHAT_PRIVATE = unchecked((sbyte)-22);
    public const sbyte CHAT_SYS = unchecked((sbyte)-19);
    public const sbyte CHAT_BROAD = unchecked((sbyte)-25);

    public const byte MOVE = 52;   // client->server
    public const byte LOGIN_USER = 118;  // (acc,pass) theo case 118
    public const byte TRADE = 125;  // client->server
    // thêm các opcode server->client nếu cần parse

    // Trade actions (theo mô tả)
    public const byte TRADE_INVITE = 0; // [int targetId]
    public const byte TRADE_ADD_XU = 1; // [int xu]
    public const byte TRADE_ACCEPT = 2; // [bool]
    public const byte TRADE_CANCEL = 3; // optional
}
