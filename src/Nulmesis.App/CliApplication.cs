using System.IO;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Services;

namespace Nulmesis.App;

public sealed class CliApplication
{
    private static readonly HashSet<string> HelpAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "help",
        "--help",
        "-h",
        "/h",
        "-?",
        "/?"
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly NulFileScanner _scanner;
    private readonly NulFileDeleter _deleter;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private readonly string _version;
    private readonly RootCommand _rootCommand;
    private readonly Option<string> _rootOption;
    private readonly Option<ScanMode> _modeOption;
    private readonly Option<bool> _jsonOption;

    public CliApplication(
        NulFileScanner scanner,
        NulFileDeleter deleter,
        TextReader input,
        TextWriter output,
        TextWriter error,
        string version)
    {
        _scanner = scanner;
        _deleter = deleter;
        _input = input;
        _output = output;
        _error = error;
        _version = version;

        _rootOption = new Option<string>("--root")
        {
            Description = "Root directory to inspect.",
            Recursive = true,
            DefaultValueFactory = _ => AppContext.BaseDirectory
        };

        _modeOption = new Option<ScanMode>("--mode")
        {
            Description = "Matching mode.",
            Recursive = true,
            DefaultValueFactory = _ => ScanMode.Strict
        };

        _jsonOption = new Option<bool>("--json")
        {
            Description = "Emit JSON output to stdout.",
            Recursive = true
        };

        _rootCommand = BuildRootCommand();
    }

    public static CliApplication CreateDefault(TextReader input, TextWriter output, TextWriter error)
        => new(new NulFileScanner(), new NulFileDeleter(), input, output, error, Program.GetApplicationVersion());

    public async Task<int> InvokeAsync(string[] args, CancellationToken cancellationToken)
    {
        var normalizedArgs = NormalizeHelpArguments(args);

        if (IsHelpRequest(normalizedArgs))
        {
            await WriteHelpAsync(normalizedArgs, cancellationToken);
            return CliExitCode.Success;
        }

        var parseResult = _rootCommand.Parse(normalizedArgs);
        if (parseResult.Errors.Count > 0)
        {
            return await HandleParseErrorsAsync(parseResult, cancellationToken);
        }

        if (parseResult.CommandResult.Command == _rootCommand)
        {
            await _error.WriteLineAsync("A command is required: scan, list, or delete.");
            return CliExitCode.InvalidArguments;
        }

        return await parseResult.InvokeAsync(cancellationToken: cancellationToken);
    }

    public static bool IsJsonRequested(string[] args)
        => args.Any(static arg => string.Equals(arg, "--json", StringComparison.Ordinal));

