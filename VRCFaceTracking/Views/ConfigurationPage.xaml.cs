
using Microsoft.UI.Xaml.Controls;
using VRCFaceTracking.ViewModels;

namespace VRCFaceTracking.Views
{
    public sealed partial class ConfigurationPage : Page
    {
        public ConfigurationViewModel ViewModel { get; }

        public ConfigurationPage()
        {
            ViewModel = App.GetService<ConfigurationViewModel>();
            InitializeComponent();
        }
    }
}
