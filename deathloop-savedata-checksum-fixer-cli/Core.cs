using deathloop_savedata_checksum_fixer_cli.Helpers;
using deathloop_savedata_checksum_fixer_cli.Infrastructure;
using Mi5hmasH.Logger;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static System.Text.Encoding;

namespace deathloop_savedata_checksum_fixer_cli;

public class Core(SimpleLogger logger, ProgressReporter progressReporter)
{
    private const string SaveDataFileName = "save.dloop";
    private const string TitleDataFileName = "title.data";
    private const string DataMagicPattern = "OOD\0";
    private const string PcHeaderMagicPattern = "SAVE\x01";
    private const int PcHeaderMagicPatternSize = sizeof(ulong);
    private const int ChecksumSize = sizeof(uint);
    private const int PcHeaderSize = 72;

    /// <summary>
    /// Creates a new ParallelOptions instance configured with the specified cancellation token and an optimal degree of parallelism for the current environment.
    /// </summary>
    /// <param name="cts">The CancellationTokenSource whose token will be used to support cancellation of parallel operations.</param>
    /// <returns>A ParallelOptions object initialized with the provided cancellation token and a maximum degree of parallelism based on the number of available processors.</returns>
    private static ParallelOptions GetParallelOptions(CancellationTokenSource cts)
        => new()
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
        };

    /// <summary>
    /// Retrieves the full paths of all save data and title data files located within the specified folder and its subdirectories.
    /// </summary>
    /// <param name="inputFolderPath">The path to the folder in which to search for save data and title data files. This path must refer to an existing and accessible directory.</param>
    /// <returns>An array of strings containing the full paths of all found save data and title data files. Returns an empty
    /// array if no matching files are found.</returns>
    public static string[] GetSaveDataFiles(string inputFolderPath)
    {
        const SearchOption so = SearchOption.AllDirectories;
        return Directory.GetFiles(inputFolderPath, SaveDataFileName, so)
            .Concat(Directory.GetFiles(inputFolderPath, TitleDataFileName, so))
            .ToArray();
    }

    /// <summary>
    /// Determines whether the specified file signature matches the expected console save data pattern.
    /// </summary>
    /// <param name="fileSignature">A read-only span of bytes representing the file signature to validate against the console save data pattern.</param>
    /// <returns>true if the file signature matches the expected console save data pattern; otherwise, false.</returns>
    public static bool IsConsoleSaveData(ReadOnlySpan<byte> fileSignature)
        => fileSignature.SequenceEqual(ASCII.GetBytes(DataMagicPattern));

    /// <summary>
    /// Retrieves the user identifier as a string from the underlying data buffer.
    /// </summary>
    /// <returns>A string that represents the user ID. Returns an empty string if no valid user ID is present.</returns>
    public static string GetUserId(ReadOnlySpan<byte> data)
    {
        var buffer = data[PcHeaderMagicPatternSize..];
        var len = buffer.IndexOf((byte)0);
        if (len < 0) len = buffer.Length;

        return ASCII.GetString(buffer[..len]);
    }

    /// <summary>
    /// Sets the user ID by encoding the specified string value into a buffer.
    /// </summary>
    /// <param name="data">The buffer where the user ID will be encoded.</param>
    /// <param name="value">The user ID to set, which will be encoded into the buffer. The length of the user ID is limited to the size of the buffer.</param>
    public static void SetUserId(Span<byte> data, string value)
    {
        var buffer = data[PcHeaderMagicPatternSize..];
        buffer.Clear();
        var len = value.Length > buffer.Length ? buffer.Length : value.Length;
        ASCII.GetBytes(value.AsSpan()[..len], buffer);
    }

    /// <summary>
    /// Converts the provided save data between console and PC platform formats in place.
    /// </summary>
    /// <param name="data">A reference to a span of bytes containing the save data to convert. The span is modified to reflect the target platform format.</param>
    /// <param name="isConsoleSaveData">A value indicating whether the input data is in console save format.
    /// If <see langword="true"/>, the data is converted to PC format; otherwise, it is converted from PC format to console save format.</param>
    private static void SwitchPlatform(ref Span<byte> data, ref bool isConsoleSaveData)
    {
        const int dataOffset = ChecksumSize + PcHeaderSize;
        if (isConsoleSaveData)
        {
            var convertedData = new byte[data.Length + PcHeaderSize];
            var convertedDataSpan = convertedData.AsSpan();
            data[..ChecksumSize].CopyTo(convertedDataSpan);
            data[ChecksumSize..].CopyTo(convertedDataSpan[dataOffset..]);
            ASCII.GetBytes(PcHeaderMagicPattern).CopyTo(convertedDataSpan.Slice(ChecksumSize, PcHeaderMagicPatternSize));
            data = convertedData;
        }
        else
        {
            var convertedData = new byte[data.Length - PcHeaderSize];
            data[..ChecksumSize].CopyTo(convertedData);
            data[dataOffset..].CopyTo(convertedData.AsSpan()[ChecksumSize..]);
            data = convertedData;
        }
        // Toggle the console save data flag
        isConsoleSaveData ^= true;
    }

    /// <summary>
    /// Calculates and updates the checksum value in the provided data buffer using the MD5 hash of the data, excluding the initial checksum segment.
    /// </summary>
    /// <param name="data">A span of bytes representing the data to update. The first element is overwritten with the newly computed checksum value.</param>
    private static void FixChecksum(Span<byte> data)
    {
        var hash = MD5.HashData(data[ChecksumSize..]);
        var hashSpan = hash.AsSpan();
        var hashSpanAsUint = MemoryMarshal.Cast<byte, uint>(hashSpan);
        var checksumNew = hashSpanAsUint[0] ^ hashSpanAsUint[1] ^ hashSpanAsUint[2] ^ hashSpanAsUint[3];
        var dataSpanAsUint = MemoryMarshal.Cast<byte, uint>(data);
        dataSpanAsUint[0] = checksumNew;
    }

    /// <summary>
    /// Processes the provided data span by optionally switching the platform and updating the user ID, based on the characteristics of the data.
    /// </summary>
    /// <param name="dataSpan">A reference to a span of bytes representing the data to process. The span is modified in place during processing.</param>
    /// <param name="switchPlatform">true to switch the platform based on the data's characteristics; otherwise, false.</param>
    /// <param name="newUserId">The new user ID to set if the data does not correspond to console save data; or null to leave the user ID unchanged.</param>
    public static void ProcessSaveData(ref Span<byte> dataSpan, bool switchPlatform, string? newUserId)
    {
        // Check if the file is a console SaveData file by examining its signature
        var fileSignature = dataSpan.Slice(ChecksumSize, DataMagicPattern.Length);
        var isConsoleSaveData = IsConsoleSaveData(fileSignature);

        // Switch platform if necessary
        if (switchPlatform) SwitchPlatform(ref dataSpan, ref isConsoleSaveData);

        // Update the user ID if necessary
        if (!isConsoleSaveData && newUserId != null)
            SetUserId(dataSpan.Slice(ChecksumSize, PcHeaderSize), newUserId);

        // Fix the checksum
        FixChecksum(dataSpan);
    }

    /// <summary>
    /// Asynchronously processes SaveData files from the specified input folder, optionally switching platform and updating user IDs, while saving the processed files to a new output directory.
    /// </summary>
    /// <param name="inputFolderPath">The path to the folder containing the SaveData files to be processed. This folder must exist and contain valid SaveData files.</param>
    /// <param name="switchPlatform">A boolean value indicating whether to switch the platform of the SaveData files during processing. If true, the platform will be switched.</param>
    /// <param name="newUserId">An optional new user ID to set in the SaveData files. This parameter is only used if the files are not console SaveData files.</param>
    /// <param name="cts">A CancellationTokenSource used to signal cancellation of the processing operation. If cancellation is requested, the operation will stop processing files.</param>
    public async Task ProcessSaveDataFilesAsync(string inputFolderPath, bool switchPlatform, string? newUserId, CancellationTokenSource cts)
        => await Task.Run(() => ProcessSaveDataFiles(inputFolderPath, switchPlatform, newUserId, cts));

    /// <summary>
    /// Processes SaveData files from the specified input folder, optionally switching platform and updating user IDs, while saving the processed files to a new output directory.
    /// </summary>
    /// <param name="inputFolderPath">The path to the folder containing the SaveData files to be processed. This folder must exist and contain valid SaveData files.</param>
    /// <param name="switchPlatform">A boolean value indicating whether to switch the platform of the SaveData files during processing. If true, the platform will be switched.</param>
    /// <param name="newUserId">An optional new user ID to set in the SaveData files. This parameter is only used if the files are not console SaveData files.</param>
    /// <param name="cts">A CancellationTokenSource used to signal cancellation of the processing operation. If cancellation is requested, the operation will stop processing files.</param>
    public void ProcessSaveDataFiles(string inputFolderPath, bool switchPlatform, string? newUserId, CancellationTokenSource cts)
    {
        // Get all SaveData files from the input folder
        var filesToProcess = GetSaveDataFiles(inputFolderPath);
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory();
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(filesToProcess, inputFolderPath, outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        var progress = 0;
        try
        {
            Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
            {
                while (true)
                {
                    var group = $"Task {ctr}";
                    var fileName = Path.GetFileName(filesToProcess[ctr]);

                    // Try to read file data
                    Span<byte> dataSpan;
                    try { dataSpan = File.ReadAllBytes(filesToProcess[ctr]); }
                    catch (Exception ex)
                    {
                        logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to read the [{fileName}] file: {ex}", group);
                        break; // Skip to the next file
                    }

                    // Process the file data
                    ProcessSaveData(ref dataSpan, switchPlatform, newUserId);

                    // Try to save the processed file data
                    try
                    {
                        var outputFilePath = filesToProcess[ctr].Replace(inputFolderPath, outputDir);
                        File.WriteAllBytes(outputFilePath, dataSpan);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Failed to save the file: {ex}", group);
                        break; // Skip to the next file
                    }
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Processed the [{fileName}] file.", group);
                    break;
                }
                Interlocked.Increment(ref progress);
                progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
            });
            logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex.Message);
        }
        finally
        {
            // Ensure progress is set to 100% at the end
            progressReporter.Report(100);
        }
    }
}