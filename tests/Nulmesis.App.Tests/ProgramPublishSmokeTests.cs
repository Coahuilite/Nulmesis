using System.IO;

namespace Nulmesis.App.Tests;

public sealed class ProgramPublishSmokeTests
{
    [Fact]
    [Trait("Category", "PublishSmoke")]
    public void PublishSmoke_NoArguments_SelectsGuiMode()
    {
        Assert.True(Program.ShouldLaunchGui([]));
    }

    [Fact]
    [Trait("Category", "PublishSmoke")]
    public void PublishSmoke_WithArguments_SelectsCliMode()
    {
        Assert.False(Program.ShouldLaunchGui(["scan"]));
    }

    [Fact]
    [Trait("Category", "PublishSmoke")]
    public void PublishSmoke_ExecutablePath_UsesCurrentProcessPath()
    {
        var executablePath = Program.GetExecutablePath();

        Assert.False(string.IsNullOrWhiteSpace(executablePath));
        Assert.Equal(Environment.ProcessPath, executablePath);
        Assert.True(Path.IsPathRooted(executablePath));
    }
}
