using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chip8EmuGUI
{
    internal class ConsoleKeyMap
    {
        public static Dictionary<ConsoleKey, Keys> KeyMap { get; } = new()
        {
            { ConsoleKey.A, Keys.A },
            { ConsoleKey.B, Keys.B },
            { ConsoleKey.C, Keys.C },
            { ConsoleKey.D, Keys.D },
            { ConsoleKey.E, Keys.E },
            { ConsoleKey.F, Keys.F },
            { ConsoleKey.G, Keys.G },
            { ConsoleKey.H, Keys.H },
            { ConsoleKey.I, Keys.I },
            { ConsoleKey.J, Keys.J },
            { ConsoleKey.K, Keys.K },
            { ConsoleKey.L, Keys.L },
            { ConsoleKey.M, Keys.M },
            { ConsoleKey.N, Keys.N },
            { ConsoleKey.O, Keys.O },
            { ConsoleKey.P, Keys.P },
            { ConsoleKey.Q, Keys.Q },
            { ConsoleKey.R, Keys.R },
            { ConsoleKey.S, Keys.S },
            { ConsoleKey.T, Keys.T },
            { ConsoleKey.U, Keys.U },
            { ConsoleKey.V, Keys.V },
            { ConsoleKey.W, Keys.W },
            { ConsoleKey.X, Keys.X },
            { ConsoleKey.Y, Keys.Y },
            { ConsoleKey.Z, Keys.Z },
            { ConsoleKey.D0, Keys.D0 },
            { ConsoleKey.D1, Keys.D1 },
            { ConsoleKey.D2, Keys.D2 },
            { ConsoleKey.D3, Keys.D3 },
            { ConsoleKey.D4, Keys.D4 },
            { ConsoleKey.D5, Keys.D5 },
            { ConsoleKey.D6, Keys.D6 },
            { ConsoleKey.D7, Keys.D7 },
            { ConsoleKey.D8, Keys.D8 },
            { ConsoleKey.D9, Keys.D9 },
            { ConsoleKey.Escape, Keys.Escape },
            { ConsoleKey.Enter, Keys.Enter },
        };
    }
}
