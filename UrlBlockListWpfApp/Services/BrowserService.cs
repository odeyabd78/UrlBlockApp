using System.Diagnostics;

namespace UrlBlockListWpfApp.Services;

public interface IBrowserService
{
    Task RestartAsync(
        BrowserType browserType, CancellationToken cancellationToken = default);
}

public sealed class BrowserService : IBrowserService
{
    public async Task RestartAsync(
        BrowserType browserType, CancellationToken cancellationToken = default)
    {
        string processName;
        switch (browserType)
        {
            case BrowserType.Chrome:
                processName = "chrome";
                break;
            case BrowserType.Edge:
                processName = "msedge";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(browserType), browserType, null);
        }

       await CloseBrowserAsync(
         // await CloseAllBrowserProcessesAsync(
            processName: processName,
            cancellationToken);

        StartWithPreviousSession(processName);
    }

    private static async Task CloseBrowserAsync(
        string processName,
        CancellationToken cancellationToken)
    {
        Process[] processes =
            Process.GetProcessesByName(processName);

        foreach (Process process in processes)
        {
            using (process)
            {
                try
                {
                    // Only processes that own a browser window
                    // normally have a non-zero MainWindowHandle.
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    process.CloseMainWindow();

                    try
                    {
                        await process.WaitForExitAsync(cancellationToken)
                            .WaitAsync(
                                TimeSpan.FromSeconds(10),
                                cancellationToken);
                    }
                    catch (TimeoutException)
                    {
                        // Do not kill automatically here.
                        // The browser may be showing a confirmation dialog.
                    }
                }
                catch (InvalidOperationException)
                {
                    // The process already exited.
                }
            }
        }
    }

    private static async Task CloseAllBrowserProcessesAsync(
        string processName,
        CancellationToken cancellationToken)
    {
        Process[] processes =
            Process.GetProcessesByName(processName);

        foreach (Process process in processes)
        {
            try
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    process.CloseMainWindow();
                }
            }
            catch
            {
                // Process may already have exited.
            }
            finally
            {
                process.Dispose();
            }
        }

        await Task.Delay(
            TimeSpan.FromSeconds(3),
            cancellationToken);

        processes = Process.GetProcessesByName(processName);

        foreach (Process process in processes)
        {
            using (process)
            {
                try
                {
                    process.Kill(entireProcessTree: true);

                    await process.WaitForExitAsync(
                        cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    // Already closed.
                }
            }
        }
    }

    private static void StartWithPreviousSession(string processName)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = $"{processName}.exe",
            Arguments = "--restore-last-session",
            UseShellExecute = true
        });
    }
}