
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
}
