using System.IO;
using System.Text.Json;
using Nulmesis.Core.Domain.Models;
using Nulmesis.Core.Domain.Normalizers;
using Nulmesis.Tests.Shared;

namespace Nulmesis.App.Tests;

public sealed class CliApplicationTests
{
    [Fact]
    [Trait("Category", "Cli")]
    public async Task Cli_scan_json_outputs_contract_and_success_exit_code()
    {
        using var root = TempTestRootFactory.Create<CliApplicationTests>();
        var fixture = ReservedNameFixtureBuilder.Create(root);
        var target = fixture.CreateReadOnlyNulFile("sample\\nul");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var app = CliApplication.CreateDefault(new StringReader(string.Empty), stdout, stderr);

        var exitCode = await app.InvokeAsync(["scan", "--root", root.RootPath, "--json"], CancellationToken.None);

        Assert.Equal(CliExitCode.Success, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var payload = document.RootElement;

        Assert.True(payload.TryGetProperty("version", out _));
        Assert.True(payload.TryGetProperty("timestampUtc", out _));
        Assert.Equal(Path.GetFullPath(root.RootPath), payload.GetProperty("root").GetString());
        Assert.Equal("strict", payload.GetProperty("mode").GetString());
        Assert.Equal(1, payload.GetProperty("matches").GetArrayLength());
        Assert.Equal(target, payload.GetProperty("matches")[0].GetProperty("absolutePath").GetString());
        Assert.Equal(0, payload.GetProperty("errors").GetArrayLength());
        Assert.Equal(1, payload.GetProperty("summary").GetProperty("matchedCount").GetInt32());
    }

    [Fact]
    [Trait("Category", "Cli")]
    public async Task Cli_delete_returns_user_cancelled_exit_code_when_confirmation_is_not_y()
    {
        using var root = TempTestRootFactory.Create<CliApplicationTests>();
        var fixture = ReservedNameFixtureBuilder.Create(root);
        var target = fixture.CreateReadOnlyNulFile("pending\\nul");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var app = CliApplication.CreateDefault(new StringReader("n" + Environment.NewLine), stdout, stderr);

        var exitCode = await app.InvokeAsync(["delete", "--root", root.RootPath], CancellationToken.None);

        Assert.Equal(CliExitCode.UserCancelledDelete, exitCode);
        Assert.Contains("Delete these 1 item(s)? [y/N]:", stdout.ToString());
        Assert.Contains("Delete cancelled.", stdout.ToString());
        Assert.True(File.Exists(ReservedPathNormalizer.Normalize(target)));
    }

    [Fact]
    [Trait("Category", "Cli")]
    public async Task Cli_delete_removes_file_after_confirmation()
    {
        using var root = TempTestRootFactory.Create<CliApplicationTests>();
        var fixture = ReservedNameFixtureBuilder.Create(root);
        var target = fixture.CreateReadOnlyNulFile("delete-me\\nul");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var app = CliApplication.CreateDefault(new StringReader("y" + Environment.NewLine), stdout, stderr);

        var exitCode = await app.InvokeAsync(["delete", "--root", root.RootPath, "--mode", ScanMode.Loose.ToString()], CancellationToken.None);

        Assert.Equal(CliExitCode.Success, exitCode);
        Assert.Contains("Deleted 1 of 1 target(s).", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.False(File.Exists(ReservedPathNormalizer.Normalize(target)));
    }

    [Fact]
    [Trait("Category", "Cli")]
    public async Task Cli_delete_skips_candidates_that_do_not_require_extended_path_deletion()
    {
        using var root = TempTestRootFactory.Create<CliApplicationTests>();
        var fixture = ReservedNameFixtureBuilder.Create(root);
        var candidate = fixture.CreateReadOnlyNulFile("normal\\nul");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var app = new CliApplication(
            new DeleteTargetFilteringScanner(new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
            new Nulmesis.Core.Services.NulFileDeleter(),
            new StringReader("y" + Environment.NewLine),
            stdout,
            stderr,
            "test-version");

        var exitCode = await app.InvokeAsync(["delete", "--root", root.RootPath], CancellationToken.None);

        Assert.Equal(CliExitCode.Success, exitCode);
        Assert.Contains("No delete targets found", stdout.ToString());
        Assert.DoesNotContain("Delete these", stdout.ToString(), StringComparison.Ordinal);
        Assert.True(File.Exists(ReservedPathNormalizer.Normalize(candidate)));
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    [Trait("Category", "Cli")]
    public async Task Cli_invalid_mode_returns_invalid_arguments_exit_code()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var app = CliApplication.CreateDefault(new StringReader(string.Empty), stdout, stderr);

        var exitCode = await app.InvokeAsync(["scan", "--mode", "invalid"], CancellationToken.None);

        Assert.Equal(CliExitCode.InvalidArguments, exitCode);
        Assert.Contains("invalid", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class DeleteTargetFilteringScanner(IReadOnlySet<string> blockedRelativePaths) : Nulmesis.Core.Services.NulFileScanner
    {
        private readonly IReadOnlySet<string> _blockedRelativePaths = blockedRelativePaths;

        protected override bool IsBlockedReservedNul(NulMatch candidate)
        {
            return _blockedRelativePaths.Contains(candidate.RelativePath);
        }
    }
}
