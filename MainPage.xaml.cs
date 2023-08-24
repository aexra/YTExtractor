using Google.Apis.YouTube.v3.Data;
using Google.Apis.YouTube.v3;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using YTExtractor;
using YTExtractor.Data;

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

            // warmup extractor
            extractor.GetVideoInfo("https://youtu.be/GG7Yb0tg0rw?si=WH11zdXpCjo4Jmkv");
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
            Debug.Log("Вставлена строка");
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
            Debug.Log("Нажата кнопка загрузить");
            await InitiateDownloadSequence();
        }

        private async Task InitiateDownloadSequence()
        {
            string url = UrlBox.Text;
            Debug.Log($"Запущена последовательность загрузки для запроса: [{url}]");
            ClearUrlBox();
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
            catch (Exception e)
            {
                await WarningDialog(url, WarningType.UnknownError, e);
            }
        }

        private async void OnSelectFolderPressed(object sender, RoutedEventArgs e)
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
            }
        }

        private async Task WarningDialog(string url, WarningType t, Exception e = null)
        {
            switch (t)
            {
                case WarningType.InvalidUrl:
                    {
                        Debug.Warning($"Недействительная ссылка: [{url}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = "Бака, это не ссылка!",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.NotYTUrl:
                    {
                        Debug.Warning($"Неютубная ссылка: [{url}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = "Бака, это не ссылка на ютуб!",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.PlaylistNotFound:
                    {
                        Debug.Warning($"Плейлист не найден: [{url}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, я не нашел плейлиста по твоей ссылке!\n\r{url}",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.VideoNotFound:
                    {
                        Debug.Warning($"Видео не найдено: [{url}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, я не нашел видео по твоей ссылке!\n\r{url}",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.UnknownError:
                    {
                        Debug.Error($"Вызвана неизвестная ошибка: [{url}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, ТЫ вызвал доселе неизвестную ошибку!\nВозможно плейлист или видео запривачен(о)\nТы виноват, подумай над своим поведением!\n\nТвоя ссылка:\n{url}\n\n\n{e}",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
            }
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
            var result = await pfd.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var ids = extractor.GetPlaylistIds(pd.PlaylistId);
                foreach (string id in ids)
                {
                    await extractor.Extract(id);
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
                var result = await vfip.ShowAsync();
                if (result == ContentDialogResult.Primary)
                    foreach (string id in pd.Ids)
                        await extractor.Extract(id);
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
            Download.IsEnabled = false;
            UrlBox.Text = string.Empty;
            var res = await fv.ShowAsync();
            if (res == ContentDialogResult.Primary)
                await extractor.Extract(url);
        }

        private void ClearUrlBox()
        {
            UrlBox.Text = string.Empty;
        }
    }
}
