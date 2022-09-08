using Chip8Emu;
using System.Diagnostics;
using System.Media;

public class Program
{
    static byte[] memory = new byte[4096];
    static byte[] displayMemory = new byte[64 * 32];
    static Stack<short> stack = new Stack<short>(32);
    static short[] registers = new short[16];
    static short iRegister = 0;
    static short dT = short.MaxValue, sT = short.MaxValue;
    static short pc = 0x200-2;

    static float targetKHz = 1f;

    static bool slowdown = false;
    static int slowdownMs = 100;

    static Random rnd = new();

    public static Dictionary<ConsoleKey, byte> KeyMap = new()
    {
        { ConsoleKey.D1, 1 }, { ConsoleKey.D2, 2 }, { ConsoleKey.D3, 3 }, { ConsoleKey.D4, 0xC },
        { ConsoleKey.Q, 4 }, { ConsoleKey.W, 5 }, { ConsoleKey.E, 6 }, { ConsoleKey.R, 0xD },
        { ConsoleKey.A, 7 }, { ConsoleKey.S, 8 }, { ConsoleKey.D, 9 }, { ConsoleKey.F, 0xE },
        { ConsoleKey.Y, 0xA }, { ConsoleKey.X, 0 }, { ConsoleKey.C, 0xB }, { ConsoleKey.V, 0xF }, // CHANGE MOST LEFT TO Z IF UR ON QWERTY
    };
    
    public static void Main(string[] args)
    {
        Console.WriteLine("Path to execute");
        string path = Console.ReadLine();
        
        if(!File.Exists(path))
        {
            Console.WriteLine("file not found!");
            return;
        }

        // Init display
        Console.SetWindowSize(64, 40); // 8 additional lines for debug
        Console.SetBufferSize(64, Console.BufferHeight);
        Console.Clear();

        // Show first debug stuff
        WriteDbg(0, "Current Task: Initiliazing");

        // Initiliaze font data
        Buffer.BlockCopy(Font.Data, 0, memory, 0x050, Font.Data.Length);

        byte[] prg = File.ReadAllBytes(path);
        Buffer.BlockCopy(prg, 0, memory, 0x200, prg.Length);

        StartTimerThread();
        StartMainThread();
    }

