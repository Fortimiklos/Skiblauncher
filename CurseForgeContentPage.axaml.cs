using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Skiblauncher;
public partial class CurseForgeContentPage : ContentPage
{
    public CurseForgeContentPage()
    {
        InitializeComponent();
        Downloader downloads = new Downloader();
        CurseForge.NavigationStarted += (sender, e) =>
        {
            //Console.WriteLine(e.Request);
            if (e.Request.ToString().Contains("download"))
            {
                CurseForge.Stop();
                downloads.Download(e.Request.ToString());
            }
        };
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        CurseForge.GoBack();
    }
}