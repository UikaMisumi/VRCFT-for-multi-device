# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VRCFaceTracking is a Windows application that provides eye tracking and lip tracking in VRChat by bridging tracking hardware and VRChat's OSC server. The solution consists of three main projects:

- **VRCFaceTracking** - WinUI 3 desktop application (main GUI)
- **VRCFaceTracking.Core** - Core tracking logic and OSC communication
- **VRCFaceTracking.SDK** - SDK for external tracking modules

## Build and Development Commands

### Building the Solution
```bash
# Build entire solution
dotnet build VRCFaceTracking.sln

# Build specific project
dotnet build VRCFaceTracking/VRCFaceTracking.csproj

# Build and publish for Windows x64
dotnet publish VRCFaceTracking/VRCFaceTracking.csproj -c Release -r win-x64 --self-contained

# Run the application directly
"VRCFaceTracking\bin\Debug\net8.0-windows10.0.19041.0\win-x64\publish\VRCFaceTracking.exe" --no-gui
```

### Platform Targets
The main application supports multiple platforms:
- x86, x64, ARM64 for Windows
- Target framework: .NET 8.0 (Windows 10.0.19041.0+)

## Architecture Overview

### Core Application Flow
1. **App.xaml.cs** - Application entry point with dependency injection setup
2. **MainStandalone.cs** - Core service that manages tracking lifecycle
3. **UnifiedTracking** - Central tracking data processing
4. **OSC Services** - Handle VRChat communication via OSC protocol

### Key Services (Dependency Injection)
- `IMainService` (MainStandalone) - Core application lifecycle
- `ILibManager` (UnifiedLibManager) - External module management
- `UnifiedTrackingMutator` - Parameter processing and mutations
- `OscSendService`/`OscRecvService` - OSC communication
- `ParameterSenderService` - Parameter distribution
- `ModuleDataService` - Tracking module data management

### UI Architecture (WinUI 3)
- **MVVM Pattern** - ViewModels in `ViewModels/`, Views in `Views/`
- **Navigation** - Shell-based navigation with NavigationView
- **Services** - UI services for theming, settings, navigation
- **Pages**: Main, Parameters, Mutator, Output, Settings, ModuleRegistry

### Core Tracking System
- **UnifiedExpressions** - Standardized face tracking parameters
- **Parameter Processing** - Mutations, filtering, calibration
- **Module System** - External tracking hardware integration
- **OSC Query** - VRChat parameter discovery and configuration

### Configuration and Data
- Settings stored via `ILocalSettingsService`
- Avatar configs parsed from VRChat OSC files
- Module metadata and installation via `ModuleInstaller`
- Custom configurations via `IConfigurationService`

## Development Notes

### No Test Framework
This project does not have automated tests. Manual testing is required.

### Debugging and Logs
- Sentry integration for error reporting (both debug and release)
- Comprehensive logging throughout application
- Output logging available in the UI (OutputPage)

### External Dependencies
- Native OSC library: `fti_osc.dll/.dylib/.so` (platform-specific)
- OpenVR integration for VR headset detection
- WinUI 3 for modern Windows UI

### Module System
External tracking modules can be developed using the SDK. Modules are loaded dynamically and must implement the tracking interfaces defined in VRCFaceTracking.SDK.