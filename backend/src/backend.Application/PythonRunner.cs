using System;
using System.Diagnostics;
using System.Threading.Tasks;

public static class PythonRunner
{
    // public static async Task<(int exitCode, string stdout, string stderr)> RunAsync(
    //     string pythonExe, string scriptPath, string args = "")
    // {
    //     var psi = new ProcessStartInfo
    //     {
    //         FileName = pythonExe,                 // e.g. "python3" or "/usr/bin/python3"
    //         Arguments = $"\"{scriptPath}\" {args}",
    //         RedirectStandardOutput = true,
    //         RedirectStandardError = true,
    //         UseShellExecute = false,
    //         CreateNoWindow = true
    //     };

    //     using var p = new Process { StartInfo = psi };
    //     p.Start();

    //     string stdout = await p.StandardOutput.ReadToEndAsync();
    //     string stderr = await p.StandardError.ReadToEndAsync();

    //     await p.WaitForExitAsync();
    //     // return (p.ExitCode, stdout, stderr);
    //     return p.ExitCode; 

    // }

        public static async Task<(int exitCode, string stdout, string stderr)> RunAsync(
            string pythonExe, string scriptPath, string args = "")
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" {args}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var p = new Process { StartInfo = psi };
            p.Start();

            string stdout = await p.StandardOutput.ReadToEndAsync();
            string stderr = await p.StandardError.ReadToEndAsync();

            await p.WaitForExitAsync();
            return (p.ExitCode, stdout, stderr);
        }
    
}