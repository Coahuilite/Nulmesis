using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Nulmesis.App;

public static partial class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (ShouldLaunchGui(args))
        {
            return RunGui();
        }

        return RunCliAsync(args).GetAwaiter().GetResult();
    }

    private static int RunGui()
    {
        HideConsoleForGuiLaunch();
        var app = new App();
        return app.Run();
    }

    private static async Task<int> RunCliAsync(string[] args)
    {
        using var consoleSession = ConsoleHost.AttachOrAllocate();
        using var cancellationSource = new CancellationTokenSource();

        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        Console.CancelKeyPress += handler;

        try
        {
            var application = CliApplication.CreateDefault(Console.In, Console.Out, Console.Error);
            return await application.InvokeAsync(args, cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("Operation cancelled.");
            return CliExitCode.UnhandledException;
        }
        catch (Exception ex)
        {
            if (CliApplication.IsJsonRequested(args))
            {
                await CliApplication.WriteUnhandledExceptionJsonAsync(
                    Console.Out,
                    version: GetApplicationVersion(),
                    root: AppContext.BaseDirectory,
                    mode: null,
                    commandName: null,
                    ex,
                    CancellationToken.None);
            }
            else
            {
                await Console.Error.WriteLineAsync(ex.Message);
            }

            return CliExitCode.UnhandledException;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    public static bool ShouldLaunchGui(string[] args) => args.Length == 0;

    public static void HideConsoleForGuiLaunch() => ConsoleHost.FreeConsole();

    public static string GetExecutablePath()
        => Environment.ProcessPath ?? throw new InvalidOperationException("Unable to resolve executable path.");

    internal static string GetApplicationVersion()
        => FileVersionInfo.GetVersionInfo(GetExecutablePath()).ProductVersion
            ?? FileVersionInfo.GetVersionInfo(GetExecutablePath()).FileVersion
            ?? "0.1.0";
}

internal sealed class ConsoleSession : IDisposable
{
    private readonly bool _ownsConsole;

    public ConsoleSession(bool ownsConsole)
    {
        _ownsConsole = ownsConsole;
    }

    public void Dispose()
    {
        if (_ownsConsole)
        {
            ConsoleHost.FreeConsole();
        }
    }
}

internal static class ConsoleHost
{
    private const int AttachParentProcess = -1;
    private const uint Utf8CodePage = 65001;
    private const int StdInputHandle = -10;
    private const int StdOutputHandle = -11;
    private const int StdErrorHandle = -12;
    private static readonly IntPtr InvalidHandleValue = new(-1);
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    public static ConsoleSession AttachOrAllocate()
    {
        if (HasUsableStandardHandles())
        {
            RebindStandardStreams();
            return new ConsoleSession(ownsConsole: false);
        }

        if (GetConsoleWindow() != IntPtr.Zero)
        {
            RebindStandardStreams();
            return new ConsoleSession(ownsConsole: false);
        }

        if (AttachConsole(AttachParentProcess))
        {
            RebindStandardStreams();
            return new ConsoleSession(ownsConsole: false);
        }

        if (!AllocConsole())
        {
            throw new InvalidOperationException("Failed to attach or allocate a console for CLI mode.");
        }

        RebindStandardStreams();
        return new ConsoleSession(ownsConsole: true);
    }

    private static bool HasUsableStandardHandles()
    {
        return IsValidHandle(GetStdHandle(StdInputHandle))
            && IsValidHandle(GetStdHandle(StdOutputHandle))
            && IsValidHandle(GetStdHandle(StdErrorHandle));
    }

    private static bool IsValidHandle(IntPtr handle)
        => handle != IntPtr.Zero && handle != InvalidHandleValue;

    public static void FreeConsole()
    {
        _ = Program.FreeConsoleNative();
    }

    private static void RebindStandardStreams()
    {
        ConfigureConsoleEncoding();

        var stdout = Console.OpenStandardOutput();
        var stderr = Console.OpenStandardError();
        var stdin = Console.OpenStandardInput();

        Console.SetOut(new StreamWriter(stdout, Utf8WithoutBom) { AutoFlush = true });
        Console.SetError(new StreamWriter(stderr, Utf8WithoutBom) { AutoFlush = true });
        Console.SetIn(new StreamReader(stdin, Console.InputEncoding, detectEncodingFromByteOrderMarks: false));
    }

    private static void ConfigureConsoleEncoding()
    {
        if (!Console.IsOutputRedirected)
        {
            _ = SetConsoleOutputCP(Utf8CodePage);
            Console.OutputEncoding = Utf8WithoutBom;
        }

        if (!Console.IsInputRedirected)
        {
            _ = SetConsoleCP(Utf8CodePage);
            Console.InputEncoding = Utf8WithoutBom;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);
}

public static partial class Program
{
    [DllImport("kernel32.dll", EntryPoint = "FreeConsole", SetLastError = true)]
    internal static extern bool FreeConsoleNative();
}
