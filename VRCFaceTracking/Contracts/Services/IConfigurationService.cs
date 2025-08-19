
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using VRCFaceTracking.Models;

namespace VRCFaceTracking.Core.Contracts.Services;

public interface IConfigurationService
{
    List<CustomConfiguration> Configurations { get; }
    CustomConfiguration? ActiveConfiguration { get; }

    List<string> AllKnownModules { get; }

    Task InitializeAsync();

    // Legacy method for compatibility, will be phased out
    void SetConfiguration(string configurationName);

    Task SetActiveConfigurationAsync(Guid configId);

    void RefreshAvailableModules();

    Task SaveAsync();
}
