
using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VRCFaceTracking.Models
{
    public partial class CustomConfiguration : ObservableObject
    {
        [ObservableProperty]
        private Guid _id;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private List<string> _activeModuleNames;

        [ObservableProperty]
        private bool _isActive;

        public CustomConfiguration()
        {
            Id = Guid.NewGuid();
            Name = "New Configuration";
            ActiveModuleNames = new List<string>();
            IsActive = false;
        }
    }
}
