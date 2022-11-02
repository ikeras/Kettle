using System.Diagnostics;

namespace Emulation;

public class Emulator : IChip8Emulator
{
    private readonly byte[] memory;
    private readonly CPU processor;
    private bool isExecuting;

    public Emulator()
    {
        this.memory = new byte[4 * 1024];
        this.processor = new CPU(this.memory);
        this.isExecuting = false;
    }

    public int DisplayHeight => this.processor.DisplayHeight;

    public int DisplayWidth => this.processor.DisplayWidth;

    public uint[] GetDisplay()
    {
        return this.processor.GetDisplay();
    }

    public async Task LoadRomAsync(string path)
    {
        await Ch8Loader.LoadAsync(path, this.memory);
    }

    public void PressKey(int emulatorKey)
    {
        this.processor.PressKey(emulatorKey);
    }

    public void ReleaseKey(int emulatorKey)
    {
        this.processor.ReleaseKey(emulatorKey);
    }

    public void StartOrContinue(int instructionsPerSecond)
    {
        this.isExecuting = true;

        long ticksToSleepBetweenInstructions = this.AdjustTimingToMatchProvidedSpeed(instructionsPerSecond);

        while (this.isExecuting)
        {
            Thread.Sleep(TimeSpan.FromTicks(ticksToSleepBetweenInstructions));
            this.processor.ExecuteNextInstruction();
        }
    }

    private long AdjustTimingToMatchProvidedSpeed(int instructionsPerSecond)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int instructionIndex = 0; instructionIndex < instructionsPerSecond; instructionIndex++)
        {
            this.processor.ExecuteNextInstruction();
        }

        long machineTicksElapsed = stopwatch.ElapsedTicks;

        long ticksPerInstruction = (TimeSpan.TicksPerSecond - machineTicksElapsed) / instructionsPerSecond;

        if (ticksPerInstruction < 0)
        {
            ticksPerInstruction = 0;
        }

        return ticksPerInstruction;
    }

    public void Tick()
    {
        this.processor.Tick();
    }
}
