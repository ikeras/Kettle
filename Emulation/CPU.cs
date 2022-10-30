namespace Emulation;

internal class CPU
{
    private int displayWidth;
    private int displayHeight;
    private byte[] display;
    private readonly byte[] memory;

    private readonly byte[] registers;
    private ushort i;
    private byte soundTimer;
    private byte delayTimer;
    private readonly Stack<byte> stack;
    private ushort pc;

    public CPU(byte[] memory)
    {
        this.memory = memory;
        this.displayHeight = 32;
        this.displayWidth = 64;
        this.display = new byte[this.displayHeight * this.displayWidth];
        this.registers = new byte[16];
        this.stack = new Stack<byte>();
        this.soundTimer = 0;
        this.delayTimer = 0;
        this.i = 0;
        this.pc = 0x200;
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
                if (nnn == 0x0e0)
                {
                    Array.Clear(this.display);
                }
                break;
            case 0x1:
                this.pc = nnn;
                break;
            case 0x06:
                this.registers[x] = kk;
                break;
            case 0x07:
                this.registers[x] += kk;
                break;
            case 0xa:
                this.i = nnn;
                break;
            case 0xd:
                this.DrawSprite(x, y, n);
                break;
            default:
                throw new Exception();
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
