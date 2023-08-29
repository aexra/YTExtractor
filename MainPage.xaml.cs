using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using YTExtractor;
using YTExtractor.Data;
using Windows.Storage;
using Windows.System;
using AngleSharp.Dom;
using YTExtractor.Extensions;
using System.Linq;
using YoutubeExplode.Videos;

namespace YTExtractor
{
    public sealed partial class MainPage : Page
    {
        /**
         *           PRIVATE FIELDS
         */

        private YTAudioExtractor extractor;

        /**
         *           CONSTRUCTOR
         */

        public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 225));
            ApplicationView.PreferredLaunchViewSize = new Size(480, 800);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            extractor = new YTAudioExtractor();
        }

        /**
         *           PRIVATE MATHODS
         */

        private async Task InitiateDownloadSequence()
        {
            string url = UrlBox.Text;
            Debug.Log($"Запущена последовательность загрузки для запроса: [{url}]");
            ClearUrlBox();
            try
            {
                // это нормальная ссылка?
                if (!extractor.IsUrl(url))
                    await extractor.WarningDialog(WarningType.InvalidUrl, null, url);

                // это (почти) ссылка на ютуб?
                else if (!url.Contains("youtu"))
                    await extractor.WarningDialog(WarningType.NotYTUrl, null, url);

                // это ссылка на плейлист?
                else if (url.Contains("playlist"))
                    await DownloadPlaylist(url);

                // это видео состоит в плейлисте?
                else if (url.Contains("&list="))
                    await DownloadInPlaylist(url);

                // а иначе это просто ссылка на видео
                else await DownloadOne(url);
            }
            catch (Exception e)
            {
                await extractor.WarningDialog(WarningType.UnknownError, e, url);
            }
        }
        private async Task ExtractAudio(string url, VideoData video = null)
        {
            video ??= extractor.GetVideoInfo(url);
            HistoryVideoPage hvp = new HistoryVideoPage(video.Title, video.Thumbnail, video.ChannelTitle, video.ChannelThumbnail);
            HistoryBox.Children.Insert(0, hvp);
            var size = await extractor.GetAudioSizeAsync(url);
            string sizeStr = Math.Round((double)size / 1048576, 1).ToString();
            hvp.SetProgressText($"0/{sizeStr} MB");
            IProgress<int> progress = new SynchronousProgress<int>(value =>
            {
                hvp.SetProgress(value);
            });
            IProgress<long> dataProgress = new SynchronousProgress<long>(value =>
            {
                hvp.SetProgressText($"{Math.Round((double)value / 1048576, 1)}/{sizeStr} MB");
            });
            await extractor.Extract(url, progress, dataProgress);
        }

        private async Task DownloadPlaylist(string url)
        {
            Debug.Log($"Запущена последовательность загрузки плейлиста: [{url}]");
            PlaylistData pd = extractor.GetPlaylistData(url);
            PlaylistFoundDialogue pfd = new PlaylistFoundDialogue(
                pd.Title,
                pd.Thumbnail,
                pd.ChannelTitle,
                pd.ChannelThumbnail
            )
            {
                Title = $"Обнаружен плейлист - {pd.Count} видео",
                PrimaryButtonText = "Извлечь все",
                SecondaryButtonText = "Отмена",
            };
            var response = await pfd.ShowAsync();
            if (response == ContentDialogResult.Primary)
            {
                var ids = extractor.GetPlaylistIds(pd.PlaylistId);
                foreach (string id in ids)
                {
                    await ExtractAudio($"https://www.youtube.com/watch?v={id}");
                }
            }
        }
        private async Task DownloadInPlaylist(string url)
        {
            Debug.Log($"Запущена последовательность загрузки плейлиста по видео: [{url}]");
            try
            {
                PlaylistData pd = extractor.GetPlaylistData(url);

                if (pd == null)
                {
                    await DownloadOne(url);
                    return;
                }
                PlaylistFoundDialogue vfip = new PlaylistFoundDialogue(
                    pd.Title,
                    pd.Thumbnail,
                    pd.ChannelTitle,
                    pd.ChannelThumbnail
                )
                {
                    Title = $"Обнаружен плейлист - {pd.Count} видео",
                    PrimaryButtonText = "Извлечь все",
                    SecondaryButtonText = "Извлечь только из этого видео",
                };
                var response = await vfip.ShowAsync();
                if (response == ContentDialogResult.Primary)
                    foreach (string id in pd.Ids)
                        await ExtractAudio($"https://www.youtube.com/watch?v={id}");
                else
                    await DownloadOne(url);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Плейлист недоступен - форсирую загрузку конкретного видео");
                await DownloadOne(url);
            }
        }
        private async Task DownloadOne(string url)
        {
            Debug.Log($"Запущена последовательность загрузки видео: [{url}]");
            var video = extractor.GetVideoInfo(url);
            PlaylistFoundDialogue fv = new PlaylistFoundDialogue(
                video.Title,
                video.Thumbnail,
                video.ChannelTitle,
                video.ChannelThumbnail
            )
            {
                Title = $"Найдено видео",
                PrimaryButtonText = "Извлечь аудио",
                SecondaryButtonText = "Отмена",
            };
            var response = await fv.ShowAsync();
            if (response == ContentDialogResult.Primary)
            {
                await ExtractAudio(url, video);
            }
        }

        private void ClearUrlBox()
        {
            UrlBox.Text = string.Empty;
        }

        /**
         *           EVENTS
         */

        private void OnUrlChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlBox.Text))
            {
                if (UrlBox.Text != string.Empty) ClearUrlBox();
                Download.IsEnabled = false;
                return;
            }
            Download.IsEnabled = true;
        }
        private async void OnUrlPasted(object sender, TextControlPasteEventArgs e)
        {
            Debug.Log("Вставлено из буфера обмена");
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
                    {
                        var text = await dataPackageView.GetTextAsync();
                        if (string.IsNullOrWhiteSpace(text))
                        {
                            //Windows.ApplicationModel.DataTransfer.Clipboard.Clear();
                            return;
                        }
                        if (extractor.IsUrl(text))
                        {
                            await InitiateDownloadSequence();
                            return;
                        }
                    }
                }
                catch (Exception) { }
                System.Threading.Thread.Sleep(10);
            }
        }
        private void OnUrlKeyDown(object sender, KeyRoutedEventArgs e)
        {

        }
        private async void OnDownloadClicked(object sender, RoutedEventArgs e)
        {
            Debug.Log("Нажата кнопка загрузить");
            await InitiateDownloadSequence();
        }
        private async void OnSelectFolderClicked(object sender, RoutedEventArgs e)
        {
            Debug.Log("Выбор пути сохранения");

            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                Debug.Log("OLD FOLDER:  " + extractor.downloadPath);
                Debug.Log("NEW FOLDER:  " + folder.Path);
                extractor.SetDownloadPath(folder.Path);
                ConfigManager.Config["downloadPath"] = folder.Path;
                ConfigManager.SaveConf();
            }
        }
        private async void OnOpenDownloadsFolderClicked(object sender, RoutedEventArgs e)
        {
            Debug.Log("Открыта папка загрузок");
            try
            { await Launcher.LaunchFolderAsync(await StorageFolder.GetFolderFromPathAsync(extractor.downloadPath)); }
            catch (Exception)
            { await extractor.WarningDialog(WarningType.FolderAccessDenied); }
        }
        private async void OnOpenRootFolderClicked(object sender, RoutedEventArgs e)
        {
            Debug.Log("Открыта корневая папка");
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }
        private void OnResetConfigClicked(object sender, RoutedEventArgs e)
        {
            ConfigManager.ResetConf();
            extractor.UpdateValues();
        }
    }
}
