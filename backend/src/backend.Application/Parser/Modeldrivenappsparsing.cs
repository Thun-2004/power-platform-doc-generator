using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace backend.Application.Parser;

public static class ModelDrivenAppsParsing
{
    /// <summary>
    /// Detects model-driven apps in an unpacked Power Platform solution.
    ///
    /// Model-driven apps are NOT .msapp files.
    /// They appear as:
    ///   - Subfolders/files under AppModules/
    ///   - RootComponent entries with type="80" in solution.xml
    ///   - *.appmodule.xml files scattered in the solution tree
    /// </summary>
    public static List<string> DetectModelDrivenApps(DirectoryInfo solutionRoot)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        
        var appModulesDir = FsHelpers.FindDirCaseInsensitive(solutionRoot, "AppModules");
        if (appModulesDir != null && appModulesDir.Exists)
        {
            foreach (var item in FsHelpers.SafeListDir(appModulesDir))
            {
                if (item is DirectoryInfo d)
                {
                    found.Add(d.Name);
                }
                else if (item is FileInfo f)
                {
                    var cleanName = Path.GetFileNameWithoutExtension(f.Name)
                        .Replace(".appmodule", "", StringComparison.OrdinalIgnoreCase)
                        .Trim();
                    if (!string.IsNullOrWhiteSpace(cleanName))
                        found.Add(cleanName);
                }
            }
        }

      
        var solutionXml = solutionRoot
            .GetFiles("solution.xml", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (solutionXml != null)
        {
            try
            {
                var doc = XDocument.Load(solutionXml.FullName);

                var appModuleRefs = doc.Descendants()
                    .Where(el =>
                        string.Equals(el.Name.LocalName, "RootComponent", StringComparison.OrdinalIgnoreCase) &&
                        el.Attribute("type")?.Value == "80")
                    .Select(el =>
                        el.Attribute("schemaName")?.Value ??
                        el.Attribute("id")?.Value ?? "")
                    .Where(n => !string.IsNullOrWhiteSpace(n));

                foreach (var name in appModuleRefs)
                    found.Add(name.Trim());
            }
            catch
            {
               
            }
        }

      
        try
        {
            foreach (var f in solutionRoot.GetFiles("*.appmodule.xml", SearchOption.AllDirectories))
            {
                var cleanName = f.Name
                    .Replace(".appmodule.xml", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
                if (!string.IsNullOrWhiteSpace(cleanName))
                    found.Add(cleanName);
            }
        }
        catch { }

         
        try
        {
            foreach (var f in solutionRoot.GetFiles("*.appmodule", SearchOption.AllDirectories))
            {
                var cleanName = Path.GetFileNameWithoutExtension(f.Name).Trim();
                if (!string.IsNullOrWhiteSpace(cleanName))
                    found.Add(cleanName);
            }
        }
        catch { }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
