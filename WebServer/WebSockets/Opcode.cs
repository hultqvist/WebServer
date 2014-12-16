using System;

namespace SilentOrbit.WebSockets
{
    /// <summary>
    /// Websocket frame Opcodes
    /// </summary>
    public enum Opcode
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,

        //Reserved 3 - 7

        Close = 8,
        Ping = 9,
        Pong = 10,

        //Reserved 11 - 15
    }
}

