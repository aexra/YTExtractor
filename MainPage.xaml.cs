using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace YTExtractor
{
    public sealed partial class MainPage : Page
    {
        YTAudioExtractor extractor;
        enum WarningType {InvalidUrl, NotYTUrl, PlaylistNotFound, VideoNotFound, UnknownError};

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 225));
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            extractor = new YTAudioExtractor();

            extractor.GetVideoInfo("https://youtu.be/mas76qT3JVM");
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
            try
            {
                if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
                {
                    var text = await dataPackageView.GetTextAsync();
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        Download.IsEnabled = false;
                        urlBox.Text = string.Empty;
                        return;
                    }
                }
            } catch (Exception) { }
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

            try
            {
                // это нормальная ссылка?
                if (!extractor.IsUrl(url))
                    await WarningDialog(url, WarningType.InvalidUrl);

                // это (почти) ссылка на ютуб?
                else if (!url.Contains("youtu"))
                    await WarningDialog(url, WarningType.NotYTUrl);

                // это ссылка на плейлист?
                else if (url.Contains("playlist"))
                    await DownloadPlaylist(url);

                // это видео состоит в плейлисте?
                else if (url.Contains("&list="))
                    await DownloadInPlaylist(url);

                // а иначе это просто ссылка на видео
                else await DownloadOne(url);
            }
            catch (Exception)
            {
                await WarningDialog(url, WarningType.UnknownError);
            }
        }

        private async void OnSelectFolderPressed(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                System.Diagnostics.Debug.WriteLine("OLD FOLDER:  " + extractor.downloadPath);
                System.Diagnostics.Debug.WriteLine("NEW FOLDER:  " + folder.Path);
                extractor.SetDownloadPath(folder.Path);
            }
        }

        private async Task WarningDialog(string url, WarningType t)
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
                case WarningType.UnknownError:
                    {
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, ТЫ вызвал доселе неизвестную ошибку! Возможно плейлист или видео запривачен(о), подумай над своим поведением!\n\nТвоя ссылка:\n{url}",
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
            PlaylistData pd = extractor.GetPlaylistData(url);
            PlaylistFoundDialogue pfd = new PlaylistFoundDialogue(
                pd.title,
                pd.thumbnail,
                pd.channelTitle,
                pd.channelThumbnail
            )
            {
                Title = $"Обнаружен плейлист - {pd.n} видео",
                PrimaryButtonText = "Извлечь все",
                SecondaryButtonText = "Отмена",
            };
            Download.IsEnabled = false;
            UrlBox.Text = string.Empty;
            var result = await pfd.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                foreach (string id in pd.ids)
                {
                    await extractor.Extract(id);
                }
            }
        }

        private async Task DownloadInPlaylist(string url)
        {
            PlaylistData pd = extractor.GetPlaylistData(url);
            if (pd == null)
            {
                await DownloadOne(url);
                return;
            }
            PlaylistFoundDialogue vfip = new PlaylistFoundDialogue(
                pd.title,
                pd.thumbnail,
                pd.channelTitle,
                pd.channelThumbnail
            )
            {
                Title = $"Обнаружен плейлист - {pd.n} видео",
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
