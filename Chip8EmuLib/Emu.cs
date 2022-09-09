using System.Diagnostics;

namespace Chip8EmuLib
{
    public class Emu
    {
        public static byte[] memory = new byte[4096];
        public static byte[] displayMemory = new byte[64 * 32];
        public static Stack<short> stack = new Stack<short>(32);
        public static short[] registers = new short[16];
        public static short iRegister = 0;
        public static short dT = short.MaxValue, sT = short.MaxValue;
        public static short pc = 0x200 - 2;
        public static byte byte0, byte1;

        public static int renderSizeX = 64, renderSizeY = 32;

        public static float targetKHz = 1f;
        public static bool slowdown = false;
        public static int slowdownMs = 100;
        
        public static Random rnd = new();

        public static Action<int, int> ApiSetRenderSize = null;
        public static Action ApiBeep = null;
        public static Action<string> ApiSetTitle = null;
        public static Action<int, int, bool> ApiSetPixel = null;
        public static Func<ConsoleKey, bool> ApiIsKeyPressed = null;
        public static Func<ConsoleKey> ApiWaitForKey = null;
        public static Action ApiClearScreen = null;

        public static Thread MainThread, TimerThread;

        public static bool Exit = false, Pause = false;

        public static Dictionary<ConsoleKey, byte> KeyMap = new()
        {
            { ConsoleKey.D1, 1 },
            { ConsoleKey.D2, 2 },
            { ConsoleKey.D3, 3 },
            { ConsoleKey.D4, 0xC },
            { ConsoleKey.Q, 4 },
            { ConsoleKey.W, 5 },
            { ConsoleKey.E, 6 },
            { ConsoleKey.R, 0xD },
            { ConsoleKey.A, 7 },
            { ConsoleKey.S, 8 },
            { ConsoleKey.D, 9 },
            { ConsoleKey.F, 0xE },
            { ConsoleKey.Y, 0xA }, // CHANGE TO Z IF UR ON QWERTY
            { ConsoleKey.X, 0 },
            { ConsoleKey.C, 0xB },
            { ConsoleKey.V, 0xF }, 
        };

        public static void Main(string path)
        {
            if (ApiClearScreen == null || ApiSetRenderSize == null || ApiBeep == null || ApiSetTitle == null || ApiSetPixel == null || ApiIsKeyPressed == null || ApiWaitForKey == null)
            {
                throw new Exception("One or more required methods have not been defined!");
            }

            if (!File.Exists(path))
            {
                throw new Exception("File not found!");
                return;
            }

            // Init display
            renderSizeX = 64;
            renderSizeY = 32;
            ApiSetRenderSize(renderSizeX, renderSizeY);

            // Show first debug stuff
            //WriteDbg(0, "Current Task: Initiliazing");

            // Initiliaze font data
            Buffer.BlockCopy(Font.Data, 0, memory, 0x050, Font.Data.Length);

            byte[] prg = File.ReadAllBytes(path);
            Buffer.BlockCopy(prg, 0, memory, 0x200, prg.Length);

            StartTimerThread();
            StartMainThread();
        }

