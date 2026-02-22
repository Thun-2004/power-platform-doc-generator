using System;
using System.IO;

namespace backend.Application.Helpers; 
public class EnvReader
{
    public static string Load(string name)
    {
        var v = Environment.GetEnvironmentVariable(name);
        
        if (string.IsNullOrWhiteSpace(v))
            throw new Exception($"Missing env var: {name}");
        return v!;
    }
}
