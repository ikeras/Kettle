namespace Emulation;

internal class CPU
{
    private const byte smallFontHeight = 5;
    private const byte largeFontHeight = 10;
    private const ushort smallFontMemoryOffset = 0x00;
    private const ushort largeFontMemoryOffset = 0x50;

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

    private static readonly byte[] largeFont = new byte[]
    {
            0x7C, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0x7C, 0x00, // 0
            0x08, 0x18, 0x38, 0x08, 0x08, 0x08, 0x08, 0x08, 0x3C, 0x00, // 1
            0x7C, 0x82, 0x02, 0x02, 0x04, 0x18, 0x20, 0x40, 0xFE, 0x00, // 2
            0x7C, 0x82, 0x02, 0x02, 0x3C, 0x02, 0x02, 0x82, 0x7C, 0x00, // 3
            0x84, 0x84, 0x84, 0x84, 0xFE, 0x04, 0x04, 0x04, 0x04, 0x00, // 4
            0xFE, 0x80, 0x80, 0x80, 0xFC, 0x02, 0x02, 0x82, 0x7C, 0x00, // 5
            0x7C, 0x82, 0x80, 0x80, 0xFC, 0x82, 0x82, 0x82, 0x7C, 0x00, // 6
            0xFE, 0x02, 0x04, 0x08, 0x10, 0x20, 0x20, 0x20, 0x20, 0x00, // 7
            0x7C, 0x82, 0x82, 0x82, 0x7C, 0x82, 0x82, 0x82, 0x7C, 0x00, // 8
            0x7C, 0x82, 0x82, 0x82, 0x7E, 0x02, 0x02, 0x82, 0x7C, 0x00, // 9
            0x10, 0x28, 0x44, 0x82, 0x82, 0xFE, 0x82, 0x82, 0x82, 0x00, // A
            0xFC, 0x82, 0x82, 0x82, 0xFC, 0x82, 0x82, 0x82, 0xFC, 0x00, // B
            0x7C, 0x82, 0x80, 0x80, 0x80, 0x80, 0x80, 0x82, 0x7C, 0x00, // C
            0xFC, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0x82, 0xFC, 0x00, // D
            0xFE, 0x80, 0x80, 0x80, 0xF8, 0x80, 0x80, 0x80, 0xFE, 0x00, // E
            0xFE, 0x80, 0x80, 0x80, 0xF8, 0x80, 0x80, 0x80, 0x80, 0x00, // F
    };

    private int displayWidth;
    private int displayHeight;
    private byte[] display;
    private readonly bool[] keys;
    private readonly byte[] memory;

    private readonly byte[] persistedRegisters;
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
        this.persistedRegisters = new byte[16];
        this.registers = new byte[16];
        this.stack = new();
        this.soundTimer = 0;
        this.delayTimer = 0;
        this.lastKeyPressed = 0x0;
        this.numberOfKeysPressed = 0;
        this.i = 0;
        this.pc = 0x200;

