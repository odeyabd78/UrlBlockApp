using System.IO;
using Microsoft.Extensions.Options;

namespace UrlBlockListWpfApp.Services;

public interface IBrowserPolicyService
{
    Task ConfirmChrome(string[] text, bool restart = true, CancellationToken cancellationToken = default);
    Task ConfirmEdge(string[] text, bool restart = true, CancellationToken cancellationToken = default);

    Task<string[]> LoadChromeFile(
        CancellationToken cancellationToken = default);

    Task<string[]> LoadEdgeFile(
        CancellationToken cancellationToken = default);

    Task<string[]> AddChromeUrlBlock(string url,
        CancellationToken cancellationToken = default);

    Task<string[]> AddEdgeUrlBlock(string url,
        CancellationToken cancellationToken = default);
}

public class BrowserPolicyService(
    IOptions<BlockListOptions> blockOptions,
    IRegistryFileService registryFileService,
    IBrowserService browserService)
    : IBrowserPolicyService
{
    private const char QuotationMarksChar = '\"';


    public Task ConfirmChrome(string[] text, bool restart = true, CancellationToken cancellationToken = default)
        => Confirm(BrowserType.Chrome, blockOptions.Value.ChromePath, text, restart, cancellationToken);

    public Task ConfirmEdge(string[] text, bool restart = true, CancellationToken cancellationToken = default)
        => Confirm(BrowserType.Edge, blockOptions.Value.EdgePath, text, restart, cancellationToken);

    public async Task<string[]> AddChromeUrlBlock(string url,
        CancellationToken cancellationToken = default) =>
        await ReadAndAddNewUrlBlock(blockOptions.Value.ChromePath, url, cancellationToken);

    public async Task<string[]> AddEdgeUrlBlock(string url,
        CancellationToken cancellationToken = default) =>
        await ReadAndAddNewUrlBlock(blockOptions.Value.EdgePath, url, cancellationToken);

    public async Task<string[]> LoadChromeFile(CancellationToken cancellationToken = default) =>
        await Load(blockOptions.Value.ChromePath, cancellationToken);

    public async Task<string[]> LoadEdgeFile(CancellationToken cancellationToken = default) =>
        await Load(blockOptions.Value.EdgePath, cancellationToken);

    private static async Task<string[]> Load(string path,
        CancellationToken cancellationToken = default) => await File.ReadAllLinesAsync(path, cancellationToken);

    private static async Task<string[]> ReadAndAddNewUrlBlock(string path, string url,
        CancellationToken cancellationToken = default)
    {
        var text = await Load(path, cancellationToken);
        var lastLine = text.Last();
        var lastNumber = lastLine[1..lastLine.IndexOf(QuotationMarksChar, 1)];
        if (!int.TryParse(lastNumber, out var number))
            return text;
        var lst = text.ToList();
        lst.Add($"{QuotationMarksChar}{number + 1}{QuotationMarksChar}={QuotationMarksChar}{url}{QuotationMarksChar}");
        return lst.ToArray();
    }

    private async Task Confirm(BrowserType browserType, string path, string[] text, bool restart = true,
        CancellationToken cancellationToken = default)
    {
        await File.WriteAllLinesAsync(path, text, cancellationToken);
        var registryKey = text.FirstOrDefault(x => x.Contains("URLBlocklist"))!.Replace("[HKEY_LOCAL_MACHINE\\", "").Replace("]", "");
        await registryFileService.ImportAsync(path, cancellationToken);
        if (restart)
            await browserService.RestartAsync(browserType, cancellationToken);
    }
}