    public async static void StartMainThread()
    {
        Console.CursorVisible = false;
        new Thread(async () =>
        {
            Stopwatch sw = new();
            Stopwatch dbg = new();

            int i = 0;

            while (true)
            {
                double dur = (slowdown ? 1 : sw.Elapsed.TotalMilliseconds * targetKHz);

                for(int rep = 0; rep < dur; rep++)
                {
                    i++;

                    byte b0 = memory[pc];
                    byte b1 = memory[pc+1];
                    byte nibble0 = (byte)((b0 & 0xF0) >> 4);
                    byte nibble1 = (byte)(b0 & 0x0F);
                    byte nibble2 = (byte)((b1 & 0xF0) >> 4);
                    byte nibble3 = (byte)(b1 & 0x0F);

                    byte x = nibble1;
                    byte y = nibble2;
                    byte n = nibble3;
                    byte nn = b1; // yes, ik, its not performant but easier to read
                    short nnn = (short)((nibble1 << 4 | nibble2) << 4 | nibble3);

                    pc += 2;

                    unchecked // We can freely overflow as we want
                    {
                        if (nibble0 == 0x0 && nibble1 == 0x0 && nibble2 == 0xE && nibble3 == 0x0)
                        {
                            Console.Clear();
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
                            Console.SetCursorPosition(Math.Min(Console.WindowWidth, (int)registers[x]), Math.Min(Console.WindowWidth, (int)registers[y]));
                            registers[0xF] = 0;

                            Debug.WriteLine(HexOutput(memory, iRegister, 32));

                            for (int n3I = 0; n3I < nibble3; n3I++)
                            {
                                byte data = memory[iRegister + n3I];

                                for (int k = 0; k < 8; k++)
                                {
                                    int xpos = registers[x] + k;
                                    int ypos = registers[y] + n3I;
                                    int offset = (ypos * 64) + xpos;

                                    if (xpos >= Console.WindowWidth || ypos > Console.WindowHeight) continue;

                                    if ((data & (0x80 >> k)) != 0)
                                    {
                                        if (displayMemory[registers[x] + k + ((registers[y] + n3I) * 64)] == 1)
                                        {
                                            registers[0xF] = 1;
                                        }
                                        displayMemory[registers[x] + k + ((registers[y] + n3I) * 64)] ^= 1;

                                        if(displayMemory[registers[x] + k + ((registers[y] + n3I) * 64)] == 1)
                                        {
                                            Console.SetCursorPosition(registers[x] + k, registers[y] + n3I);
                                            Console.Write('█');
                                        }else
                                        {
                                            Console.SetCursorPosition(registers[x] + k, registers[y] + n3I);
                                            Console.Write(' ');
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
                            }else
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
                            if(registers[x] < 0) registers[x] = (short)(256 + registers[x]);
                            if(registers[x] > 255) registers[x] -= 256;
                        }
                        else if (nibble0 == 0x8 && nibble3 == 0xE)
                        {
                            registers[0xF] = (short)((registers[x] >> 7) & 1);
                            //registers[x] = (short)(registers[y]);
                            registers[x] <<= 1;
                            if (registers[x] < 0) registers[x] = (short)(256 + registers[x]);
                            if (registers[x] > 255) registers[x] -= 256;
                        }
                        else if(nibble0 == 0xB)
                        {
                            pc = (short)(nnn + registers[0]);
                        }else if(nibble0 == 0xC)
                        {
                            byte rand = (byte)rnd.Next(255);
                            registers[x] = (short)(rand & nn);
                        }
                        else if (nibble0 == 0xE && nibble2 == 0x9 && nibble3 == 0xE)
                        {
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);
                                if (key.Key == KeyMap.FirstOrDefault(el => el.Value == registers[x], new(ConsoleKey.Applications, 0x0)).Key)
                                {
                                    pc += 2;
                                }
                            }
                        }
                        else if (nibble0 == 0xE && nibble2 == 0xA && nibble3 == 0x1)
                        {
                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(true);
                                if (key.Key != KeyMap.FirstOrDefault(el => el.Value == registers[x], new(ConsoleKey.Applications, 0x0)).Key)
                                {
                                    pc += 2;
                                }
                            }else
                            {
                                pc += 2;
                            }
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
                                var key = Console.ReadKey(true);
                                if (!KeyMap.ContainsKey(key.Key)) continue;
                                registers[x] = KeyMap[key.Key];
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
                        Console.Title = "Chip8Emu | TimePerKHz: " + (dbg.Elapsed.TotalMilliseconds).ToString("F2") + "ms/KHz | Speed: " + (dbg.Elapsed.TotalMilliseconds).ToString("F2") + "KHz";
                        dbg.Restart();
                    }
                }

                sw.Restart();
                Thread.Sleep((slowdown ? slowdownMs : 1));
            }
        }).Start();
    }

    static string HexOutput(byte[] arr, int off, int sz)
    {
        string o = "";

        for(int i = off; i < off+sz; i++)
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
        new Thread(() =>
        {
                while (true)
                {
                    if (sT > 0) sT--;
                    if (dT > 0) dT--;

                    if (sT == 0)
                    {
                        SystemSounds.Beep.Play();
                    }

                    Thread.Sleep(1000 / 60);
                }
        }).Start();
    }

    private static void WriteDbg(int lineOffset, string str)
    {
        if (lineOffset > 8) return;

        //(int ccx, int ccy) = Console.GetCursorPosition();
        Console.SetCursorPosition(0, 32+lineOffset);
        Console.Write(str.PadRight(64, ' '));
        //Console.SetCursorPosition(ccx, ccy);
    }
}