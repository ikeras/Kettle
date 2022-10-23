using System.CommandLine;
using Emulation;

RootCommand rootCommand = new();
Argument<string> ch8FilePathArgument = new(name: "path to ch8 file", description: "A full path to the location of a chip 8 (.ch8) ROM");

rootCommand.AddArgument(ch8FilePathArgument);
rootCommand.SetHandler(
    async (romPath) =>
    {
        Emulator emulator = new();
        using Display.Game1 game = new(emulator);
        await game.LoadRomAsync(romPath);
        game.Run();
    },
    ch8FilePathArgument);

await rootCommand.InvokeAsync(args);
