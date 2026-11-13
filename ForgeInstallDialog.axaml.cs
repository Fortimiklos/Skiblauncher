using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.VersionMetadata;

namespace Skiblauncher;

public partial class ForgeInstallDialog : Window
{
    public class DataBack
    {
        public string? Version { get; set; }
        public bool IsForge { get; set; }

        public DataBack(string? version, bool isForge)
        {
            Version =  version;
            IsForge = isForge;
        }
    }

    protected static bool _forge = true;
    public ForgeInstallDialog()
    {
        InitializeComponent();
        PageStart();
    }

    private async void PageStart()
    {
        //ForgeSelected.IsChecked = true;
        var launcher = new MinecraftLauncher();
        var versions =  await launcher.GetAllVersionsAsync();
        foreach (var version in versions)
        {
            if (version.Type == "release")
            {
                VanillaVersionBox.Items.Add(version.Name);
            }
            
        }
        VanillaVersionBox.SelectedIndex = 0;

        
    }
    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        switch (NeoForgeSelected.IsChecked)
        {
            case true: _forge = false; break;
            case false: _forge = true; break;
        }
        
        var dataBack = new DataBack(VanillaVersionBox.SelectedItem.ToString(),_forge);
        Close(dataBack);
    }
}