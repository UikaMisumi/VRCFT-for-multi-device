
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Models;
using WinRT.Interop;

namespace VRCFaceTracking.ViewModels
{
    public partial class ConfigurationViewModel : ObservableRecipient
    {
        private readonly IConfigurationService _configurationService;

        [ObservableProperty]
        private CustomConfiguration? _selectedConfiguration;

        [ObservableProperty]
        private string? _selectedIncludedModule;

        [ObservableProperty]
        private string? _selectedAvailableModule;
        
        // Module Import Properties
        [ObservableProperty]
        private string? _selectedImportPath;
        
        [ObservableProperty]
        private string _importModuleName = string.Empty;
        
        [ObservableProperty]
        private bool _canImportModule;
        
        [ObservableProperty]
        private bool _showImportStatus;
        
        [ObservableProperty]
        private InfoBarSeverity _importStatusSeverity = InfoBarSeverity.Informational;
        
        [ObservableProperty]
        private string _importStatusTitle = string.Empty;
        
        [ObservableProperty]
        private string _importStatusMessage = string.Empty;

        public ObservableCollection<CustomConfiguration> Configurations { get; }
        public ObservableCollection<string> IncludedModules { get; } = new();
        public ObservableCollection<string> AvailableModules { get; } = new();

        public CustomConfiguration? ActiveConfiguration => _configurationService?.ActiveConfiguration;
        
        public string ActiveConfigurationModules => ActiveConfiguration?.ActiveModuleNames?.Count > 0 
            ? $"Active modules: {string.Join(", ", ActiveConfiguration.ActiveModuleNames)}"
            : "No modules active";

        public ConfigurationViewModel(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            Configurations = new ObservableCollection<CustomConfiguration>();
            
            // Initialize asynchronously to avoid blocking UI
            _ = InitializeAsync();
        }
        
        private async Task InitializeAsync()
        {
            try
            {
                await _configurationService.InitializeAsync();
                
                // Update collections on UI thread
                Configurations.Clear();
                foreach (var config in _configurationService.Configurations)
                {
                    Configurations.Add(config);
                }
                
                var activeConfig = Configurations.FirstOrDefault(c => c.Id == _configurationService.ActiveConfiguration?.Id);
                SelectedConfiguration = activeConfig;
                UpdateModuleLists();
                
                // Notify property changes
                OnPropertyChanged(nameof(ActiveConfiguration));
                OnPropertyChanged(nameof(ActiveConfigurationModules));
            }
            catch (Exception ex)
            {
                // Handle initialization errors gracefully
                System.Diagnostics.Debug.WriteLine($"ConfigurationViewModel initialization error: {ex.Message}");
            }
        }

        partial void OnSelectedConfigurationChanged(CustomConfiguration? value)
        {
            UpdateModuleLists();
        }

        private void UpdateModuleLists()
        {
            IncludedModules.Clear();
            AvailableModules.Clear();

            if (SelectedConfiguration?.ActiveModuleNames == null || _configurationService?.AllKnownModules == null)
                return;

            foreach (var module in SelectedConfiguration.ActiveModuleNames)
            {
                IncludedModules.Add(module);
            }

            foreach (var module in _configurationService.AllKnownModules)
            {
                if (!IncludedModules.Contains(module))
                {
                    AvailableModules.Add(module);
                }
            }
        }
        
        partial void OnSelectedImportPathChanged(string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                ImportModuleName = Path.GetFileName(value);
                CanImportModule = !_configurationService.AllKnownModules.Contains(ImportModuleName);
            }
            else
            {
                ImportModuleName = string.Empty;
                CanImportModule = false;
            }
        }
        
        [RelayCommand]
        private async Task BrowseModuleFolder()
        {
            try
            {
                var folderPicker = new FolderPicker();
                folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
                folderPicker.FileTypeFilter.Add("*");
                
                // Get the current window handle for the picker
                var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
                InitializeWithWindow.Initialize(folderPicker, hwnd);
                
                var folder = await folderPicker.PickSingleFolderAsync();
                if (folder != null)
                {
                    SelectedImportPath = folder.Path;
                }
            }
            catch (Exception ex)
            {
                ShowImportError("Browse Failed", $"Failed to open folder picker: {ex.Message}");
            }
        }
        
        [RelayCommand]
        private void CancelImport()
        {
            SelectedImportPath = null;
            ShowImportStatus = false;
        }
        
