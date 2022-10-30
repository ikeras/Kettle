namespace Emulation;

internal static class Ch8Loader
{
    public static async Task LoadAsync(string path, byte[] memory)
    {
        // Helpful tip from Andrew that if you don't use the FileStream constructor with useAsync true, then File I/O isn't
        // async even if we use the async stream methods. https://learn.microsoft.com/en-gb/dotnet/api/system.io.filestream?view=net-6.0#remarks
        using FileStream inputStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        await inputStream.ReadAsync(memory.AsMemory(0x200, (int)inputStream.Length));
    }
}
