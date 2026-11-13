using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using System.Diagnostics;
using Avalonia.Interactivity;

namespace Skiblauncher;

public partial class ModrinthContentPage : ContentPage
{
    public ModrinthContentPage()
    {
        InitializeComponent();
        Downloader downloads = new Downloader();
        Modrinth.NavigationStarted += (sender, e) =>
        {
            if (e.Request.ToString().Split("/").Contains("data"))
            {
                //Console.WriteLine("skib");
                Modrinth.Stop();
                downloads.Download(e.Request.ToString());
            }
            //Console.WriteLine(e.Request);

        };
    }
    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Modrinth.GoBack();
    }
}

public class Downloader
{  
    public void Download(string link)
    {
        Process.Start(new  ProcessStartInfo
        {
            FileName = link,
            UseShellExecute = true
        });
    }


}
    
    