        [RelayCommand]
        private async Task ImportModule()
        {
            if (string.IsNullOrEmpty(SelectedImportPath) || string.IsNullOrEmpty(ImportModuleName))
                return;
                
            try
            {
                var customLibsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"VRCFaceTracking\CustomLibs");
                var destinationPath = Path.Combine(customLibsDirectory, ImportModuleName);
                
                // Ensure CustomLibs directory exists
                if (!Directory.Exists(customLibsDirectory))
                {
                    Directory.CreateDirectory(customLibsDirectory);
                }
                
                // Check if module already exists
                if (Directory.Exists(destinationPath))
                {
                    ShowImportError("Import Failed", "A module with this name already exists.");
                    return;
                }
                
                // Copy the folder
                await Task.Run(() => CopyDirectory(SelectedImportPath, destinationPath));
                
                // Update the service to detect new modules
                await RefreshAvailableModules();
                
                ShowImportSuccess("Import Successful", $"Module '{ImportModuleName}' has been imported successfully.");
                
                // Clear import state
                SelectedImportPath = null;
            }
            catch (Exception ex)
            {
                ShowImportError("Import Failed", $"Failed to import module: {ex.Message}");
            }
        }
        
        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
                
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);
            
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }
            
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir);
            }
        }
        
        private async Task RefreshAvailableModules()
        {
            // Refresh the module discovery and reinitialize
            _configurationService.RefreshAvailableModules();
            
            // Update all configurations on the UI thread
            Configurations.Clear();
            foreach (var config in _configurationService.Configurations)
            {
                Configurations.Add(config);
            }
            
            UpdateModuleLists();
            
            // Save to persist the updated module list
            await _configurationService.SaveAsync();
        }
        
        private void ShowImportSuccess(string title, string message)
        {
            ImportStatusSeverity = InfoBarSeverity.Success;
            ImportStatusTitle = title;
            ImportStatusMessage = message;
            ShowImportStatus = true;
        }
        
        private void ShowImportError(string title, string message)
        {
            ImportStatusSeverity = InfoBarSeverity.Error;
            ImportStatusTitle = title;
            ImportStatusMessage = message;
            ShowImportStatus = true;
        }

        [RelayCommand]
        private void AddModule()
        {
            if (SelectedAvailableModule != null && SelectedConfiguration != null)
            {
                SelectedConfiguration.ActiveModuleNames.Add(SelectedAvailableModule);
                UpdateModuleLists();
            }
        }

        [RelayCommand]
        private void RemoveModule()
        {
            if (SelectedIncludedModule != null && SelectedConfiguration != null)
            {
                SelectedConfiguration.ActiveModuleNames.Remove(SelectedIncludedModule);
                UpdateModuleLists();
            }
        }

        [RelayCommand]
        private async Task AddNew()
        {
            var newConfig = new CustomConfiguration
            {
                Name = "New Configuration"
            };
            Configurations.Add(newConfig);
            _configurationService.Configurations.Add(newConfig);
            SelectedConfiguration = newConfig;
            await _configurationService.SaveAsync();
        }

        [RelayCommand]
        private async Task Delete()
        {
            if (SelectedConfiguration != null)
            {
                var configToRemove = SelectedConfiguration;
                var configIndex = Configurations.IndexOf(configToRemove);
                Configurations.Remove(configToRemove);
                _configurationService.Configurations.Remove(configToRemove);

                if (Configurations.Count > 0)
                {
                    SelectedConfiguration = Configurations[configIndex > 0 ? configIndex - 1 : 0];
                }
                else
                {
                    SelectedConfiguration = null;
                }
                await _configurationService.SaveAsync();
            }
        }

        [RelayCommand]
        private async Task SetActive()
        {
            if (SelectedConfiguration != null)
            {
                await _configurationService.SetActiveConfigurationAsync(SelectedConfiguration.Id);
                OnPropertyChanged(nameof(ActiveConfiguration));
                OnPropertyChanged(nameof(ActiveConfigurationModules));
                
                // Reload modules just like MainViewModel does
                var libManager = App.GetService<VRCFaceTracking.Core.Contracts.Services.ILibManager>();
                App.MainWindow.DispatcherQueue.TryEnqueue(() => 
                {
                    libManager.TeardownAllAndResetAsync();
                    libManager.Initialize();
                });
            }
        }

        [RelayCommand]
        private async Task SaveChanges()
        {
            await _configurationService.SaveAsync();
        }
    }
}
