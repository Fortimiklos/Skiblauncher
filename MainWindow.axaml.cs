using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Auth;

namespace Skiblauncher;


public partial class MainWindow : Window
{
    private static int minRAM = 1024;
    static int maxRAM = 2048;
    static string username;
    static int selectedVersion = 0;
    bool snapshotState = false;
    MinecraftLauncher launcher = new MinecraftLauncher();
    private List<string> downloadedVersions = new List<string>();
    private List<string> snapshots = new List<string>();
    private List<string> fullVersions = new List<string>();
    MinecraftPath mcFolder = new MinecraftPath();

    public MainWindow()
    {
        InitializeComponent();
        launcherStart();

    }

    private async void launcherStart()
    {
        var versions = await launcher.GetAllVersionsAsync();
        if (!File.Exists($"{mcFolder}/laucher_profiles.json"))
        {
            File.WriteAllText($"{mcFolder}/laucher_profiles.json", "{\"profiles\":{}}");
        }
        if (!File.Exists($"{mcFolder}/accsave/settings.skib"))
        {
            try
            {
                if (!Directory.Exists($"{mcFolder}/accsave"))
                {
                    Directory.CreateDirectory($"{mcFolder}/accsave");
                }


                File.Create($"{mcFolder}/accsave/settings.skib");
                Console.WriteLine("létrejött");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("sikertelen mentés fájl létrehozás");
            }

        }

        try
        {
            string[] tomb = new string[5];
            tomb = File.ReadAllLines($"{mcFolder}/accsave/settings.skib");
            Console.WriteLine("beolavas");
            if (tomb.Length != 0)
            {
                minRAM = int.Parse(tomb[0].Split(':')[1]);
                maxRAM = int.Parse(tomb[1].Split(':')[1]);
                username = tomb[2].Split(':')[1];
                selectedVersion = int.Parse(tomb[3].Split(':')[1]);
                snapshotState = tomb[4].Split(':')[1] == "True" ? true : false;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        

        foreach (var version in versions)
        {
            if (version.Type != "release")
            {
                snapshots.Add(version.Name);
            }
            else
            {
                fullVersions.Add(version.Name);
            }
        }

        ToggleButton_OnIsCheckedChanged(null, null);
        user.Text = username;
        snapshotsBox.IsChecked = snapshotState;
        foreach (var version in Directory.GetDirectories($"{mcFolder}/versions"))
        {
            if (!version.Contains("forge"))
            {
                downloadedVersions.Add(version.Split('/').Last());
            }
            
        }

        verziok.SelectedIndex = selectedVersion;
        verziok_SelectionChanged(null, null);
    }

    private void verziok_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!downloadedVersions.Contains(verziok.SelectedItem))
        {
            startButton.Content = "Letöltés";
        }

        if (downloadedVersions.Contains(verziok.SelectedItem))
        {
            startButton.Content = "Indítás";
        }

        selectedVersion = verziok.SelectedIndex;
    }

    private void saveSettings()
    {

        username = user.Text;
        selectedVersion = verziok.SelectedIndex;
        if (snapshotsBox.IsChecked == true)
        {
            snapshotState = true;
        }
        else
        {
            snapshotState = false;
        }

        Console.WriteLine(verziok.SelectedIndex);
        Console.WriteLine(selectedVersion);
        StreamWriter file = new StreamWriter($"{mcFolder}/accsave/settings.skib");
        file.WriteLine(
            $"minram:{minRAM}\nmaxram:{maxRAM}\nusername:{username}\nselectedVersion:{selectedVersion}\nsnapshotState:{snapshotState}");
        file.Close();
    }

    private async void downloadMC()
    {
        startButton.IsEnabled = false;
        saveSettings();
        ProgressBar.IsVisible = true;
        launcher.ByteProgressChanged += (sender, args) =>
        {
            ProgressBar.Value = (args.ProgressedBytes / args.TotalBytes) * 100;
        };
        await launcher.InstallAsync(verziok.SelectedItem.ToString());
        downloadedVersions.Add(verziok.SelectedItem.ToString());
        startButton.Content = "Indítás";
        startButton.IsEnabled = true;
        ProgressBar.IsVisible = false;
    }

    private async void startMC()
    {
        if (user.Text == "")
        {
            user.Text = "Steve";
        }

        if (user.Text != "")
        {
            saveSettings();
            startButton.IsEnabled = false;
            var process = await launcher.BuildProcessAsync(verziok.SelectedItem.ToString(), new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(user.Text),
                MaximumRamMb = maxRAM,
                MinimumRamMb = minRAM,
                /*ExtraJvmArguments = new MArgument[]
                {
                    new MArgument($"-javaagent:{mcFolder}/accsave/authlib-injector-1.2.7.jar={mcFolder}/accsave/skin/skib.png")
                }*/
                //skin java agent még kell + progressbar
            });
            process.Start();
            startButton.IsEnabled = true;

        }
    }

    private void ToggleButton_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        verziok.Clear();
        if (DownloadedVersions.IsChecked == true)
        {
            DownloadedVersions.IsChecked = false;
        }
        if (snapshotsBox.IsChecked == true)
        {
            verziok.ItemsSource = snapshots;

        }
        else
        {
            verziok.ItemsSource = fullVersions;
        }

        verziok.SelectedIndex = selectedVersion;
    }

    private void StartButton_OnClick(object? sender, RoutedEventArgs e)
    {
        saveSettings();
        if (downloadedVersions.Contains(verziok.SelectedItem))
        {
            startMC();
        }
        else
        {
            downloadMC();
        }
    }
    
    private async void ViewSwap_OnClick(object? sender, RoutedEventArgs e)
    {
        saveSettings();
        await Navigation.PushAsync(new ModManagerWindow());
    }

    private void DownloadedVersions_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        verziok.Clear();
        if (snapshotsBox.IsChecked == true)
        {
            snapshotsBox.IsChecked = false;
            
        }
        if (DownloadedVersions.IsChecked == true)
        {
            verziok.ItemsSource = downloadedVersions;
        }
        else
        {
            verziok.ItemsSource =  fullVersions;
        }

        verziok.SelectedIndex = 0;
    }
}
    
