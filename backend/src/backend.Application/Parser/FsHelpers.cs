using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace backend.Application.Parser;

public static class FsHelpers
{
    public static bool IsIgnored(string name) =>
        name.StartsWith(".") || name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase);

    public static IEnumerable<FileSystemInfo> SafeListDir(DirectoryInfo dir)
    {
        if (!dir.Exists) return Enumerable.Empty<FileSystemInfo>();
        try
        {
            return dir.EnumerateFileSystemInfos()
                .Where(x => !IsIgnored(x.Name))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return Enumerable.Empty<FileSystemInfo>();
        }
    }

    public static DirectoryInfo? FindDirCaseInsensitive(DirectoryInfo root, string targetName)
    {
        foreach (var item in SafeListDir(root))
            if (item is DirectoryInfo d && d.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return d;
        return null;
    }

    public static string SafeReadAllText(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8); }
        catch
        {
            try { return File.ReadAllText(path); }
            catch { return ""; }
        }
    }

    public static string RelPath(DirectoryInfo baseDir, string fullPath)
    {
        try
        {
            var b = baseDir.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (fullPath.StartsWith(b, StringComparison.OrdinalIgnoreCase))
                return fullPath.Substring(b.Length);
        }
        catch { }
        return fullPath;
    }

    public static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);

        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(src.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var destFile = Path.Combine(dst, relative);
            var destFolder = Path.GetDirectoryName(destFile);

            if (!string.IsNullOrWhiteSpace(destFolder))
                Directory.CreateDirectory(destFolder);

            File.Copy(file, destFile, overwrite: true);
        }
    }

    public static void RemoveDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch { }
    }

}

