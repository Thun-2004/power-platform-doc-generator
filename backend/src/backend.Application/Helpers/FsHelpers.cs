using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace backend.Application.Helpers;

// Summary: Provides helper methods for working with the filesystem in a robust, parser-friendly way.
public static class FsHelpers
{
    // Summary: Returns true if a filesystem entry name should be ignored (hidden or known junk files).
    public static bool IsIgnored(string name) =>
        name.StartsWith(".") || name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase);

    // Summary: Safely enumerates directory contents, filtering ignored entries and ordering by name.
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

    // Summary: Finds a direct child directory under root whose name matches the target, ignoring case.
    public static DirectoryInfo? FindDirCaseInsensitive(DirectoryInfo root, string targetName)
    {
        foreach (var item in SafeListDir(root))
            if (item is DirectoryInfo d && d.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                return d;
        return null;
    }

    // Summary: Reads all text from a file using UTF-8 first and falls back to default encoding, returning empty on failure.
    public static string SafeReadAllText(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8); }
        catch
        {
            try { return File.ReadAllText(path); }
            catch { return ""; }
        }
    }

    // Summary: Computes a path relative to baseDir if possible, otherwise returns the original full path.
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

    // Summary: Recursively copies all files from a source directory to a destination directory, preserving structure.
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

    // Summary: Attempts to delete a directory tree if it exists, ignoring failures.
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

