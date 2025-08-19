
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.Helpers;
using VRCFaceTracking.Models;

namespace VRCFaceTracking.Core.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly ILocalSettingsService _localSettingsService;
    private static readonly string CustomLibsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"VRCFaceTracking\CustomLibs");
    private const string ConfigSaveKey = "CustomConfigurations";
    private const string ActiveConfigKey = "ActiveConfigurationId";

    private List<string> _allKnownModules = new() { "2.vive focus vision", "1.htc facial", "3.bigscreen eye" };
    public List<string> AllKnownModules => _allKnownModules;

    public List<CustomConfiguration> Configurations { get; private set; } = new();
    public CustomConfiguration? ActiveConfiguration { get; private set; }

    public ConfigurationService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public async Task InitializeAsync()
    {
        // Discover available modules from filesystem
        RefreshAvailableModules();
        
        Configurations = await _localSettingsService.ReadSettingAsync<List<CustomConfiguration>>(ConfigSaveKey) ?? await CreateDefaultConfigurations();

        var activeConfigId = await _localSettingsService.ReadSettingAsync<Guid?>(ActiveConfigKey);
        if (activeConfigId.HasValue)
        {
            ActiveConfiguration = Configurations.FirstOrDefault(c => c.Id == activeConfigId.Value);
        }
        
        if (ActiveConfiguration == null && Configurations.Any())
        {
            ActiveConfiguration = Configurations.First();
        }

        // Ensure IsActive flags are correct
        if (ActiveConfiguration != null)
        {
            await SetActiveConfigurationAsync(ActiveConfiguration.Id);
        }
    }

    public async Task SetActiveConfigurationAsync(Guid configId)
    {
        var targetConfiguration = Configurations.FirstOrDefault(c => c.Id == configId);
        if (targetConfiguration == null)
            return; 

        ActiveConfiguration = targetConfiguration;

        foreach (var config in Configurations)
        {
            config.IsActive = config.Id == targetConfiguration.Id;
        }

        foreach (var moduleName in AllKnownModules)
        {
            var shouldBeEnabled = ActiveConfiguration.ActiveModuleNames.Contains(moduleName);
            MoveModule(moduleName, shouldBeEnabled);
        }

        await SaveAsync();
    }

    private async Task<List<CustomConfiguration>> CreateDefaultConfigurations()
    {
        var viveConfig = new CustomConfiguration
        {
            Name = "Vive Focus Vision",
            ActiveModuleNames = new List<string> { "2.vive focus vision" },
            IsActive = true,
        };

        var bigscreenConfig = new CustomConfiguration
        {
            Name = "Bigscreen Beyond",
            ActiveModuleNames = new List<string> { "1.htc facial", "3.bigscreen eye" },
        };

        var configs = new List<CustomConfiguration> { viveConfig, bigscreenConfig };
        
        ActiveConfiguration = viveConfig;

        await _localSettingsService.SaveSettingAsync(ConfigSaveKey, configs);
        await _localSettingsService.SaveSettingAsync(ActiveConfigKey, ActiveConfiguration.Id);

        return configs;
    }

    public async Task SaveAsync()
    {
        await _localSettingsService.SaveSettingAsync(ConfigSaveKey, Configurations);
        await _localSettingsService.SaveSettingAsync(ActiveConfigKey, ActiveConfiguration?.Id);
    }

    private void MoveModule(string moduleName, bool enable)
    {
        var enabledPath = Path.Combine(CustomLibsDirectory, moduleName);
        var disabledPath = Path.Combine(CustomLibsDirectory, ".disable", moduleName);
        var disableDirectory = Path.Combine(CustomLibsDirectory, ".disable");

        // Ensure .disable directory exists
        if (!Directory.Exists(disableDirectory))
        {
            Directory.CreateDirectory(disableDirectory);
        }

        if (enable)
        {
            if (Directory.Exists(disabledPath))
            {
                Directory.Move(disabledPath, enabledPath);
            }
        }
        else
        {
            if (Directory.Exists(enabledPath))
            {
                Directory.Move(enabledPath, disabledPath);
            }
        }
    }

    public void SetConfiguration(string configurationName)
    {
        var config = Configurations.FirstOrDefault(c => c.Name == configurationName);
        if (config != null)
        {
            Task.Run(async () => await SetActiveConfigurationAsync(config.Id)).Wait();
        }
    }
    
    public void RefreshAvailableModules()
    {
        var discoveredModules = new List<string>();
        
        // Add default modules
        discoveredModules.AddRange(new[] { "2.vive focus vision", "1.htc facial", "3.bigscreen eye" });
        
        // Scan CustomLibs directory for additional modules
        if (Directory.Exists(CustomLibsDirectory))
        {
            var directories = Directory.GetDirectories(CustomLibsDirectory)
                .Where(dir => !Path.GetFileName(dir).StartsWith(".")) // Exclude .disable folder
                .Select(dir => Path.GetFileName(dir))
                .Where(name => !string.IsNullOrEmpty(name));
            
            foreach (var dirName in directories)
            {
                if (!discoveredModules.Contains(dirName))
                {
                    discoveredModules.Add(dirName);
                }
            }
        }
        
        // Scan .disable directory for disabled modules
        var disableDirectory = Path.Combine(CustomLibsDirectory, ".disable");
        if (Directory.Exists(disableDirectory))
        {
            var disabledDirectories = Directory.GetDirectories(disableDirectory)
                .Select(dir => Path.GetFileName(dir))
                .Where(name => !string.IsNullOrEmpty(name));
            
            foreach (var dirName in disabledDirectories)
            {
                if (!discoveredModules.Contains(dirName))
                {
                    discoveredModules.Add(dirName);
                }
            }
        }
        
        _allKnownModules = discoveredModules;
    }
}
