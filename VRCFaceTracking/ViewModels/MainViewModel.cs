using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using VRCFaceTracking.Core.Contracts;
using VRCFaceTracking.Core.Contracts.Services;
using VRCFaceTracking.Core.OSC;
using VRCFaceTracking.Core.Services;
using VRCFaceTracking.Models;
using System.Collections.ObjectModel;

namespace VRCFaceTracking.ViewModels;

public partial class MainViewModel : ObservableRecipient
{
    public ILibManager LibManager { get; }
    public IConfigurationService ConfigurationService { get; }
    public OscQueryService ParameterOutputService { get; }
    public OscRecvService OscRecvService { get; }
    public OscSendService OscSendService { get; }
    public IOscTarget OscTarget { get; }
    
    [ObservableProperty] private ObservableCollection<CustomConfiguration> _configurations = new();
    [ObservableProperty] private CustomConfiguration? _selectedConfiguration;

    private int _messagesRecvd;
    [ObservableProperty] private int _messagesInPerSec;

    private int _messagesSent;
    [ObservableProperty] private int _messagesOutPerSec;

    [ObservableProperty] private bool _noModulesInstalled;
    
    [ObservableProperty] private bool _oscWasDisabled;

    private DispatcherTimer msgCounterTimer;

    public MainViewModel(
        ILibManager libManager,
        IConfigurationService configurationService,
        OscQueryService parameterOutputService,
        IModuleDataService moduleDataService,
        IOscTarget oscTarget,
        OscRecvService oscRecvService,
        OscSendService oscSendService
        )
    {
        //Services
        LibManager = libManager;
        ConfigurationService = configurationService;
        ParameterOutputService = parameterOutputService;
        OscTarget = oscTarget;
        OscRecvService = oscRecvService;
        OscSendService = oscSendService;
        
        // Initialize configuration
        InitializeConfigurationAsync();
        
        // Modules
        var installedNewModules = moduleDataService.GetInstalledModules();
        var installedLegacyModules = moduleDataService.GetLegacyModules().Count();
        NoModulesInstalled = !installedNewModules.Any() && installedLegacyModules == 0;
        
        // Message Timer
        OscRecvService.OnMessageReceived += MessageReceived;
        OscSendService.OnMessagesDispatched += MessageDispatched;
        msgCounterTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        msgCounterTimer.Tick += (_, _) =>
        {
            MessagesInPerSec = _messagesRecvd;
            _messagesRecvd = 0;
            
            MessagesOutPerSec = _messagesSent;
            _messagesSent = 0;
        };
        msgCounterTimer.Start();
    }

    private void MessageReceived(OscMessage msg) => _messagesRecvd++;
    private void MessageDispatched(int msgCount) => _messagesSent += msgCount;

    private async void InitializeConfigurationAsync()
    {
        try
        {
            await ConfigurationService.InitializeAsync();
            
            Configurations.Clear();
            foreach (var config in ConfigurationService.Configurations)
            {
                Configurations.Add(config);
            }
            
            SelectedConfiguration = ConfigurationService.ActiveConfiguration;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Configuration initialization failed: {ex.Message}");
        }
    }
    
    partial void OnSelectedConfigurationChanged(CustomConfiguration? value)
    {
        if (value != null && value != ConfigurationService.ActiveConfiguration)
        {
            ChangeConfigurationAsync(value);
        }
    }
    
    private async void ChangeConfigurationAsync(CustomConfiguration configuration)
    {
        try
        {
            // 设置新的配置
            await ConfigurationService.SetActiveConfigurationAsync(configuration.Id);
            
            // 重新加载模组 - 使用与ModuleRegistryPage相同的方式
            App.MainWindow.DispatcherQueue.TryEnqueue(() => 
            {
                LibManager.TeardownAllAndResetAsync();
                LibManager.Initialize();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to change configuration: {ex.Message}");
        }
    }

    ~MainViewModel()
    {
        OscRecvService.OnMessageReceived -= MessageReceived;
        OscSendService.OnMessagesDispatched -= MessageDispatched;
        
        msgCounterTimer.Stop();
    }
}
