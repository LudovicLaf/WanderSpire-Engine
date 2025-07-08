// SceneEditor/Program.cs - Enable Console for Debugging
using Avalonia;
using System;
using System.Runtime.InteropServices;

namespace SceneEditor;

class Program
{
    // Platform-specific console allocation
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FreeConsole();

    [DllImport("libc", SetLastError = true)]
    static extern IntPtr stdout();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Enable console output for debugging
        EnableConsoleOutput();

        Console.WriteLine("=== WanderSpire Scene Editor Starting ===");
        Console.WriteLine($"Arguments: {string.Join(", ", args)}");

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application crashed: {ex}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            throw;
        }
        finally
        {
            Console.WriteLine("=== Application Shutting Down ===");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace(); // This helps with Avalonia internal logging

    private static void EnableConsoleOutput()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, allocate a console window
                if (AllocConsole())
                {
                    Console.WriteLine("Console allocated successfully");
                }
                else
                {
                    // Console might already be allocated (e.g., when running from command line)
                    Console.WriteLine("Console already available or allocation failed");
                }
            }

            // Set up console encoding for better character support
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Redirect Console.WriteLine to also go to Debug output
            Console.SetOut(new ConsoleTraceWriter());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to enable console: {ex}");
        }
    }
}

// Helper class to write to both console and debug output
public class ConsoleTraceWriter : System.IO.TextWriter
{
    private readonly System.IO.TextWriter _console;

    public ConsoleTraceWriter()
    {
        _console = Console.Out;
    }

    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        _console?.WriteLine(value);
        System.Diagnostics.Debug.WriteLine(value);
    }

    public override void Write(string? value)
    {
        _console?.Write(value);
        System.Diagnostics.Debug.Write(value);
    }
}