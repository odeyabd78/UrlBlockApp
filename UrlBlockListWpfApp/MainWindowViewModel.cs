using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using UrlBlockListWpfApp.Services;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IBrowserPolicyService _policyService;

    private string _statusMessage = "Ready";
    private bool _isBusy;
    private string[] _chromeFile = [];
    private string[] _edgeFile = [];
    private bool _restartChrome = true;
    private bool _restartEdge = true;
    private string? _selectedChromeLine;
    private string? _selectedEdgeLine;

    public bool RestartChrome
    {
        get => _restartChrome;
        set
        {
            if (_restartChrome == value)
            {
                return;
            }

            _restartChrome = value;
            OnPropertyChanged();
        }
    }

    public bool RestartEdge
    {
        get => _restartEdge;
        set
        {
            if (_restartEdge == value)
            {
                return;
            }

            _restartEdge = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel(
        IBrowserPolicyService policyService)
    {
        _policyService = policyService;
        ReadAndAddCommand = new AsyncRelayCommand(
            ReadAndAddAsync,
            () => !IsBusy);
        LoadCommand = new AsyncRelayCommand(
            LoadAsync,
            () => !IsBusy);
        ApplyCommand = new AsyncRelayCommand(
            ApplyAsync,
            () => !IsBusy);
        DeleteChromeLineCommand = new RelayCommand(
            DeleteSelectedChromeLine,
            CanDeleteChromeLine);

        DeleteEdgeLineCommand = new RelayCommand(
            DeleteSelectedEdgeLine,
            CanDeleteEdgeLine);
    }

    public ICommand ReadAndAddCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ApplyCommand { get; }
    public ICommand DeleteChromeLineCommand { get; }

    public ICommand DeleteEdgeLineCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string[] EdgeFileLines
    {
        get => _edgeFile;
        private set
        {
            _edgeFile = value;
            OnPropertyChanged();
        }
    }

    public string[] ChromeFileLines
    {
        get => _chromeFile;
        private set
        {
            _chromeFile = value;
            OnPropertyChanged();
        }
    }

    public string? SelectedChromeLine
    {
        get => _selectedChromeLine;
        set
        {
            if (_selectedChromeLine == value)
            {
                return;
            }

            _selectedChromeLine = value;
            OnPropertyChanged();

            if (DeleteChromeLineCommand is RelayCommand command)
            {
                command.NotifyCanExecuteChanged();
            }
        }
    }

    public string? SelectedEdgeLine
    {
        get => _selectedEdgeLine;
        set
        {
            if (_selectedEdgeLine == value)
            {
                return;
            }

            _selectedEdgeLine = value;
            OnPropertyChanged();

            if (DeleteEdgeLineCommand is RelayCommand command)
            {
                command.NotifyCanExecuteChanged();
            }
        }
    }

    private string _url = string.Empty;

    public string Url
    {
        get => _url;
        set
        {
            if (_url == value)
            {
                return;
            }

            _url = value;
            OnPropertyChanged();
            if (ReadAndAddCommand is AsyncRelayCommand command)
            {
                NotifyCommandsCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();

            if (ApplyCommand is AsyncRelayCommand command)
            {
                NotifyCommandsCanExecuteChanged();
            }
        }
    }

    private async Task ReadAndAddAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ChromeFileLines = await _policyService.AddChromeUrlBlock(Url, cancellationToken);
            EdgeFileLines = await _policyService.AddEdgeUrlBlock(Url, cancellationToken);


            StatusMessage =
                "The policies were applied successfully.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage =
                "Administrator permission is required.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            ChromeFileLines = await _policyService.LoadChromeFile( cancellationToken);
            EdgeFileLines = await _policyService.LoadEdgeFile( cancellationToken);


            StatusMessage =
                "The files were load successfully.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage =
                "Administrator permission is required.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDeleteChromeLine()
    {
        return !IsBusy &&
               IsRegistryValueLine(SelectedChromeLine);
    }

    private bool CanDeleteEdgeLine()
    {
        return !IsBusy &&
               IsRegistryValueLine(SelectedEdgeLine);
    }

    private void DeleteSelectedChromeLine()
    {
        if (!IsRegistryValueLine(SelectedChromeLine))
        {
            return;
        }

        ChromeFileLines = RemoveLineAndRenumber(
            ChromeFileLines,
            SelectedChromeLine!);

        SelectedChromeLine = null;

        StatusMessage =
            "The selected Chrome entry was removed from the preview. Press Apply to save it.";
    }

    private void DeleteSelectedEdgeLine()
    {
        if (!IsRegistryValueLine(SelectedEdgeLine))
        {
            return;
        }

        EdgeFileLines = RemoveLineAndRenumber(
            EdgeFileLines,
            SelectedEdgeLine!);

        SelectedEdgeLine = null;

        StatusMessage =
            "The selected Edge entry was removed from the preview. Press Apply to save it.";
    }

    private static bool IsRegistryValueLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string trimmed = line.Trim();

        // Only allow deletion of lines such as:
        // "1"="youtube.com"
        return trimmed.StartsWith('"') &&
               trimmed.Contains("\"=\"", StringComparison.Ordinal);
    }

    private static string[] RemoveLineAndRenumber(
        IEnumerable<string> fileLines,
        string selectedLine)
    {
        List<string> remainingLines = fileLines
            .Where(line => !string.Equals(
                line,
                selectedLine,
                StringComparison.Ordinal))
            .ToList();

        int nextRegistryIndex = 1;

        for (int index =remainingLines.FindIndex(x=>x.Contains("URLAllowlist")); index < remainingLines.Count; index++)
        {
            string line = remainingLines[index];

            if (!IsRegistryValueLine(line))
            {
                continue;
            }

            int separatorPosition = line.IndexOf(
                "\"=\"",
                StringComparison.Ordinal);

            if (separatorPosition < 0)
            {
                continue;
            }

            string valuePart = line[(separatorPosition + 2)..].Replace("\"","");

            remainingLines[index] =
                $"\"{nextRegistryIndex}\"=\"{valuePart}\"";

            nextRegistryIndex++;
        }

        return remainingLines.ToArray();
    }

    private void NotifyCommandsCanExecuteChanged()
    {
        if (ReadAndAddCommand is AsyncRelayCommand addCommand)
        {
            addCommand.NotifyCanExecuteChanged();
        }

        if (LoadCommand is AsyncRelayCommand loadCommand)
        {
            loadCommand.NotifyCanExecuteChanged();
        }

        if (ApplyCommand is AsyncRelayCommand applyCommand)
        {
            applyCommand.NotifyCanExecuteChanged();
        }

        if (DeleteChromeLineCommand is RelayCommand chromeCommand)
        {
            chromeCommand.NotifyCanExecuteChanged();
        }

        if (DeleteEdgeLineCommand is RelayCommand edgeCommand)
        {
            edgeCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task ApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Applying chrome browser policies...";

            await _policyService.ConfirmChrome(
                ChromeFileLines, RestartChrome, cancellationToken);

            StatusMessage = "Applying edge browser policies...";

            await _policyService.ConfirmEdge(
                EdgeFileLines, RestartEdge, cancellationToken);


            StatusMessage =
                "The policies were applied successfully.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage =
                "Administrator permission is required.";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}