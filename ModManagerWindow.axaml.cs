using Avalonia;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using Microsoft.VisualBasic.CompilerServices;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;

namespace Skiblauncher;

public class ModListObjects
{
    public string ModName { get; set; }
    public bool OnState { get; set; }
    public ModListObjects(string modName,bool onState)
    {
        ModName =  modName;
        OnState = onState;
    }

    public override string ToString()
    {
        return $"{OnState} {ModName}";
    }

}
public partial class ModManagerWindow : ContentPage
{
    private MinecraftPath _minecraftPath = new();
    public ModManagerWindow()
    {
        InitializeComponent();
        ModListLoad();
        forgeListRefresh();
        ForgeVersions.SelectedIndex = 0;
    }

    private async void ModrinthButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ResizeTweak();
        await Navigation.PushAsync(new ModrinthContentPage());
        
    }
    
    private async void CurseForgeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ResizeTweak();
        await Navigation.PushAsync(new CurseForgeContentPage());
        
    }
    
//just a resize tweak for linux NativeWebView render bug
    private async void ResizeTweak()
    {
        await Task.Delay(50);
        var window = TopLevel.GetTopLevel(this) as Window;
        if (window != null)
        {
            double currentWidth = window.Bounds.Width;
            double currentHeight = window.Bounds.Height;
            window.Width = currentWidth + 100;
            window.Height = currentHeight + 100;
            await Task.Delay(200);
            window.Width = currentWidth;
            window.Height = currentHeight;
        }
        
    }

    private void ModListLoad()
    {
        //Console.WriteLine("skib");
        List<string> ModLoaderMods = new();
        List<string> Mods = new();
        var mcfolder = _minecraftPath;
        if (!Directory.Exists($"{mcfolder}/modloader/mods"))
        {
            Directory.CreateDirectory($"{mcfolder}/modloader/mods");
        }
        if(!Directory.Exists($"{mcfolder}/mods")){
            Directory.CreateDirectory($"{mcfolder}/mods");}

        foreach (var directory in Directory.GetFiles($"{mcfolder}/modloader/mods"))
        {
            //Console.WriteLine(directory);
            ModLoaderMods.Add(Path.GetFileName(directory));
        }
        
        foreach (var directory in Directory.GetFiles($"{mcfolder}/mods"))
        {
            
            Mods.Add(Path.GetFileName(directory));
        }
        
        ModList.Items.Clear();
        bool onstate = false;
        foreach (var mod in ModLoaderMods)
        {
            onstate = false;
            string sigma = mod;
            if (Mods.Contains(sigma)){onstate = true;}

            Console.WriteLine(sigma);
            ModList.Items.Add(new ModListObjects(mod,onstate));
            
        }
        
    }

    private async void ModInstall_OnClick(object? sender, RoutedEventArgs e)
    {
        var modPath = Path.Combine($"{_minecraftPath}/modloader/mods");
        var ForgeModPath = Path.Combine($"{_minecraftPath}/mods");
        var window = TopLevel.GetTopLevel(this) as Window;
        var storage = window.StorageProvider;
        var JarFileType = new FilePickerFileType( "Java Archives")
        {
            Patterns = new []{"*.jar"},
            MimeTypes = new []{"application/java-archive"}
            
        };
        var openFilePicker = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            FileTypeFilter = new[]{JarFileType},
            AllowMultiple = true,
        });
        IStorageFolder? ModLoaderFolder = await storage.TryGetFolderFromPathAsync(modPath);
        IStorageFolder? ModFolder = await storage.TryGetFolderFromPathAsync(ForgeModPath);
        if (openFilePicker.Count != 0)
        {
            foreach (var file in openFilePicker)
            {
                await file.MoveAsync(ModLoaderFolder);
                var skib  = file.Path.LocalPath;
                Console.WriteLine(skib);
                File.Copy(skib,$"{ForgeModPath}/{file.Name}");
            }
            
        }
        ModListLoad();
    }

    private void ModListRefresh_OnClick(object? sender, RoutedEventArgs e)
    {
        ModListLoad();
        forgeListRefresh();
    }

    private void DeleteMod_OnClick(object? sender, RoutedEventArgs e)
    {
        
        if (sender is Button button)
        {
            var item = button.FindAncestorOfType<ListBoxItem>();
            if (item != null)
            {
                item.IsSelected = true;
            }
            string SelectedModName = ModList.SelectedItem.ToString().Split(" ")[1];
            File.Delete($"{_minecraftPath}/modloader/mods/{SelectedModName}");
            if(File.Exists($"{_minecraftPath}/mods/{SelectedModName}"))
            {
                File.Delete($"{_minecraftPath}/mods/{SelectedModName}");
            }
            ModListLoad();
        }
    }

    private void Checkbox_OnClick(object? sender, RoutedEventArgs e)
    {
        var mcfolder = _minecraftPath;
        if (sender is Button button)
        {
            var item = button.FindAncestorOfType<ListBoxItem>();
            if (item != null)
            {
                item.IsSelected = true;
            }
            string SelectedModName = ModList.SelectedItem.ToString().Split(" ")[1];
            bool SelectedItemState = bool.Parse(ModList.SelectedItem.ToString().Split(" ")[0]);
            if (SelectedItemState && !File.Exists($"{mcfolder}/mods/{SelectedModName}"))
            {
                //kell a fájl az mcfolder/modloader/mods -ból és másold a mcfolder/mods -ba
                File.Copy($"{mcfolder}/modloader/mods/{SelectedModName}",$"{mcfolder}/mods/{SelectedModName}");
            }
            else if(!SelectedItemState && File.Exists($"{mcfolder}/mods/{SelectedModName}"))
            {
                //ide pedig a törlés de csak a mcfolder/mods -ból
                File.Delete($"{mcfolder}/mods/{SelectedModName}");
            }
            ModListLoad();
        }
    }

    private async void StartForge()
    {
        var mcFolder = _minecraftPath;
        var launcher =  new MinecraftLauncher();
        string? selectedForgeVersion =  ForgeVersions.SelectedItem.ToString();
        
        
        string[] tomb = new string[5];
        int minRAM = 1024;
        int maxRAM = 2048;
        string username = "Steve";
        tomb = File.ReadAllLines($"{mcFolder}/accsave/settings.skib");
        if (tomb.Length != 0)
        {
            minRAM = int.Parse(tomb[0].Split(':')[1]);
            maxRAM = int.Parse(tomb[1].Split(':')[1]);
            username = tomb[2].Split(':')[1];
        }

        if (selectedForgeVersion != null)
        {
            var process = await launcher.BuildProcessAsync(selectedForgeVersion, new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(username),
                MaximumRamMb = maxRAM,
                MinimumRamMb = minRAM,
            });
            Console.WriteLine("indult");
            StartForgeButton.Content = "Indul...";
            StartForgeButton.IsEnabled = false;
            process.Start(); 
            process.WaitForExit();
            Console.WriteLine("megállt");
            StartForgeButton.Content = "Forge Indítása";
            StartForgeButton.IsEnabled = true;
        }
        
    }

    private async void ForgeInstall_OnClick(object? sender, RoutedEventArgs e)
    {
        ForgeInstaller forgeInstaller = new ForgeInstaller(new MinecraftLauncher());
        var neoForgeInstaller = new NeoForgeInstaller(new  MinecraftLauncher());
        var mcFolder = _minecraftPath;
        var dialog = new ForgeInstallDialog();
        var result = await dialog.ShowDialog<ForgeInstallDialog.DataBack?>(TopLevel.GetTopLevel(this) as Window);
        Console.WriteLine(result);
        if (result != null)
        {
            if (result.IsForge)
            {
                await forgeInstaller.Install(result.Version);
            }
            else
            {
                await neoForgeInstaller.Install(result.Version);
            }
        }

        Console.WriteLine("siker");
        forgeListRefresh();
        
    }

    private void forgeListRefresh()
    {
        ForgeVersions.Items.Clear();
        var mcFolder = _minecraftPath;
        foreach (var directory in Directory.GetDirectories($"{mcFolder}/versions"))
        {
            if (directory.Contains("forge"))
            {
                ForgeVersions.Items.Add(directory.Split('/').Last());
            }
        }
    }

    private void StartForgeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        StartForge();
    }
}