        Array.Copy(CPU.smallFont, 0, this.memory, CPU.smallFontMemoryOffset, CPU.smallFont.Length);
        Array.Copy(CPU.largeFont, 0, this.memory, CPU.largeFontMemoryOffset, CPU.largeFont.Length);
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
                // No default because we ignore 0x0nnn instruction as it was only used in very old ROMs. But we don't want to throw on it.
                switch (nnn)
                {
                    case 0x0e0:
                        lock (this.display)
                        {
                            Array.Clear(this.display);
                        }
                        break;
                    case >= 0x00c0 and <= 0x00cf:
                        this.ScrollDisplayDown(n);
                        break;
                    case 0x00ee:
                        this.pc = this.stack.Pop();
                        break;
                    case 0x00fb:
                        this.ScrollDisplayRight();
                        break;
                    case 0x00fc:
                        this.ScrollDisplayLeft();
                        break;
                    case 0x00fe:
                        this.CreateDisplay(64, 32);
                        break;
                    case 0x00ff:
                        this.CreateDisplay(128, 64);
                        break;
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
            case 0x5:
                if (this.registers[x] == this.registers[y])
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
                    case 0x0:
                        this.registers[x] = this.registers[y];
                        break;
                    case 0x1:
                        this.registers[x] |= this.registers[y];
                        break;
                    case 0x2:
                        this.registers[x] &= this.registers[y];
                        break;
                    case 0x3:
                        this.registers[x] ^= this.registers[y];
                        break;
                    case 0x4:
                        int result = this.registers[x] + this.registers[y];
                        this.registers[0xf] = (byte)(result > 0xff ? 1 : 0);
                        this.registers[x] = (byte)result;
                        break;
                    case 0x5:
                        result = this.registers[x] - this.registers[y];
                        this.registers[0xf] = (byte)(result >= 0 ? 1 : 0);
                        this.registers[x] = (byte)result;
                        break;
                    case 0x6:
                        this.registers[0xf] = (byte)(((this.registers[x] & 0x01) == 0x01) ? 1 : 0);
                        this.registers[x] >>= 1;
                        break;
                    case 0x7:
                        result = this.registers[y] - this.registers[x];
                        this.registers[0xf] = (byte)(result >= 0 ? 1 : 0);
                        this.registers[x] = (byte)result;
                        break;
                    case 0xe:
                        this.registers[0xf] = (byte)(this.registers[x] >> 7);
                        this.registers[x] <<= 1;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                break;
            case 0x9:
                if (this.registers[x] != this.registers[y])
                {
                    this.pc += 2;
                }
                break;
            case 0xa:
                this.i = nnn;
                break;
            case 0xb:
                this.pc = (ushort)(nnn + this.registers[0x0]);
                break;
            case 0xc:
                this.registers[x] = (byte)(Random.Shared.Next() & kk);
                break;
            case 0xd:
                this.DrawSprite(x, y, n);
                break;
            case 0xe:
                switch (kk)
                {
                    case 0x9e:
                        if (this.keys[this.registers[x]])
                        {
                            this.pc += 2;
                        }
                        break;
                    case 0xa1:
                        if (!this.keys[this.registers[x]])
                        {
                            this.pc += 2;
                        }
                        break;
                }
                break;
            case 0xf:
                switch (kk)
                {
                    case 0x07:
                        this.registers[x] = this.delayTimer;
                        break;
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
                    case 0x15:
                        this.delayTimer = this.registers[x];
                        break;
                    case 0x18:
                        this.soundTimer = this.registers[x];
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
                    case 0x30:
                        this.i = (ushort)(CPU.largeFontMemoryOffset + (this.registers[x] * CPU.largeFontHeight));
                        break;
                    case 0x33:
                        byte vx = this.registers[x];
                        this.memory[this.i] = (byte)(vx / 100);
                        this.memory[this.i + 1] = (byte)(vx / 10 % 10);
                        this.memory[this.i + 2] = (byte)(vx % 10);
                        break;
                    case 0x55:
                        Array.Copy(this.registers, 0, this.memory, this.i, x + 1);
                        break;
                    case 0x65:
                        Array.Copy(this.memory, this.i, this.registers, 0, x + 1);
                        break;
                    case 0x75:
                        Array.Copy(this.registers, 0, this.persistedRegisters, 0, 16);
                        break;
                    case 0x85:
                        Array.Copy(this.persistedRegisters, 0, this.registers, 0, 16);
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

    public void Tick()
    {
        if (this.delayTimer > 0)
        {
            this.delayTimer--;
        }

        if (this.soundTimer > 0)
        {
            this.soundTimer--;
        }
    }

    private void CreateDisplay(int width, int height)
    {
        this.displayHeight = height;
        this.displayWidth = width;

        this.display = new byte[this.displayWidth * this.displayHeight];
    }

    private void DrawSprite(byte x, byte y, byte n)
    {
        int xStart = this.registers[x] % this.displayWidth;
        int yOffset = this.registers[y] % this.displayHeight;
        this.registers[0xf] = 0;

        int spriteWidth = n == 0 ? 16 : 8;
        int spriteHeight = n == 0 ? 16 : n;

        lock (this.display)
        {
            for (int row = 0; row < spriteHeight; row++)
            {
                int spriteRowData = n == 0 ?
                    this.memory[this.i + (row * 2)] << 8 | this.memory[this.i + (row * 2) + 1] :
                    this.memory[this.i + row];
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

    private void ScrollDisplayDown(int rows)
    {
        int pixelsToMove = rows * this.displayWidth;
        Array.Copy(this.display, 0, this.display, pixelsToMove, this.display.Length - pixelsToMove);
        Array.Clear(this.display, 0, pixelsToMove);
    }

    private void ScrollDisplayLeft()
    {
        for (int row = 0; row < this.displayHeight; row++)
        {
            int rowOffset = row * this.displayWidth;
            Array.Copy(this.display, rowOffset + 4, this.display, rowOffset, this.displayWidth - 4);
            Array.Clear(this.display, rowOffset + (this.displayWidth - 4), 4);
        }
    }

    private void ScrollDisplayRight()
    {
        for (int row = 0; row < this.displayHeight; row++)
        {
            int rowOffset = row * this.displayWidth;
            Array.Copy(this.display, rowOffset, this.display, rowOffset + 4, this.displayWidth - 4);
            Array.Clear(this.display, rowOffset, 4);
        }
    }
}
