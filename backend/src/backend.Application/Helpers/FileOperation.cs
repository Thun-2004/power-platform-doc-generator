using System.IO.Compression;

namespace backend.Application.Helpers; 

public class FileOperation
{
    public static void CopyDirectory(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir))
            throw new DirectoryNotFoundException($"Source not found: {sourceDir}");

        if (Directory.Exists(destDir))
            Directory.Delete(destDir, true);

        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, true);
        }
    }

    public static void RemoveDirectory(string targetDir)
    {
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Source not found: {targetDir}");

        Directory.Delete(targetDir, true); 
       
    }

    public static void ValidateSolutionZipOrThrow(string zipPath)
    {
        try
        {
            using var fs = File.OpenRead(zipPath);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

            // Check it contains CanvasApps/*.msapp (case-insensitive)
            bool hasMsapp = zip.Entries.Any(e =>
                e.FullName.Replace('\\', '/')
                .Contains("canvasapps/", StringComparison.OrdinalIgnoreCase)
                && e.FullName.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase)
            );

            if (!hasMsapp)
                throw new ArgumentException("Invalid solution zip: missing CanvasApps/*.msapp");

            bool hasWorkflows = zip.Entries.Any(e =>
                e.FullName.Replace('\\', '/')
                .StartsWith("workflows/", StringComparison.OrdinalIgnoreCase)
            );

        }
        catch (InvalidDataException)
        {
            throw new ArgumentException("Invalid zip file (corrupted or not a zip).");
        }
    }
}
