using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace backend.Application.Parser;

public static class ModelDrivenAppsParsing
{
<<<<<<< HEAD
=======
    /// <summary>
    /// Detects model-driven apps in an unpacked Power Platform solution.
    ///
    /// Model-driven apps are NOT .msapp files.
    /// They appear as:
    ///   - Subfolders/files under AppModules/
    ///   - RootComponent entries with type="80" in solution.xml
    ///   - *.appmodule.xml files scattered in the solution tree
    /// </summary>
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
    public static List<string> DetectModelDrivenApps(DirectoryInfo solutionRoot)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

<<<<<<< HEAD
=======
        
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
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
<<<<<<< HEAD

=======
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
                    if (!string.IsNullOrWhiteSpace(cleanName))
                        found.Add(cleanName);
                }
            }
        }

<<<<<<< HEAD
=======
      
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
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
<<<<<<< HEAD
            }
        }

=======
               
            }
        }

      
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
        try
        {
            foreach (var f in solutionRoot.GetFiles("*.appmodule.xml", SearchOption.AllDirectories))
            {
                var cleanName = f.Name
                    .Replace(".appmodule.xml", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
<<<<<<< HEAD

=======
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
                if (!string.IsNullOrWhiteSpace(cleanName))
                    found.Add(cleanName);
            }
        }
<<<<<<< HEAD
        catch
        {
        }

=======
        catch { }

         
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
        try
        {
            foreach (var f in solutionRoot.GetFiles("*.appmodule", SearchOption.AllDirectories))
            {
                var cleanName = Path.GetFileNameWithoutExtension(f.Name).Trim();
                if (!string.IsNullOrWhiteSpace(cleanName))
                    found.Add(cleanName);
            }
        }
<<<<<<< HEAD
        catch
        {
        }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
=======
        catch { }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
>>>>>>> e57b607 (refactor: integrate with Dara's code + add backend/README.md)
