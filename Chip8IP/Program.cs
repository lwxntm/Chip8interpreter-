using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using SDL2;

namespace Chip8IP
{
    class Program
    {
        static void Main()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_EVERYTHING) < 0)
            {
                Console.WriteLine("SDL init failed!");
                return;
            }
            IntPtr window = SDL.SDL_CreateWindow("Chip-8", 200, 200, 64 * 10, 32 * 10, SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE);
            if (window == IntPtr.Zero)
            {
                Console.WriteLine("Window init failed!");
            }
            IntPtr render = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);
            if (render == IntPtr.Zero)
            {
                Console.WriteLine("render init failed!");
            }

            CPU cpu = new CPU();
            using BinaryReader reader = new BinaryReader(new FileStream("Pong 2 (Pong hack) [David Winter, 1997].ch8", FileMode.Open));
            List<byte> Program = new List<byte>();
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                Program.Add(reader.ReadByte());
            }
            cpu.LoadProgram(Program.ToArray());

            SDL.SDL_Event e;

            bool running = true;

            IntPtr sdlSurface, sdlTexture = IntPtr.Zero;

            while (running)
            {
                while (SDL.SDL_PollEvent(out e) != 0)
                {

                    if (e.type == SDL.SDL_EventType.SDL_QUIT) running = false;
                    if (e.type == SDL.SDL_EventType.SDL_KEYDOWN)
                    {
                        var key = CastInput(e.key.keysym.sym);
                        if (key == -1) break;
                        cpu.key_m[key] = true;
                        break;
                    }
                    if (e.type == SDL.SDL_EventType.SDL_KEYUP)
                    {
                        var key = CastInput(e.key.keysym.sym);
                        if (key == -1) break;
                        cpu.key_m[key] = false;
                        break;
                    }

                };

                var displayHander = GCHandle.Alloc(cpu.Display, GCHandleType.Pinned);


                if (sdlTexture != IntPtr.Zero) SDL.SDL_DestroyTexture(sdlTexture);

                sdlSurface = SDL.SDL_CreateRGBSurfaceFrom(displayHander.AddrOfPinnedObject(), 64, 32, 32, 64 * 4,
                    0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000);
                sdlTexture = SDL.SDL_CreateTextureFromSurface(render, sdlSurface);

                displayHander.Free();

                //在把sdlTexture复制到render里之前，先清空 render里的东西.
                SDL.SDL_RenderClear(render);
                SDL.SDL_RenderCopy(render, sdlTexture, IntPtr.Zero, IntPtr.Zero);
                SDL.SDL_RenderPresent(render);
                Thread.Sleep(1);
                // try { 
                cpu.ExecuteOpcode();
                //  cpu.DrawDisplay();

                //  } catch (Exception e) { Console.WriteLine(e.ToString()); }

            }
            SDL.SDL_DestroyRenderer(render);
            SDL.SDL_DestroyWindow(window);
        }
        private static int KeyCodeToKey(int keycode)
        {
            int keyIndex = 0;
            if (keycode < 58) keyIndex = keycode - 48;
            else keyIndex = keycode - 87;

            return keyIndex;
        }
        public static int CastInput(SDL.SDL_Keycode inp)
        {
            switch (inp)
            {
                case SDL.SDL_Keycode.SDLK_1:
                    return 1;
                case SDL.SDL_Keycode.SDLK_2:
                    return 2;
                case SDL.SDL_Keycode.SDLK_3:
                    return 3;
                case SDL.SDL_Keycode.SDLK_4:
                    return 0xc;
                case SDL.SDL_Keycode.SDLK_q:
                    return 4;
                case SDL.SDL_Keycode.SDLK_w:
                    return 5;
                case SDL.SDL_Keycode.SDLK_e:
                    return 6;
                case SDL.SDL_Keycode.SDLK_r:
                    return 0xd;
                case SDL.SDL_Keycode.SDLK_a:
                    return 7;
                case SDL.SDL_Keycode.SDLK_s:
                    return 8;
                case SDL.SDL_Keycode.SDLK_d:
                    return 9;
                case SDL.SDL_Keycode.SDLK_f:
                    return 0xe;
                case SDL.SDL_Keycode.SDLK_z:
                    return 0xa;
                case SDL.SDL_Keycode.SDLK_x:
                    return 0;
                case SDL.SDL_Keycode.SDLK_c:
                    return 0xb;
                case SDL.SDL_Keycode.SDLK_v:
                    return 0xf;
                default:
                    return -1;
            }
        }
    }
    public class CPU
    {
        public byte[] RAM = new byte[4096];
        public byte[] V = new byte[16];
        public ushort PC = 0;
        public ushort I = 0;
        //  public ushort[] stack=new ushort[24];
        public Stack<ushort> Stack = new Stack<ushort>();
        public byte DelayTimer;
        public byte SoundTimer;
        public ushort KeyBoard;
        public bool[] key_m = new bool[16] { false, false , false , false ,
        false, false , false , false ,
        false, false , false , false ,
        false, false , false , false };
        public uint[] Display = new uint[64 * 32];

        private Random generator = new Random(Environment.TickCount);

        public bool WaitingForKeyInput = false;

        private void InitializeFont()
        {
            byte[] characters = new byte[] { 0xF0, 0x90, 0x90, 0x90, 0xF0, 0x20, 0x60, 0x20, 0x20, 0x70, 0xF0, 0x10, 0xF0, 0x80, 0xF0, 0xF0, 0x10, 0xF0, 0x10, 0xF0, 0x90, 0x90, 0xF0, 0x10, 0x10, 0xF0, 0x80, 0xF0, 0x10, 0xF0, 0xF0, 0x80, 0xF0, 0x90, 0xF0, 0xF0, 0x10, 0x20, 0x40, 0x40, 0xF0, 0x90, 0xF0, 0x90, 0xF0, 0xF0, 0x90, 0xF0, 0x10, 0xF0, 0xF0, 0x90, 0xF0, 0x90, 0x90, 0xE0, 0x90, 0xE0, 0x90, 0xE0, 0xF0, 0x80, 0x80, 0x80, 0xF0, 0xE0, 0x90, 0x90, 0x90, 0xE0, 0xF0, 0x80, 0xF0, 0x80, 0xF0, 0xF0, 0x80, 0xF0, 0x80, 0x80 };
            Array.Copy(characters, RAM, characters.Length);
        }

        public void LoadProgram(byte[] program)
        {
            //for (int i = 0; i < program.Length; i++)
            //{
            //    Console.WriteLine($"program:{i} : {program[i]:X4}");
            //}

            RAM = new byte[4096];
            InitializeFont();
            for (int i = 0; i < program.Length; i++)
            {
                RAM[512 + i] = program[i];
            }
            PC = 512;
        }
        public Stopwatch watch = new Stopwatch();

        public void ExecuteOpcode()
        {
            if (!watch.IsRunning) watch.Start();
            if (watch.ElapsedMilliseconds > 16)
            {
                if (DelayTimer > 0) DelayTimer--;
                if (SoundTimer > 0) SoundTimer--;
                watch.Restart();
            }

            //for (int i = 0; i < RAM.Length; i++)
            //{
            //    Console.WriteLine($"RAM:{i} : {RAM[i]:X4}");
            //}
            //从内存中读取当前需要执行的opcode。
            ushort opcode = (ushort)((RAM[PC] << 8) | RAM[PC + 1]);
            if (WaitingForKeyInput)
            {
                //  V[opcode & 0x0F00 >> 8] = KeyBoard;
                throw new Exception("not support yet");
                return;
            }
            var nibble = (ushort)(opcode & 0xF000);

            PC += 2;
            switch (nibble)
            {
                case 0x0000:
                    if (opcode == 0x00E0)
                    {
                        for (int i = 0; i < Display.Length; i++)
                        {
                            Display[i] = 0;
                        }
                    }
                    else if (opcode == 0x00EE)
                    {
                        PC = Stack.Pop();
                    }
                    else
                    {
                        throw new Exception($"unsupport opcode {opcode:X4}");
                    }
                    break;
                case 0x1000:
                    //Jumps to address NNN.
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x2000:
                    //Calls subroutine at NNN.
                    Stack.Push(PC);
                    PC = (ushort)(opcode & 0x0FFF);
                    break;
                case 0x3000:
                    //Skips the next instruction if VX equals NN.
                    //(Usually the next instruction is a jump to skip a code block);
                    if (V[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF)) PC += 2;
                    break;
                case 0x4000:
                    //Skips the next instruction if VX equals NN.
                    //(Usually the next instruction is a jump to skip a code block);
                    if (V[(opcode & 0x0F00) >> 8] != (opcode & 0x00FF)) PC += 2;
                    break;
                case 0x5000:
                    //5XY0	Cond	if (Vx == Vy)
                    //	Skips the next instruction if VX equals VY.
                    //	(Usually the next instruction is a jump to skip a code block);
                    if (V[(opcode & 0x0F00) >> 8] == V[(opcode & 0x00F0) >> 4]) PC += 2;
                    break;
                case 0x6000:
                    //6XNN	Const	Vx = N	Sets VX to NN.
                    V[(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF);
                    break;
                //7XNN	Const	Vx += N	Adds NN to VX. (Carry flag is not changed);
                case 0x7000:
                    V[(opcode & 0x0F00) >> 8] += (byte)(opcode & 0x00FF);
                    break;

                case 0x8000:
                    var vx = (opcode & 0x0F00) >> 8;
                    var vy = (opcode & 0x00F0) >> 4;

                    switch (opcode & 0x000F)
                    {
                        //8XY0	Assig	Vx = Vy	Sets VX to the value of VY.
                        case 0:
                            V[vx] = V[vy];
                            break;
                        //8XY1	BitOp	Vx |= Vy	Sets VX to VX or VY. (Bitwise OR operation);
                        case 1:
                            V[vx] |= V[vy];
                            break;
                        //8XY2	BitOp	Vx &= Vy	Sets VX to VX and VY. (Bitwise AND operation);
                        case 2:
                            V[vx] &= V[vy];
                            break;
                        //8XY3	BitOp	Vx ^= Vy	Sets VX to VX xor VY.
                        case 3:
                            V[vx] ^= V[vy];
                            break;
                        //8XY4	Math	Vx += Vy	Adds VY to VX.
                        //VF is set to 1 when there's a carry, and to 0 when there is not.
                        case 4:
                            V[15] = (byte)((V[vx] + V[vy]) > 255 ? 1 : 0);
                            V[vx] = (byte)((V[vx] + V[vy]) & 0x00FF);
                            break;
                        case 5:
                            V[15] = (byte)((V[vx] - V[vy]) < 0 ? 0 : 1);
                            V[vx] = (byte)((V[vx] - V[vy]) & 0x00FF);
                            break;
                        case 6:
                            V[15] = (byte)(V[vx] & 0x0001);
                            V[vx] >>= 1;
                            break;
                        case 7:
                            V[15] = (byte)(V[vy] > V[vx] ? 1 : 0);
                            V[vx] = (byte)((V[vy] - V[vx]) & 0x00FF);
                            break;
                        case 0xe:
                            V[15] = (byte)((V[vx] & 0x80) == 0x80 ? 1 : 0);
                            V[vx] <<= 1;
                            break;
                        default:
                            throw new Exception($"unsupport opcode {opcode:X4}");
                    }
                    break;
                case 0x9000:
                    if (V[(opcode & 0x0F00) >> 8] != V[(opcode & 0x00F0) >> 4]) PC += 2;
                    break;
                case 0xA000:
                    I = (ushort)(opcode & 0x0FFF); break;
                case 0xB000:
                    PC = (ushort)((opcode & 0x0FFF) + V[0]);
                    break;
                case 0xC000:
                    V[(opcode & 0x0F00) >> 8] = (byte)(generator.Next(0xFF) & (opcode & 0x00FF)); break;
                case 0xD000:
                    //Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision.
                    // The interpreter reads n bytes from memory, starting at the address stored inI.
                    // These bytes are then displayed as sprites on screen at coordinates(Vx, Vy).
                    // Sprites are XORed onto the existing screen.
                    // If this causes any pixels to beerased, VF is set to 1, otherwise it is set to 0.
                    // If the sprite is positionedso part of it is outside the coordinates of the display,
                    // it wraps around tothe opposite side of the screen. 
                    var xloc = V[(opcode & 0x0F00) >> 8];
                    var yloc = V[(opcode & 0x00F0) >> 4];
                    var n = opcode & 0x000F;
                    V[15] = 0;

                    //  bool displayDrity = false;

                    for (int i = 0; i < n; i++)
                    {
                        byte mem = RAM[I + i];
                        for (int j = 0; j < 8; j++)
                        {
                            //参见 
                            byte pixel = ((byte)((mem >> (7 - j)) & 0x01));
                            int index = xloc + j + (yloc + i) * 64;
                            if (index > 2047) continue;
                            if (pixel == 1 && Display[index] != 0) V[15] = 1;

                            /*

                            Console.SetCursorPosition(xloc + j, yloc + i);

                            // 如果异或完成之后 结果为1 那么就以下两种情况，原本为1，pixel为0或者原本为0，pixel 为1
                            if (Display[index] != 0 && pixel == 0)
                            {
                                Console.Write("*");
                            }
                            if (Display[index] == 0 && pixel == 1)
                            {
                                Console.Write("*");
                                displayDrity = true;
                            }
                            if (Display[index] != 0 && pixel == 1)
                            {
                                Console.Write(" ");
                                displayDrity = true;
                            }
                            if (Display[index] == 0 && pixel == 0)
                            {
                                Console.Write(" ");
                            }

                            */
                            //// if ((Display[index]==1 && pixel == 0) || (Display[index] == 0 && pixel == 1))
                            //{
                            //    Console.Write("*");
                            //}else Console.Write(" ");
                            //displayDrity = true;


                            //if (pixel == 1 && Display[index] == 1)
                            //{
                            //    Console.SetCursorPosition(xloc+j,yloc+i);
                            //    Console.Write("*");
                            //    displayDrity = true;
                            //}
                            //if (pixel == 1 && Display[index] == 0)
                            //{
                            //    Console.SetCursorPosition(xloc + j, yloc + i);
                            //    Console.Write(" ");
                            //    displayDrity = true;
                            //}
                            // Display[index] ^=    pixel;
                            Display[index] =
                               ((Display[index] != 0 && pixel == 0)
                               || (Display[index] == 0 && pixel == 1)) ? 0xffffffff : 0;
                        }
                    }
                    // if (displayDrity) Thread.Sleep(20);
                    // DrawDisplay();
                    break;
                case 0xE000:
                   
                    //Skip next instruction if key with the value of Vx is pressed.

                    //Checks the keyboard, and if the key corresponding to the value of Vx is currentlyin the down position
                    //, PC is increased by 2.
                    if ((opcode & 0x00FF) == 0x009E)
                    {
                        // Console.WriteLine($"need to press : {(opcode & 0x0F00) >> 8}");
                        //这里debug了半个小时。。。原因是写成了 if (key_m[(opcode & 0x0F00) >> 8] == true)
                        if (key_m[V[(opcode & 0x0F00) >> 8]] == true) PC += 2;
                        //存疑
                        //  if ((KeyBoard >> (V[(opcode & 0x0F00) >> 8] & 0x01)) == 0x01) PC += 2;
                        break;
                    }
                    else if ((opcode & 0x00FF) == 0x00A1)
                    {
                        for (int i = 0; i < key_m.Length; i++)
                        {
                            if (key_m[i]) Console.Write($" {i} "); else Console.Write("   ");
                            //Console.Write($"{key_m[i]==true?\'*':' ' } ");
                        }
                        Console.WriteLine();

                        //这里debug了半个小时。。。原因是写成了 if (key_m[(opcode & 0x0F00) >> 8] == false)
                        if (key_m[V[(opcode & 0x0F00) >> 8]] == false) PC += 2;
                        // if ((KeyBoard >> (V[(opcode & 0x0F00) >> 8] & 0x01)) != 0x01) PC += 2;
                        break;
                    }
                    else throw new Exception($"unsupport opcode {opcode:X4}");

                case 0xF000:
                    int fx = (opcode & 0x0F00) >> 8;
                    switch (opcode & 0x00FF)
                    {
                        case 0x0007:
                            V[fx] = DelayTimer;
                            break;
                        case 0x000A:
                            WaitingForKeyInput = true;
                            PC -= 2;
                            break;
                        case 0x0015:
                            DelayTimer = V[fx];
                            break;
                        case 0x0018:
                            SoundTimer = V[fx];
                            break;
                        case 0x001E:
                            I += V[fx];
                            break;
                        case 0x0029:
                            //这个没看懂
                            //
                            //
                            //
                            I = (ushort)(V[fx] * 5);
                            break;

                        //
                        //
                        //
                        //
                        case 0x0033:
                            RAM[I] = (byte)(V[fx] / 100);
                            RAM[I + 1] = (byte)((V[fx] % 100) / 10);
                            RAM[I + 2] = (byte)(V[fx] % 10);
                            break;
                        case 0x0055:
                            for (int i = 0; i <= fx; i++)
                            {
                                RAM[I + i] = V[i];
                            }
                            break;
                        case 0x0065:
                            for (int i = 0; i <= fx; i++)
                            {
                                V[i] = RAM[I + i];
                            }
                            break;
                        default:
                            throw new Exception($"unsupport opcode {opcode:X4}");
                    }
                    break;

                default:
                    throw new Exception($"unsupport opcode {opcode:X4}");
            }
        }
        /*
        public void DrawDisplay()
        {
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            for (int y = 0; y < 32; y++)
            {

                for (int x = 0; x < 64; x++)
                {
                    if (Display[x + y * 64] != 0)

                        // stringBuilder.Append("*");//
                        Console.Write("*");
                    else
                        // stringBuilder.Append(" ");//
                        Console.Write(" ");
                }
                Console.WriteLine();
            }
            Thread.Sleep(20);
        }
        */

    }
}