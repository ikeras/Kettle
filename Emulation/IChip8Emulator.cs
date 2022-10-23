namespace Emulation;

public interface IChip8Emulator
{
    /// <summary>
    /// The height of the emulator display (not the screen display)
    /// </summary>
    int DisplayHeight { get; }

    /// <summary>
    /// The width of the emulator display (not the screen display)
    /// </summary>
    int DisplayWidth { get; }

    /// <summary>
    /// Retrieves the current display. Will be called
    /// on a separate thread than the emulator is running on.
    /// </summary>
    /// <returns>An array of 'colors' of the form ARGB (1 byte per attribute).</returns>
    uint[] GetDisplay();

    /// <summary>
    /// Loads a ch8 ROM into the emulator's memory
    /// </summary>
    /// <param name="path">The full path to a ch8 ROM to be loaded</param>
    Task LoadRomAsync(string path);

    /// <summary>
    /// Called when a specific key is depressed
    /// </summary>
    /// <param name="emulatorKey">The emulator key that is being pressed</param>
    void PressKey(int emulatorKey);

    /// <summary>
    /// Called when a specific key is released
    /// </summary>
    /// <param name="emulatorKey">The emulator key that is being released</param>
    void ReleaseKey(int emulatorKey);

    /// <summary>
    /// This method starts execution of the emulator. This is called on a separate thread
    /// than the display and is long running.
    /// </summary>
    /// <param name="instructionsPerSecond">This captures how fast the emulator should execute based on number of seconds passed in</param>
    void StartOrContinue(int instructionsPerSecond);

    /// <summary>
    /// This method is called at 60hz (60 times per second)
    /// </summary>
    void Tick();
}