    private static string[] NormalizeHelpArguments(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "help", StringComparison.OrdinalIgnoreCase))
        {
            return args;
        }

        return args.Length == 1
            ? ["--help"]
            : [args[1], "--help", .. args.Skip(2)];
    }

    private static bool IsHelpRequest(IReadOnlyList<string> args)
        => args.Any(HelpAliases.Contains);

    private async Task WriteHelpAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        var commandName = ResolveHelpCommandName(args);

        if (commandName is null)
        {
            await _output.WriteLineAsync("Usage:");
            await _output.WriteLineAsync("  nulmesis <command> [options]");
            await _output.WriteLineAsync();
            await _output.WriteLineAsync("Commands:");
            await _output.WriteLineAsync("  scan    Scan for reserved-name nul files.");
            await _output.WriteLineAsync("  list    List reserved-name nul files.");
            await _output.WriteLineAsync("  delete  Delete reserved-name nul files after confirmation.");
            await _output.WriteLineAsync();
            await _output.WriteLineAsync("Global options:");
            await _output.WriteLineAsync("  --root <PATH>        Root directory to inspect.");
            await _output.WriteLineAsync("  --mode <strict|loose> Matching mode.");
            await _output.WriteLineAsync("  --json               Emit JSON output to stdout.");
            await _output.WriteLineAsync("  -h, --help           Show help information.");
            await _output.WriteLineAsync();
            await _output.WriteLineAsync("Examples:");
            await _output.WriteLineAsync("  nulmesis scan --root C:\\path\\to\\target");
            await _output.WriteLineAsync("  nulmesis list --root C:\\path\\to\\target --json");
            await _output.WriteLineAsync("  nulmesis help scan");
            return;
        }

        await _output.WriteLineAsync($"Usage:");
        await _output.WriteLineAsync($"  nulmesis {commandName} [options]");
        await _output.WriteLineAsync();

        switch (commandName.ToLowerInvariant())
        {
            case "scan":
                await _output.WriteLineAsync("Scan for reserved-name nul files.");
                break;
            case "list":
                await _output.WriteLineAsync("List reserved-name nul files.");
                break;
            case "delete":
                await _output.WriteLineAsync("Delete reserved-name nul files after confirmation.");
                break;
            default:
                await _error.WriteLineAsync($"Unknown command '{commandName}'. Available commands: scan, list, delete.");
                return;
        }

        await _output.WriteLineAsync();
        await _output.WriteLineAsync("Options:");
        await _output.WriteLineAsync("  --root <PATH>        Root directory to inspect.");
        await _output.WriteLineAsync("  --mode <strict|loose> Matching mode.");
        await _output.WriteLineAsync("  --json               Emit JSON output to stdout.");
        await _output.WriteLineAsync("  -h, --help           Show help information.");
        await _output.WriteLineAsync();
        await _output.WriteLineAsync("Examples:");
        await _output.WriteLineAsync($"  nulmesis {commandName} --root C:\\path\\to\\target");
        await _output.FlushAsync(cancellationToken);
    }

    private static string? ResolveHelpCommandName(IReadOnlyList<string> args)
    {
        return args.FirstOrDefault(arg => !HelpAliases.Contains(arg));
    }

    public static Task WriteUnhandledExceptionJsonAsync(
        TextWriter output,
        string version,
        string root,
        ScanMode? mode,
        string? commandName,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var payload = new CliEnvelope(
            Version: version,
            TimestampUtc: DateTime.UtcNow,
            Root: root,
            Mode: mode?.ToString().ToLowerInvariant() ?? "strict",
            Matches: Array.Empty<NulMatch>(),
            Errors: [new CliError("UnhandledException", root, ex.Message)],
            Summary: new
            {
                command = commandName,
                exitCode = CliExitCode.UnhandledException,
                matchedCount = 0,
                errorCount = 1
            });

        return WriteJsonAsync(output, payload, cancellationToken);
    }

    private RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("Detect and delete reserved-name nul files.")
        {
            _rootOption,
            _modeOption,
            _jsonOption
        };

        var scanCommand = new Command("scan", "Scan for reserved-name nul files.");
        scanCommand.SetAction(ExecuteScanAsync);

        var listCommand = new Command("list", "List reserved-name nul files.");
        listCommand.SetAction(ExecuteListAsync);

        var deleteCommand = new Command("delete", "Delete reserved-name nul files after confirmation.");
        deleteCommand.SetAction(ExecuteDeleteAsync);

        rootCommand.Subcommands.Add(scanCommand);
        rootCommand.Subcommands.Add(listCommand);
        rootCommand.Subcommands.Add(deleteCommand);

        return rootCommand;
    }

    private async Task<int> HandleParseErrorsAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (parseResult.GetValue(_jsonOption))
        {
            var root = parseResult.GetValue(_rootOption) ?? AppContext.BaseDirectory;
            var mode = parseResult.GetValue(_modeOption).ToString().ToLowerInvariant();
            var payload = new CliEnvelope(
                Version: _version,
                TimestampUtc: DateTime.UtcNow,
                Root: root,
                Mode: mode,
                Matches: Array.Empty<NulMatch>(),
                Errors: parseResult.Errors.Select(error => new CliError("ParseError", root, error.Message)).ToArray(),
                Summary: new
                {
                    command = parseResult.CommandResult.Command.Name,
                    exitCode = CliExitCode.InvalidArguments,
                    matchedCount = 0,
                    errorCount = parseResult.Errors.Count
                });

            await WriteJsonAsync(_output, payload, cancellationToken);
            return CliExitCode.InvalidArguments;
        }

        foreach (var parseError in parseResult.Errors)
        {
            await _error.WriteLineAsync(parseError.Message);
        }

        return CliExitCode.InvalidArguments;
    }

    private Task<int> ExecuteScanAsync(ParseResult parseResult, CancellationToken cancellationToken)
        => ExecuteScanLikeAsync(parseResult, cancellationToken, commandName: "scan", listOnly: false);

    private Task<int> ExecuteListAsync(ParseResult parseResult, CancellationToken cancellationToken)
        => ExecuteScanLikeAsync(parseResult, cancellationToken, commandName: "list", listOnly: true);

    private async Task<int> ExecuteScanLikeAsync(ParseResult parseResult, CancellationToken cancellationToken, string commandName, bool listOnly)
    {
        var root = parseResult.GetValue(_rootOption)!;
        var mode = parseResult.GetValue(_modeOption);
        var json = parseResult.GetValue(_jsonOption);

        try
        {
            var scanResult = await _scanner.ScanAsync(root, mode, cancellationToken);
            var exitCode = scanResult.Errors.Count > 0 ? CliExitCode.PartialFailure : CliExitCode.Success;

            if (json)
            {
                await WriteJsonAsync(
                    _output,
                    CreateScanEnvelope(commandName, scanResult, root, mode, exitCode),
                    cancellationToken);
            }
            else if (listOnly)
            {
                await WriteListHumanAsync(scanResult, root, mode);
            }
            else
            {
                await WriteScanHumanAsync(scanResult, root, mode);
            }

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (json)
            {
                await WriteUnhandledExceptionJsonAsync(_output, _version, root, mode, commandName, ex, cancellationToken);
            }
            else
            {
                await _error.WriteLineAsync(ex.Message);
            }

            return CliExitCode.UnhandledException;
        }
    }

    private async Task<int> ExecuteDeleteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var root = parseResult.GetValue(_rootOption)!;
        var mode = parseResult.GetValue(_modeOption);
        var json = parseResult.GetValue(_jsonOption);

        try
        {
            var scanResult = await _scanner.ScanAsync(root, mode, cancellationToken);
            if (scanResult.DeleteTargets.Count == 0)
            {
                var emptyExitCode = scanResult.Errors.Count > 0 ? CliExitCode.PartialFailure : CliExitCode.Success;
                if (json)
                {
                    await WriteJsonAsync(
                        _output,
                        CreateDeleteEnvelope(scanResult, deleteResult: null, root, mode, emptyExitCode),
                        cancellationToken);
                }
                else
                {
                    await _output.WriteLineAsync($"No delete targets found under '{scanResult.Summary.Root}'.");
                    await WriteErrorsAsync(scanResult.Errors);
                    await _output.WriteLineAsync($"Summary: {scanResult.Summary.MatchedCount} matches, {scanResult.DeleteTargets.Count} delete targets, {scanResult.Summary.ErrorCount} scan errors.");
                }

                return emptyExitCode;
            }

            if (!json)
            {
                await _output.WriteLineAsync("Pending delete targets:");
                foreach (var match in scanResult.DeleteTargets)
                {
                    await _output.WriteLineAsync($"- {match.AbsolutePath}");
                }

                await _output.WriteAsync($"Delete these {scanResult.DeleteTargets.Count} item(s)? [y/N]: ");
            }

            var confirmation = await _input.ReadLineAsync(cancellationToken);
            if (!string.Equals(confirmation?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
            {
                var cancelledDeleteResult = new DeleteResult
                {
                        Summary = new DeleteSummary
                        {
                        RequestedCount = scanResult.DeleteTargets.Count,
                        DeletedCount = 0,
                        FailedCount = 0,
                        Cancelled = true
                    },
                    Errors = []
                };

                if (json)
                {
                    await WriteJsonAsync(
                        _output,
                        CreateDeleteEnvelope(scanResult, cancelledDeleteResult, root, mode, CliExitCode.UserCancelledDelete),
                        cancellationToken);
                }
                else
                {
                    await _output.WriteLineAsync("Delete cancelled.");
                }

                return CliExitCode.UserCancelledDelete;
            }

            var deleteResult = await _deleter.DeleteAsync(scanResult.DeleteTargets, cancellationToken);
            var exitCode = scanResult.Errors.Count + deleteResult.Errors.Count > 0
                ? CliExitCode.PartialFailure
                : CliExitCode.Success;

            if (json)
            {
                await WriteJsonAsync(_output, CreateDeleteEnvelope(scanResult, deleteResult, root, mode, exitCode), cancellationToken);
            }
            else
            {
                await _output.WriteLineAsync($"Deleted {deleteResult.Summary.DeletedCount} of {deleteResult.Summary.RequestedCount} target(s).");
                await WriteErrorsAsync(scanResult.Errors);
                await WriteDeleteErrorsAsync(deleteResult.Errors);
                        await _output.WriteLineAsync(
                    $"Summary: {scanResult.Summary.MatchedCount} matches, {scanResult.DeleteTargets.Count} delete targets, {deleteResult.Summary.DeletedCount} deleted, {deleteResult.Summary.FailedCount} delete failures, {scanResult.Summary.ErrorCount} scan errors.");
            }

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            return CliExitCode.UserCancelledDelete;
        }
        catch (Exception ex)
        {
            if (json)
            {
                await WriteUnhandledExceptionJsonAsync(_output, _version, root, mode, "delete", ex, cancellationToken);
            }
            else
            {
                await _error.WriteLineAsync(ex.Message);
            }

            return CliExitCode.UnhandledException;
        }
    }

    private CliEnvelope CreateScanEnvelope(string commandName, ScanResult scanResult, string root, ScanMode mode, int exitCode)
    {
        return new CliEnvelope(
            Version: _version,
            TimestampUtc: DateTime.UtcNow,
            Root: Path.GetFullPath(root),
            Mode: mode.ToString().ToLowerInvariant(),
            Matches: scanResult.Matches,
            Errors: scanResult.Errors.Select(error => new CliError(error.Kind.ToString(), error.Path, error.Message)).ToArray(),
            Summary: new
            {
                command = commandName,
                exitCode,
                matchedCount = scanResult.Summary.MatchedCount,
                errorCount = scanResult.Summary.ErrorCount,
                durationMs = scanResult.Summary.DurationMs
            });
    }

    private CliEnvelope CreateDeleteEnvelope(ScanResult scanResult, DeleteResult? deleteResult, string root, ScanMode mode, int exitCode)
    {
        var errors = scanResult.Errors
            .Select(error => new CliError(error.Kind.ToString(), error.Path, error.Message))
            .Concat(deleteResult?.Errors.Select(error => new CliError("DeleteError", error.Path, error.Message)) ?? [])
            .ToArray();

        return new CliEnvelope(
            Version: _version,
            TimestampUtc: DateTime.UtcNow,
            Root: Path.GetFullPath(root),
            Mode: mode.ToString().ToLowerInvariant(),
            Matches: scanResult.DeleteTargets,
            Errors: errors,
            Summary: new
            {
                command = "delete",
                exitCode,
                matchedCount = scanResult.Summary.MatchedCount,
                scanErrorCount = scanResult.Summary.ErrorCount,
                requestedCount = deleteResult?.Summary.RequestedCount ?? 0,
                deletedCount = deleteResult?.Summary.DeletedCount ?? 0,
                failedCount = deleteResult?.Summary.FailedCount ?? 0,
                cancelled = deleteResult?.Summary.Cancelled ?? false
            });
    }

    private async Task WriteScanHumanAsync(ScanResult scanResult, string root, ScanMode mode)
    {
        await _output.WriteLineAsync($"Root: {Path.GetFullPath(root)}");
        await _output.WriteLineAsync($"Mode: {mode.ToString().ToLowerInvariant()}");
        await _output.WriteLineAsync($"Matches: {scanResult.Summary.MatchedCount}");

        foreach (var match in scanResult.Matches)
        {
            await _output.WriteLineAsync($"- {match.AbsolutePath} ({match.SizeBytes} bytes, {match.LastWriteTimeUtc:O})");
        }

        if (scanResult.Matches.Count == 0)
        {
            await _output.WriteLineAsync("No matches found.");
        }

        await WriteErrorsAsync(scanResult.Errors);
        await _output.WriteLineAsync($"Summary: {scanResult.Summary.MatchedCount} matches, {scanResult.Summary.ErrorCount} errors, {scanResult.Summary.DurationMs} ms.");
    }

    private async Task WriteListHumanAsync(ScanResult scanResult, string root, ScanMode mode)
    {
        if (scanResult.Matches.Count == 0)
        {
            await _output.WriteLineAsync($"No matches found under '{Path.GetFullPath(root)}' (mode: {mode.ToString().ToLowerInvariant()}).");
        }
        else
        {
            foreach (var match in scanResult.Matches)
            {
                await _output.WriteLineAsync(match.AbsolutePath);
            }
        }

        await WriteErrorsAsync(scanResult.Errors);
        await _output.WriteLineAsync($"Summary: {scanResult.Summary.MatchedCount} matches, {scanResult.Summary.ErrorCount} errors.");
    }

    private async Task WriteErrorsAsync(IEnumerable<ScanError> errors)
    {
        foreach (var error in errors)
        {
            await _error.WriteLineAsync($"[{error.Kind}] {error.Path}: {error.Message}");
        }
    }

    private async Task WriteDeleteErrorsAsync(IEnumerable<DeleteError> errors)
    {
        foreach (var error in errors)
        {
            await _error.WriteLineAsync($"[DeleteError] {error.Path}: {error.Message}");
        }
    }

    private static async Task WriteJsonAsync(TextWriter output, CliEnvelope payload, CancellationToken cancellationToken)
    {
        await output.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions).AsMemory(), cancellationToken);
        await output.WriteLineAsync();
    }

    private sealed record CliEnvelope(
        string Version,
        DateTime TimestampUtc,
        string Root,
        string Mode,
        IReadOnlyList<NulMatch> Matches,
        IReadOnlyList<CliError> Errors,
        object Summary);

    private sealed record CliError(string Kind, string Path, string Message);
}

public static class CliExitCode
{
    public const int Success = 0;
    public const int PartialFailure = 1;
    public const int InvalidArguments = 2;
    public const int UserCancelledDelete = 3;
    public const int UnhandledException = 4;
}
