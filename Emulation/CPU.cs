namespace Emulation;

internal class CPU
{
    private const byte smallFontHeight = 5;
    private const ushort smallFontMemoryOffset = 0x00;

    private static readonly byte[] smallFont = new byte[]
    {
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    };

    private int displayWidth;
    private int displayHeight;
    private byte[] display;
    private readonly bool[] keys;
    private readonly byte[] memory;

    private readonly byte[] registers;
    private ushort i;
    private int lastKeyPressed;
    private int numberOfKeysPressed;
    private byte soundTimer;
    private byte delayTimer;
    private readonly Stack<ushort> stack;
    private ushort pc;

    public CPU(byte[] memory)
    {
        this.memory = memory;
        this.displayHeight = 32;
        this.displayWidth = 64;
        this.keys = new bool[16];
        this.display = new byte[this.displayHeight * this.displayWidth];
        this.registers = new byte[16];
        this.stack = new();
        this.soundTimer = 0;
        this.delayTimer = 0;
        this.lastKeyPressed = 0x0;
        this.numberOfKeysPressed = 0;
        this.i = 0;
        this.pc = 0x200;

        Array.Copy(CPU.smallFont, 0, this.memory, CPU.smallFontMemoryOffset, CPU.smallFont.Length);
    }

    public int DisplayWidth => this.displayWidth;

    public int DisplayHeight => this.displayHeight;

    internal uint[] GetDisplay()
    {
        int displaySize = this.displayWidth * this.displayHeight;
        uint[] result = new uint[displaySize];

        lock (this.display)
        {
            for (int index = 0; index < displaySize; index++)
            {
                result[index] = this.display[index] == 0 ? 0 : 0xFFFFFFFF;
            }
        }

        return result;
    }

    internal void ExecuteNextInstruction()
    {
        byte hiInstruction = this.memory[this.pc];
        byte loInstruction = this.memory[this.pc + 1];
        this.pc += 2;

        byte instruction = (byte)(hiInstruction >> 4);
        byte x = (byte)(hiInstruction & 0x0f);
        byte y = (byte)(loInstruction >> 4);
        byte n = (byte)(loInstruction & 0x0f);

        byte kk = loInstruction;
        ushort nnn = (ushort)(x << 8 | kk);

        switch (instruction)
        {
            case 0x0:
                switch (nnn)
                {
                    case 0x0e0:
                        lock (this.display)
                        {
                            Array.Clear(this.display);
                        }
                        break;
                    case 0x0ee:
                        this.pc = this.stack.Pop();
                        break;
                    default:
                        throw new NotImplementedException();
                }
                break;
            case 0x1:
                this.pc = nnn;
                break;
            case 0x2:
                this.stack.Push(this.pc);
                this.pc = nnn;
                break;
            case 0x3:
                if (this.registers[x] == kk)
                {
                    this.pc += 2;
                }
                break;
            case 0x4:
                if (this.registers[x] != kk)
                {
                    this.pc += 2;
                }
                break;
            case 0x6:
                this.registers[x] = kk;
                break;
            case 0x7:
                this.registers[x] += kk;
                break;
            case 0x8:
                switch (n)
                {
                    case 0x7:
                        int result = this.registers[y] - this.registers[x];
                        this.registers[0xf] = (byte)(result >= 0 ? 1 : 0);
                        this.registers[x] = (byte)result;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                break;
            case 0xa:
                this.i = nnn;
                break;
            case 0xc:
                this.registers[x] = (byte)(Random.Shared.Next() & kk);
                break;
            case 0xd:
                this.DrawSprite(x, y, n);
                break;
            case 0xf:
                switch (kk)
                {
                    case 0x0a:
                        if (this.numberOfKeysPressed > 0)
                        {
                            this.registers[x] = (byte)this.lastKeyPressed;
                        }
                        else
                        {
                            this.pc -= 2;
                        }
                        break;
                    case 0x1e:
                        uint result = (uint)(this.i + this.registers[x]);
                        if (result > 0xFFF)
                        {
                            this.registers[0xf] = 1;
                        }
                        this.i = (ushort)result;
                        break;
                    case 0x29:
                        this.i = (ushort)(CPU.smallFontMemoryOffset + (this.registers[x] * CPU.smallFontHeight));
                        break;
                    default:
                        throw new NotImplementedException();
                }
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void PressKey(int key)
    {
        if (!this.keys[key])
        {
            this.numberOfKeysPressed++;
            this.keys[key] = true;
            this.lastKeyPressed = key;
        }
    }

    public void ReleaseKey(int key)
    {
        if (this.keys[key])
        {
            this.numberOfKeysPressed--;
            this.keys[key] = false;
        }
    }

    private void DrawSprite(byte x, byte y, byte n)
    {
        int xStart = this.registers[x] % this.displayWidth;
        int yOffset = this.registers[y] % this.displayHeight;
        this.registers[0xf] = 0;

        int spriteWidth = 8;
        int spriteHeight = n;

        lock (this.display)
        {
            for (int row = 0; row < spriteHeight; row++)
            {
                int spriteRowData = this.memory[this.i + row];
                int xOffset = xStart;

                for (int bit = 0; bit < spriteWidth; bit++)
                {
                    ushort spriteBit = (ushort)(spriteRowData & (1 << (spriteWidth - 1 - bit)));
                    int location = yOffset * this.displayWidth + xOffset;
                    byte pixel = this.display[location];

                    if (spriteBit > 0)
                    {
                        if (pixel != 0)
                        {
                            pixel = 0;
                            this.registers[0xf] = 1;
                        }
                        else
                        {
                            pixel = 1;
                        }
                    }

                    this.display[location] = pixel;

                    xOffset++;

                    if (xOffset >= this.displayWidth)
                    {
                        break;
                    }
                }

                yOffset++;

                if (yOffset >= this.displayHeight)
                {
                    break;
                }
            }
        }
    }
}
