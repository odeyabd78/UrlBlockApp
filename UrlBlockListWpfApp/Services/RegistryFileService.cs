using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace UrlBlockListWpfApp.Services;

public interface IRegistryFileService
{
    Task ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

public class RegistryFileService: IRegistryFileService
{
    public async Task ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "The registry file was not found.",
                fullPath);
        }

        if (!string.Equals(
                Path.GetExtension(fullPath),
                ".reg",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The selected file must be a .reg file.",
                nameof(filePath));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "reg.exe",
            Arguments = $"import \"{fullPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();

        Task<string> outputTask =
            process.StandardOutput.ReadToEndAsync(cancellationToken);

        Task<string> errorTask =
            process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        string output = await outputTask;
        string error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Failed to import registry file.{Environment.NewLine}" +
                $"Exit code: {process.ExitCode}{Environment.NewLine}" +
                $"{error}");
        }
    }
    
    
    public Task  ReplaceBlockList(
        string registryPath,
        IEnumerable<string> fileLines,
        CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<string> urls = fileLines
            .Select(TryExtractRegistryValue)
            .Where(value => value is not null)
            .Cast<string>()
            .ToList();

        using RegistryKey policyKey =
            Registry.LocalMachine.CreateSubKey(
                registryPath,
                writable: true)
            ?? throw new InvalidOperationException(
                $"Could not open registry path: {registryPath}");

        // Remove every old value first.
        foreach (string valueName in policyKey.GetValueNames())
        {
            cancellationToken.ThrowIfCancellationRequested();

            policyKey.DeleteValue(
                valueName,
                throwOnMissingValue: false);
        }

        // Write only the values currently shown in the preview.
        for (int index = 0; index < urls.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            policyKey.SetValue(
                (index + 1).ToString(),
                urls[index],
                RegistryValueKind.String);
        }

        policyKey.Flush();
        return Task.CompletedTask;
    }

    private static string? TryExtractRegistryValue(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        int separatorIndex = line.IndexOf(
            "\"=\"",
            StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            return null;
        }

        string value = line[(separatorIndex + 3)..];

        if (value.EndsWith('"'))
        {
            value = value[..^1];
        }

        return value
            .Replace("\\\"", "\"")
            .Replace(@"\\", @"\");
    }
}