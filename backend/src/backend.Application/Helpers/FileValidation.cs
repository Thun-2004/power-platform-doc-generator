using System.IO.Compression;

namespace backend.Application.Helpers; 

public class FileValidation
{
  
    public static void ValidateSolutionZipOrThrow(string zipPath)
    {
        try
        {
            using var fs = File.OpenRead(zipPath);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false);

            // Check it contains CanvasApps folder and at least one .msapp (case-insensitive)
            var normalizedEntries = zip.Entries
                .Select(e => e.FullName.Replace('\\', '/'))
                .ToList();

            bool hasCanvasAppsFolder = normalizedEntries.Any(n =>
                n.StartsWith("CanvasApps/", StringComparison.OrdinalIgnoreCase) ||
                n.Contains("/CanvasApps/", StringComparison.OrdinalIgnoreCase));
            bool hasMsapp = normalizedEntries.Any(n =>
                n.Contains("canvasapps", StringComparison.OrdinalIgnoreCase) &&
                n.EndsWith(".msapp", StringComparison.OrdinalIgnoreCase));

            if (!hasCanvasAppsFolder || !hasMsapp)
                throw new ArgumentException("No CanvasApps folder detected. Please upload a valid Power Platform solution package (.zip).");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidDataException)
        {
            throw new ArgumentException("The file is not a valid zip or is corrupted. Please upload a valid Power Platform solution package (.zip).");
        }
        catch (IOException)
        {
            throw new ArgumentException("The file could not be read. Please upload a valid Power Platform solution package (.zip).");
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException("Invalid or corrupted zip file. Please upload a valid Power Platform solution package (.zip).", ex);
        }
    }
}
