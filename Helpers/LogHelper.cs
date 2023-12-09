using System;
using System.IO;
using Avalonia.OpenGL.Egl;

namespace MSRDownloader.Helpers;

public static class LogHelper
{
    private static readonly string AppDataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRDownloader");

    public static void WriteError(string message, Exception? exception = null)
    {
        File.WriteAllText(Path.Join(AppDataDirectory, "errors.log"), $"{message}\n{exception?.Message ?? string.Empty}\n---------------\n");
    }
}