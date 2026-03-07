namespace deathloop_savedata_checksum_fixer_cli.Infrastructure;

/// <summary>
/// Provides utility methods and properties for managing application directory paths, including the root and output directories.
/// </summary>
public static class Directories
{
    public static readonly string RootPath = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string Output = Path.Combine(RootPath, "_OUTPUT");
    
    /// <summary>
    /// Generates a new output directory path using the current date and time.
    /// </summary>
    /// <returns>A string representing the full path of the new output directory, formatted with the current date and time.</returns>
    public static string GetNewOutputDirectory()
        => Path.Combine(Output, $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}");

    /// <summary>
    /// Creates the output folder structure by replicating the parent directories of the specified input files under the given output directory.
    /// </summary>
    /// <param name="filesToProcess">An array of file paths representing the files to process. Each file's parent directory will be recreated under the output directory.</param>
    /// <param name="inputRootPath">The root path of the input directory structure. This path is replaced with the output directory when creating the new folder structure.</param>
    /// <param name="outputDirectory">The path to the root output directory where the folder structure will be created.</param>
    public static void CreateOutputFolderStructure(string[] filesToProcess, string inputRootPath, string outputDirectory)
    {
        var uniqueParentDirectories = filesToProcess
            .Select(Path.GetDirectoryName)
            .Where(dir => dir != null)
            .Distinct()
            .Select(dir => dir?.Replace(inputRootPath, outputDirectory))
            .ToArray();
        foreach (var dir in uniqueParentDirectories)
        {
            if (dir == null) continue;
            Directory.CreateDirectory(dir);
        }
    }
}