        public async static void StartMainThread()
        {
            //Console.CursorVisible = false;
            MainThread = new Thread(async () =>
            {
                Stopwatch sw = new();
                Stopwatch dbg = new();

                int i = 0;

                while (true)
                {
                    if (Exit) break;
                    if (Pause)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    double dur = (slowdown ? 1 : sw.Elapsed.TotalMilliseconds * targetKHz);

                    for (int rep = 0; rep < dur; rep++)
                    {
                        i++;

                        byte0 = memory[pc]; // Those are public for debugging purposes
                        byte1 = memory[pc + 1]; // Those are public for debugging purposes
                        byte nibble0 = (byte)((byte0 & 0xF0) >> 4);
                        byte nibble1 = (byte)(byte0 & 0x0F);
                        byte nibble2 = (byte)((byte1 & 0xF0) >> 4);
                        byte nibble3 = (byte)(byte1 & 0x0F);

                        byte x = nibble1;
                        byte y = nibble2;
                        byte n = nibble3;
                        byte nn = byte1; // yes, ik, its not performant but easier to read
                        short nnn = (short)((nibble1 << 4 | nibble2) << 4 | nibble3);

                        pc += 2;

                        unchecked // We can freely overflow as we want
                        {
                            if (nibble0 == 0x0 && nibble1 == 0x0 && nibble2 == 0xE && nibble3 == 0x0)
                            {
                                ApiClearScreen();
                            }
                            else if (nibble0 == 0x1)
                            {
                                pc = nnn;
                            }
                            else if (nibble0 == 0x6)
                            {
                                registers[x] = nn;
                            }

                            else if (nibble0 == 0xA)
                            {
                                iRegister = nnn;
                            }
                            else if (nibble0 == 0xD)
                            {
                                //Console.SetCursorPosition(Math.Min(Console.WindowWidth, (int)registers[x]), Math.Min(Console.WindowWidth, (int)registers[y]));
                                registers[0xF] = 0;

                                //Debug.WriteLine(HexOutput(memory, iRegister, 32));

                                for (int n3I = 0; n3I < nibble3; n3I++)
                                {
                                    byte data = memory[iRegister + n3I];

                                    for (int k = 0; k < 8; k++)
                                    {
                                        int xpos = registers[x] + k;
                                        int ypos = registers[y] + n3I;
                                        int offset = (ypos * 64) + xpos;

                                        if (xpos >= renderSizeX || ypos > renderSizeY) continue;

                                        if ((data & (0x80 >> k)) != 0)
                                        {
                                            if (displayMemory[registers[x] + k + ((registers[y] + n3I) * 64)] == 1)
                                            {
                                                registers[0xF] = 1;
                                            }
                                            displayMemory[registers[x] + k + ((registers[y] + n3I) * 64)] ^= 1;

                                            if (displayMemory[registers[x] + k + ((registers[y] + n3I) * 64)] == 1)
                                            {
                                                //Console.SetCursorPosition(registers[x] + k, registers[y] + n3I);
                                                //Console.Write('█');
                                                ApiSetPixel(registers[x] + k, registers[y] + n3I, true);
                                            }
                                            else
                                            {
                                                //Console.SetCursorPosition(registers[x] + k, registers[y] + n3I);
                                                //Console.Write(' ');
                                                ApiSetPixel(registers[x] + k, registers[y] + n3I, false);
                                            }
                                        };
                                    }
                                }
                            }
                            else if (nibble0 == 0x0 && nibble1 == 0x0 && nibble2 == 0xE && nibble2 == 0xE)
                            {
                                short addr = stack.Pop();
                                pc = addr;
                            }
                            else if (nibble0 == 0x2)
                            {
                                stack.Push(pc);
                                pc = nnn;
                            }
                            else if (nibble0 == 0x3)
                            {
                                if (registers[x] == nn) pc += 2;
                            }
                            else if (nibble0 == 0x4)
                            {
                                if (registers[x] != nn) pc += 2;
                            }
                            else if (nibble0 == 0x5 && nibble3 == 0x0)
                            {
                                if (registers[x] == registers[y]) pc += 2;
                            }
                            else if (nibble0 == 0x9 && nibble3 == 0x0)
                            {
                                if (registers[x] != registers[y]) pc += 2;
                            }
                            else if (nibble0 == 0x6)
                            {
                                registers[x] = nn;
                            }
                            else if (nibble0 == 0x7)
                            {
                                registers[x] += nn;
                                if (registers[x] > 255) registers[x] -= 256;
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x0)
                            {
                                registers[x] = registers[y];
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x1)
                            {
                                registers[x] |= registers[y];
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x2)
                            {
                                registers[x] &= registers[y];
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x3)
                            {
                                registers[x] ^= registers[y];
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x4)
                            {
                                var difference = byte.MaxValue - registers[x];

                                if ((difference >= registers[y])) registers[0xF] = 0;
                                else registers[0xF] = 1;

                                registers[x] += registers[y];
                                if (registers[x] > 255) registers[x] -= 256;
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x5)
                            {
                                if (registers[x] > registers[y])
                                {
                                    registers[0xF] = 1;
                                }
                                else
                                {
                                    registers[0xF] = 0;
                                }

                                registers[x] = (short)(registers[x] - registers[y]);
                                if (registers[x] < 0) registers[x] = (short)(256 + registers[x]);
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x7)
                            {
                                if (registers[y] > registers[x])
                                {
                                    registers[0xF] = 1;
                                }
                                else
                                {
                                    registers[0xF] = 0;
                                }

                                registers[x] = (short)(registers[y] - registers[x]);
                                if (registers[x] < 0) registers[x] = (short)(256 + registers[x]);
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0x6)
                            {
                                registers[0xF] = (short)((registers[x] >> 0) & 1);
                                //registers[x] = (short)(registers[y]);
                                registers[x] >>= 1;
                                if (registers[x] < 0) registers[x] = (short)(256 + registers[x]);
                                if (registers[x] > 255) registers[x] -= 256;
                            }
                            else if (nibble0 == 0x8 && nibble3 == 0xE)
                            {
                                registers[0xF] = (short)((registers[x] >> 7) & 1);
                                //registers[x] = (short)(registers[y]);
                                registers[x] <<= 1;
                                if (registers[x] < 0) registers[x] = (short)(256 + registers[x]);
                                if (registers[x] > 255) registers[x] -= 256;
                            }
                            else if (nibble0 == 0xB)
                            {
                                pc = (short)(nnn + registers[0]);
                            }
                            else if (nibble0 == 0xC)
                            {
                                byte rand = (byte)rnd.Next(255);
                                registers[x] = (short)(rand & nn);
                            }
                            else if (nibble0 == 0xE && nibble2 == 0x9 && nibble3 == 0xE)
                            {
                                var targetKey = KeyMap.FirstOrDefault(el => el.Value == registers[x], new(ConsoleKey.Applications, 0x0)).Key;

                                if (ApiIsKeyPressed(targetKey)) pc += 2;
                            }
                            else if (nibble0 == 0xE && nibble2 == 0xA && nibble3 == 0x1)
                            {
                                /*if (Console.KeyAvailable)
                                {
                                    var key = Console.ReadKey(true);
                                    if (key.Key != KeyMap.FirstOrDefault(el => el.Value == registers[x], new(ConsoleKey.Applications, 0x0)).Key)
                                    {
                                        pc += 2;
                                    }
                                }
                                else
                                {
                                    pc += 2;
                                }*/

                                var targetKey = KeyMap.FirstOrDefault(el => el.Value == registers[x], new(ConsoleKey.Applications, 0x0)).Key;

                                if (!ApiIsKeyPressed(targetKey)) pc += 2;
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x0 && nibble3 == 0x7)
                            {
                                registers[x] = dT;
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x1 && nibble3 == 0x5)
                            {
                                dT = registers[x];
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x1 && nibble3 == 0x8)
                            {
                                sT = registers[x];
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x1 && nibble3 == 0xE)
                            {
                                iRegister += registers[x];
                                if (i > 0x1000) registers[0xF] = 1;
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x0 && nibble3 == 0xA)
                            {
                                while (true)
                                {
                                    var key = ApiWaitForKey();
                                    if (!KeyMap.ContainsKey(key)) continue;
                                    registers[x] = KeyMap[key];
                                    break;
                                }
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x2 && nibble3 == 0x9)
                            {
                                byte nibbleFromReg = (byte)((registers[x] >> 0) & 0x0F);
                                iRegister = (short)(0x50 + (nibbleFromReg * 5));
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x3 && nibble3 == 0x3)
                            {
                                memory[iRegister] = (byte)(registers[x] / 100);
                                memory[iRegister + 1] = (byte)((registers[x] / 10) % 10);
                                memory[iRegister + 2] = (byte)(registers[x] % 10);
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x5 && nibble3 == 0x5)
                            {
                                for (int ix = 0; ix < x + 1; ix++)
                                {
                                    memory[iRegister + ix] = (byte)registers[ix];
                                }
                            }
                            else if (nibble0 == 0xF && nibble2 == 0x6 && nibble3 == 0x5)
                            {
                                for (int ix = 0; ix < x + 1; ix++)
                                {
                                    registers[ix] = memory[iRegister + ix];
                                }
                            }
                        }

                        //if (pc % 2 == 1) pc += 1;

                        //WriteDbg(1, "PC: " + pc + "    B0: " + b0 + "    B1: " + b1);
                        //WriteDbg(2, "Regs: I " + iRegister + "  0 " + registers[0] + "  1 " + registers[1] + "  2 " + registers[2] + "  3 " + registers[3]);
                        //WriteDbg(3, "      4 " + registers[4] + "  5 " + registers[5] + "  6 " + registers[6] + "  7 " + registers[7] + "  8 " + registers[8] + "  9 " + registers[9]);
                        //WriteDbg(4, "      A " + registers[10] + "  B " + registers[11] + "  C " + registers[12] + "  D " + registers[13] + "  E " + registers[14] + "  F " + registers[15]);
                        //WriteDbg(5, "NEAR PC: " + HexOutput(memory, pc-2, 16));

                        if (i % (1000) == 0) // 1 k ticks passed == 1 khz
                        {
                            ApiSetTitle("Chip8Emu | TimePerKHz: " + (dbg.Elapsed.TotalMilliseconds).ToString("F2") + "ms/KHz | Speed: " + (1000 / dbg.Elapsed.TotalMilliseconds).ToString("F2") + "KHz");
                            dbg.Restart();
                        }
                    }

                    sw.Restart();
                    Thread.Sleep((slowdown ? slowdownMs : (targetKHz < 1 ? (int)(1 / targetKHz) : 1)));
                }
            });

            MainThread.Start();
        }

        public static string HexOutput(byte[] arr, int off, int sz)
        {
            string o = "";

            for (int i = off; i < off + sz; i++)
            {
                o += Convert.ToString(arr[i], 16) + " ";
            }

            return o;
        }

        static void SetTask(string tsk)
        {
            WriteDbg(0, ("Current Task: " + tsk).PadRight(64, ' '));
        }

        public static void StartTimerThread()
        {
            TimerThread = new Thread(() =>
            {
                while (true)
                {
                    if (Exit) break;
                    if (Pause)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    if (sT > 0) sT--;
                    if (dT > 0) dT--;

                    if (sT == 0)
                    {
                        ApiBeep();
                        //SystemSounds.Beep.Play();
                    }

                    Thread.Sleep(1000 / 60);
                }
            });

            TimerThread.Start();
        }

        private static void WriteDbg(int lineOffset, string str)
        {
            if (lineOffset > 8) return;

            //(int ccx, int ccy) = Console.GetCursorPosition();
            Console.SetCursorPosition(0, 32 + lineOffset);
            Console.Write(str.PadRight(64, ' '));
            //Console.SetCursorPosition(ccx, ccy);
        }
    }
}