using deathloop_savedata_checksum_fixer_cli;
using deathloop_savedata_checksum_fixer_cli.Helpers;
using Mi5hmasH.Logger;

namespace QualityControl.xUnit;

public sealed class CoreTests : IDisposable
{
    private readonly Core _core;
    private readonly ITestOutputHelper _output;

    public CoreTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("SETUP");

        // Setup
        var logger = new SimpleLogger();
        var progressReporter = new ProgressReporter(null, null);
        _core = new Core(logger, progressReporter);
    }

    public void Dispose()
    {
        _output.WriteLine("CLEANUP");
    }

    [Fact]
    public void ProcessSaveDataFiles_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            _core.ProcessSaveDataFiles(tempDir, false, null, cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir);

        // Assert
        Assert.True(testResult);
    }

    [Fact]
    public void ProcessSaveData_DoesConvertConsoleToPc()
    {
        // Arrange
        ReadOnlySpan<byte> consoleSaveDataFile = Properties.Resources.console_title_data;
        Span<byte> pcSaveDataFile = new byte[consoleSaveDataFile.Length];
        consoleSaveDataFile.CopyTo(pcSaveDataFile);

        // Act
        Core.ProcessSaveData(ref pcSaveDataFile, true, "76561197960265729");

        // Assert
        Assert.Equal(Properties.Resources.pc_title_data, (ReadOnlySpan<byte>)pcSaveDataFile);
    }

    [Fact]
    public void ProcessSaveData_DoesConvertPcToConsole()
    {
        // Arrange
        ReadOnlySpan<byte> pcSaveDataFile = Properties.Resources.pc_title_data;
        Span<byte> consoleSaveDataFile = new byte[pcSaveDataFile.Length];
        pcSaveDataFile.CopyTo(consoleSaveDataFile);

        // Act
        Core.ProcessSaveData(ref consoleSaveDataFile, true, "76561197960265730");

        // Assert
        Assert.Equal(Properties.Resources.console_title_data, (ReadOnlySpan<byte>)consoleSaveDataFile);
    }
}