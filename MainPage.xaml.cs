using ConsoleApp1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static System.Net.Mime.MediaTypeNames;
using Syroot.Windows.IO;
using System.Data;
using Windows.UI.Popups;
using AngleSharp.Html.Dom;
using YoutubeExplode.Playlists;
using Google.Apis.YouTube.v3.Data;
using AngleSharp.Dom;


// Документацию по шаблону элемента "Пустая страница" см. по адресу https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x419

namespace YTExtractor
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        YTAudioExtractor extractor;
        enum WarningType {InvalidUrl, NotYTUrl, PlaylistNotFound, VideoNotFound};

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 225));
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            extractor = new YTAudioExtractor();
        }

        private void OnUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                Download.IsEnabled = false;
                UrlBox.Text = string.Empty;
                return;
            }
            Download.IsEnabled = true;
        }

        private async void OnUrlPasted(object sender, TextControlPasteEventArgs e)
        {
            TextBox urlBox = sender as TextBox;
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                try
                {
                    var text = await dataPackageView.GetTextAsync();

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Download.IsEnabled = false;
                        urlBox.Text = string.Empty;
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }
            }
            Download.IsEnabled = true;
            await InitiateDownloadSequence();
        }

        private void OnUrlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            
        }

        private async void OnDownloadPressed(object sender, RoutedEventArgs e)
        {
            await InitiateDownloadSequence();
        }

        private async Task InitiateDownloadSequence()
        {
            string url = UrlBox.Text;

            // это нормальная ссылка?
            if (!extractor.IsUrl(url))
                await BadUrlWarning(url, WarningType.InvalidUrl);

            // это (почти) ссылка на ютуб?
            else if (!url.Contains("youtu"))
                await BadUrlWarning(url, WarningType.NotYTUrl);

            // это ссылка на плейлист?
            else if (url.Contains("playlist"))
                await DownloadPlaylist(url);

            // это видео состоит в плейлисте?
            else if (url.Contains("&list="))
                await DownloadInPlaylist(url);

            // а иначе это просто ссылка на видео
            else await DownloadOne(url);
        }

        private async void OnSelectFolderPressed(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                //Windows.Storage.AccessCache.StorageApplicationPermissions.
                //FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                extractor.SetDownloadPath(folder.Path);
                System.Diagnostics.Debug.WriteLine(folder.Path);
            }
        }

        private async Task BadUrlWarning(string url, WarningType t)
        {
            switch (t)
            {
                case WarningType.InvalidUrl:
                    {
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = "Бака, это не ссылка!",
                            PrimaryButtonText = "Я бака"
                        };
                        Download.IsEnabled = false;
                        UrlBox.Text = string.Empty;
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.NotYTUrl:
                    {
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = "Бака, это не ссылка на ютуб!",
                            PrimaryButtonText = "Я бака"
                        };
                        Download.IsEnabled = false;
                        UrlBox.Text = string.Empty;
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.PlaylistNotFound:
                    {
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, я не нашел плейлиста по твоей ссылке!\n\r{url}",
                            PrimaryButtonText = "Я бака"
                        };
                        Download.IsEnabled = false;
                        UrlBox.Text = string.Empty;
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.VideoNotFound:
                    {
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, я не нашел видео по твоей ссылке!\n\r{url}",
                            PrimaryButtonText = "Я бака"
                        };
                        Download.IsEnabled = false;
                        UrlBox.Text = string.Empty;
                        await bakaMsg.ShowAsync();
                        return;
                    }
            }
        }

        private async Task DownloadPlaylist(string url)
        {
            PlaylistData p = extractor.GetPlaylistData(url);
            PlaylistFoundDialogue pfd = new PlaylistFoundDialogue(
                p.title,
                p.thumbnail,
                p.channelTitle,
                p.channelThumbnail
            )
            {
                Title = $"Обнаружен плейлист",
                PrimaryButtonText = "Извлечь все",
                SecondaryButtonText = "Отмена",
            };
            Download.IsEnabled = false;
            UrlBox.Text = string.Empty;
            var result = await pfd.ShowAsync();
            if (result == ContentDialogResult.Primary)
            { }
                // extract all
        }

        private async Task DownloadInPlaylist(string url)
        {
            PlaylistData pd = extractor.GetPlaylistData(url);
            PlaylistFoundDialogue vfip = new PlaylistFoundDialogue(
                pd.title,
                pd.thumbnail,
                pd.channelTitle,
                pd.channelThumbnail
            )
            {
                Title = $"Обнаружен плейлист",
                PrimaryButtonText = "Извлечь все",
                SecondaryButtonText = "Извлечь только из этого видео",
            };
            Download.IsEnabled = false;
            UrlBox.Text = string.Empty;
            var result = await vfip.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await DownloadPlaylist(url);
            else
                await DownloadOne(url);
        }

        private async Task DownloadOne(string url)
        {
            var video = extractor.GetVideoInfo(url);
            PlaylistFoundDialogue fv = new PlaylistFoundDialogue(
                video.title,
                video.thumbnail,
                video.channelTitle,
                video.channelThumbnail
            )
            {
                Title = $"Найдено видео",
                PrimaryButtonText = "Извлечь аудио",
                SecondaryButtonText = "Отмена",
            };
            Download.IsEnabled = false;
            UrlBox.Text = string.Empty;
            var res = await fv.ShowAsync();
            if (res == ContentDialogResult.Primary)
                await extractor.Extract(url);
        }
    }
}
