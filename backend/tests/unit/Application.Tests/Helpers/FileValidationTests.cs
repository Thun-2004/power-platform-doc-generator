using System.IO.Compression;
using backend.Application.Helpers;

namespace Application.Tests.Helpers;

public class FileValidationTests
{
    [Fact]
    public void ValidateSolutionZipOrThrow_WhenCanvasAppsAndMsappPresent_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "app-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var zipPath = Path.Combine(tempDir, "solution.zip");

        try
        {
            using (var fs = File.Create(zipPath))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: false))
            {
                zip.CreateEntry("CanvasApps/someapp.msapp");
            }

            var ex = Record.Exception(() => FileValidation.ValidateSolutionZipOrThrow(zipPath));

            Assert.Null(ex);
        }
        finally
        {
            try { File.Delete(zipPath); } catch { /* ignore */ }
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void ValidateSolutionZipOrThrow_WhenEmptyZip_ThrowsArgumentException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "app-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var zipPath = Path.Combine(tempDir, "empty.zip");

        try
        {
            File.WriteAllBytes(zipPath, Array.Empty<byte>());

            Assert.Throws<ArgumentException>(() => FileValidation.ValidateSolutionZipOrThrow(zipPath));
        }
        finally
        {
            try { File.Delete(zipPath); } catch { /* ignore */ }
            try { Directory.Delete(tempDir, recursive: true); } catch { /* ignore */ }
        }
    }
}