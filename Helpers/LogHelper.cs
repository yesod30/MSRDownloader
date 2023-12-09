using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.OpenGL.Egl;

namespace MSRDownloader.Helpers;

public static class LogHelper
{
    private static readonly string AppDataDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MSRDownloader");

    public static void WriteError(string message, Exception? exception = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var exceptionMessage = exception is not null ? exception.Message + "\n" + exception.StackTrace + "\n" : string.Empty;
        File.AppendAllText(Path.Join(AppDataDirectory, "errors.log"), $"\n{timestamp} {message}\n{exceptionMessage}-----------------------\n");
    }

    public static void WriteException(Exception exception)
    {
        File.AppendAllText(Path.Join(AppDataDirectory, "errors.log"), exception.ToString());
    }
}