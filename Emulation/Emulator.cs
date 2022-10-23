namespace Emulation;

public class Emulator : IChip8Emulator
{
    public int DisplayHeight => throw new NotImplementedException();

    public int DisplayWidth => throw new NotImplementedException();

    public uint[] GetDisplay()
    {
        throw new NotImplementedException();
    }

    public Task LoadRomAsync(string path)
    {
        throw new NotImplementedException();
    }

    public void PressKey(int emulatorKey)
    {
        throw new NotImplementedException();
    }

    public void ReleaseKey(int emulatorKey)
    {
        throw new NotImplementedException();
    }

    public void StartOrContinue(int v)
    {
        throw new NotImplementedException();
    }

    public void Tick()
    {
        throw new NotImplementedException();
    }
}
