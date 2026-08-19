# Browser URL Blocker

A modern WPF application for managing blocked website policies for **Google Chrome** and **Microsoft Edge** on Windows.

The application provides a graphical interface for adding and removing URLs from the browsers' `URLBlocklist` policies without manually editing registry files.

## Features

* Add a URL to the Chrome and Edge block lists
* Preview the generated registry policies before applying them
* Select and remove existing URLs from the policy
* Apply changes to Windows Registry
* Restart Chrome automatically after applying changes
* Restart Edge automatically after applying changes
* Choose independently whether Chrome or Edge should restart
* Modern WPF interface
* MVVM architecture
* Dependency Injection
* Configuration through `appsettings.json`
* Async commands using `CommunityToolkit.Mvvm`

## Technologies

* C#
* .NET 10
* WPF
* MVVM
* Microsoft.Extensions.Hosting
* Microsoft.Extensions.DependencyInjection
* Microsoft.Extensions.Configuration
* Microsoft.Extensions.Options
* CommunityToolkit.Mvvm
* Windows Registry Policies

## Registry Policies

The application manages the following Windows policies.

### Google Chrome

```text
HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Google\Chrome\URLBlocklist
```

### Microsoft Edge

```text
HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge\URLBlocklist
```

For example:

```reg
"1"="youtube.com"
"2"="facebook.com"
"3"="example.com"
```

## How It Works

### Adding a URL

1. Enter a URL or domain in the URL field.
2. Click **Add**.
3. The application adds the URL to both policy previews.
4. Review the Chrome and Edge registry policy previews.
5. Click **Apply** to save the changes.

### Removing a URL

1. Select the URL entry in the Chrome or Edge policy preview.
2. Click **Delete**.
3. The entry is removed from the preview.
4. Click **Apply** to save the modified policy.

Deleting an entry from the preview alone does not modify Windows Registry. The change is committed when **Apply** is pressed.

## Browser Restart Options

Before applying the policy, you can independently select:

* **Restart Google Chrome**
* **Restart Microsoft Edge**

Both options can be enabled at the same time.

If neither option is selected, the policies are applied without restarting either browser.

## Checking Applied Policies

Chrome policies can be inspected at:

```text
chrome://policy
```

Edge policies can be inspected at:

```text
edge://policy
```

Use **Reload policies** on these pages if you want to verify the currently loaded policy.

Look for:

```text
URLBlocklist
```

## Administrator Permissions

Writing policies under:

```text
HKEY_LOCAL_MACHINE
```

requires administrator privileges.

The application can either run with administrator privileges or elevate only the registry operation.

When using `reg.exe`, elevation can be requested with:

```csharp
var startInfo = new ProcessStartInfo
{
    FileName = "reg.exe",
    Arguments = $"import \"{filePath}\"",
    UseShellExecute = true,
    Verb = "runas"
};
```

This causes Windows to display a UAC prompt when the policy is applied.

## Configuration

Application configuration is stored in:

```text
appsettings.json
```

The application loads configuration using the .NET Generic Host:

```csharp
_host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((context, configuration) =>
    {
        configuration.SetBasePath(AppContext.BaseDirectory);

        configuration.AddJsonFile(
            "appsettings.json",
            optional: false,
            reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IRegistryFileService, RegistryFileService>();
        services.AddSingleton<IBrowserService, BrowserService>();
        services.AddSingleton<IBrowserPolicyService, BrowserPolicyService>();

        services
            .AddOptions<BlockListOptions>()
            .Bind(context.Configuration);

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();
    })
    .Build();
```

## Project Structure

```text
UrlBlockListWpfApp
│
├── Assets
│   ├── AppIcon.ico
│   ├── ChromeIcon.png
│   └── EdgeIcon.png
│
├── Services
│   ├── IBrowserPolicyService.cs
│   ├── IBrowserService.cs
│   ├── IRegistryFileService.cs
│   ├── BrowserPolicyService.cs
│   ├── BrowserService.cs
│   └── RegistryFileService.cs
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── MainWindowViewModel.cs
├── BlockListOptions.cs
├── appsettings.json
└── UrlBlockListWpfApp.csproj
```

The exact structure may differ slightly as the application develops.

## Building

Restore NuGet packages:

```powershell
dotnet restore
```

Build the project:

```powershell
dotnet build
```

Run it:

```powershell
dotnet run --project UrlBlockListWpfApp
```

You can also open the solution in JetBrains Rider and run the WPF project directly.

## Important

This application modifies Windows browser policies.

Review the generated policy before pressing **Apply**, and make sure the application only modifies the intended Chrome and Edge `URLBlocklist` registry locations.
