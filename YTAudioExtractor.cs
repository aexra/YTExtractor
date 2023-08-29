using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.IO;
using System.Linq;
using System.Web;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Specialized;
using Windows.Storage;
using YTExtractor.Extensions;
using YTExtractor.Data;
using AngleSharp.Dom;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace YTExtractor
{
    enum WarningType { InvalidUrl, NotYTUrl, PlaylistNotFound, VideoNotFound, FolderAccessDenied, FileCreateAccessDenied, UnknownError };

    /// <summary>
    /// Обобщенный класс, выполняющий всю работу по загрузке аудиофайлов
    /// </summary>
    internal class YTAudioExtractor
    {
        /// <summary>
        /// Объект для работы с запросами через Google API
        /// </summary>
        private YouTubeService youtubeService;

        /// <summary>
        /// Объект для работы с библиотекой YoutubeExplode
        /// </summary>
        private YoutubeClient youtubeClient;

        /// <summary>
        /// Директория сохранения загруженных файлов
        /// </summary>
        public string downloadPath;

        /// <summary>
        /// Конструктор класса YTAudioExtractor
        /// </summary>
        public YTAudioExtractor()
        {
            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyBUS_UGZMp2Hjr6k4Pbn3Azw93hkdOBKwE",
                ApplicationName = this.GetType().ToString()
            });
            youtubeClient = new YoutubeClient();
            Debug.GenerateLogFile();
            ConfigManager.LoadConf();
            UpdateValues();
            GetVideoInfo("https://youtu.be/GG7Yb0tg0rw?si=WH11zdXpCjo4Jmkv");
        }

        /// <summary>
        /// Проверяет, является ли введенная строка действующей ссылкой
        /// </summary>
        /// <param name="request">Строка для проверки</param>
        /// <returns>Возвращает результат проверки</returns>
        public bool IsUrl(string request)
        {
            if (Uri.IsWellFormedUriString(request, UriKind.Absolute))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Извлекает ID видео Youtube по переданной ссылке
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Возвращает ID видео, если ссылка действительна и видео доступно</returns>
        public string ParseVideoId(string url)
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);

            string videoId;

            if (query.AllKeys.Contains("v"))
            {
                videoId = query["v"];
            }
            else
            {
                videoId = uri.Segments.Last();
            }

            return videoId;
        }

        /// <summary>
        /// Возвращает информацию о видео по переданной ссылке/id
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        public VideoData GetVideoInfo(string videoId)
        {
            if (IsUrl(videoId)) { videoId = ParseVideoId(videoId); }
            VideosResource.ListRequest listRequest = youtubeService.Videos.List("snippet");
            listRequest.Id = videoId;
            VideoListResponse response = listRequest.Execute();
            var r = response.Items.First().Snippet;
            VideoData vd = new VideoData();
            vd.Id = videoId;
            vd.Title = r.Title;
            vd.Thumbnail = r.Thumbnails.Maxres.Url;
            vd.ChannelId = r.ChannelId;
            vd.ChannelTitle = r.ChannelTitle;
            ChannelsResource.ListRequest request = youtubeService.Channels.List("snippet");
            request.Id = r.ChannelId;
            vd.ChannelThumbnail = request.Execute().Items.First().Snippet.Thumbnails.High.Url;
            return vd;
        }

        /// <summary>
        /// Возвращает ссылку на аудиопоток из видео, если видео по ссылке доступно
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        public async Task<string> GetAudioUrlAsync(string videoId)
        {
            if (IsUrl(videoId)) { videoId = ParseVideoId(videoId); }
            var audioStreams = await youtubeClient.Videos.Streams.GetManifestAsync(videoId).ConfigureAwait(false);
            var audioStreamInfo = audioStreams.GetAudioOnlyStreams().GetWithHighestBitrate();
            return audioStreamInfo.Url;
        }

        /// <summary>
        /// Возвращает экземляр потока аудио из видео по переданной ссылке/id
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        public async Task<IStreamInfo> GetAudioStreamInfoAsync(string videoId)
        {
            if (IsUrl(videoId)) { videoId = ParseVideoId(videoId); }
            var audioStreams = await youtubeClient.Videos.Streams.GetManifestAsync(videoId).ConfigureAwait(false);
            var audioStreamInfo = audioStreams.GetAudioOnlyStreams().GetWithHighestBitrate();
            return audioStreamInfo;
        }

        /// <summary>
        /// Вовзаращает прямой поток аудио по переданной ссылке/id
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        public async Task<Stream> GetAudioStreamAsync(string videoId)
        {
            if (IsUrl(videoId)) { videoId = ParseVideoId(videoId); }
            var audioStreams = await youtubeClient.Videos.Streams.GetManifestAsync(videoId).ConfigureAwait(false);
            var audioStreamInfo = audioStreams.GetAudioOnlyStreams().GetWithHighestBitrate();
            var stream = await youtubeClient.Videos.Streams.GetAsync(audioStreamInfo);
            return stream;
        }

        /// <summary>
        /// Скачивает аудио в формате mp3 из видео по переданной ссылке/id
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        public async Task Extract(string videoId, IProgress<int> percentProgress = null,  IProgress<long> dataProgress = null)
        {
            VideoData info = GetVideoInfo(videoId);

            string fileName = info.Title.ReplaceInvalidChars();

            StorageFile outputFile;
            try
            { outputFile = await MakeOutputFile(fileName); }
            catch
            { await WarningDialog(WarningType.FileCreateAccessDenied); return; }

            Stream outputStream = await GetOutputStream(outputFile);
            Stream audioStream = await GetAudioStreamAsync(videoId);
            
            if (percentProgress == null) await audioStream.CopyToAsync(outputStream);
            else await audioStream.CopyToAsync(outputStream, percentProgress, dataProgress); 

            outputStream.Dispose();

            await outputFile.RenameAsync(fileName + ".mp3", NameCollisionOption.GenerateUniqueName);
        }

        /// <summary>
        /// Создает новый файл в директории загрузки downloadPath для записи из потока аудио
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public async Task<StorageFile> MakeOutputFile(string title)
        {
            //if (downloadPath == Syroot.Windows.IO.KnownFolders.Downloads.Path)
            //    return await DownloadsFolder.CreateFileAsync(title, CreationCollisionOption.GenerateUniqueName);
            return await (await StorageFolder.GetFolderFromPathAsync(downloadPath)).CreateFileAsync(title, CreationCollisionOption.GenerateUniqueName);
        }

        /// <summary>
        /// Открывает выбранный файл для записи и возвращает поток
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public async Task<Stream> GetOutputStream(StorageFile dest)
        {
            return (await dest.OpenAsync(FileAccessMode.ReadWrite)).AsStream();
        }

        /// <summary>
        /// Возвращает информацию о плейлисте по введенной ссылке/id
        /// </summary>
        /// <returns></returns>
        public PlaylistData GetPlaylistData(string playlistId)
        {
            string id;
            if (IsUrl(playlistId))
            {
                NameValueCollection parsed = System.Web.HttpUtility.ParseQueryString(new Uri(playlistId).Query);
                id = parsed["list"].ToString();
            }
            else
            {
                id = playlistId;
            }

            var playlistData = new PlaylistData();

            if (id == "LL") return null;

            playlistData.PlaylistId = id;

            { 
                PlaylistsResource.ListRequest request = youtubeService.Playlists.List("snippet");
                request.Id = id;

                var response = request.Execute().Items.First().Snippet;

                playlistData.Title = response.Title;
                playlistData.Thumbnail = response.Thumbnails.High.Url;
                playlistData.Description = response.Description;
                playlistData.ChannelId = response.ChannelId;
                playlistData.ChannelTitle = response.ChannelTitle;
            }
            {
                ChannelsResource.ListRequest request = youtubeService.Channels.List("snippet");
                request.Id = playlistData.ChannelId;

                playlistData.ChannelThumbnail = request.Execute().Items.First().Snippet.Thumbnails.High.Url;
            }
            {
                PlaylistItemsResource.ListRequest request = youtubeService.PlaylistItems.List("snippet");
                request.PlaylistId = id;
                var response = request.Execute();
                playlistData.Count = (int)response.PageInfo.TotalResults;
            }
            return playlistData;
        }

        /// <summary>
        /// Устаналивает выбранную директорию в качестве папки для сохранения файлов.
        /// </summary>
        /// <param name="path"></param>
        public void SetDownloadPath(string path)
        {
            downloadPath = path;
        }

        /// <summary>
        /// Возвращает список id видео, входящих в плейлист
        /// </summary>
        /// <param name="playlistId"></param>
        /// <returns></returns>
        public List<string> GetPlaylistIds(string playlistId)
        {
            List<string> ids = new List<string>();

            PlaylistItemsResource.ListRequest request = youtubeService.PlaylistItems.List("snippet");
            request.PlaylistId = playlistId;
            request.MaxResults = 50;
            PlaylistItemListResponse response;
            do
            {
                response = request.Execute();

                foreach (var video in response.Items)
                    ids.Add(video.Snippet.ResourceId.VideoId);

                request.PageToken = response.NextPageToken;
            }
            while (response.NextPageToken is not null);

            return ids;
        }

        /// <summary>
        /// Обновляет значения настроек, полученных из конфига
        /// </summary>
        public void UpdateValues()
        {
            downloadPath = (string)ConfigManager.Config["downloadPath"];
        }

        /// <summary>
        /// Вызывает диалоговое окно с сообщением об ошибке
        /// </summary>
        /// <param name="t"></param>
        /// <param name="e"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public async Task WarningDialog(WarningType t, Exception e = null, string url = null)
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
                case WarningType.FolderAccessDenied:
                    {
                        Debug.Error($"Отказано в доступе при открытии папки загрузок по адресу: [{downloadPath}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, я не могу открыть твою папку! Тупая винда запрещает мне её трогать, так что выбери другую и потом открывай",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
                case WarningType.FileCreateAccessDenied:
                    {
                        Debug.Error($"Отказано в доступе при создании файла в папке: [{downloadPath}]");
                        ContentDialog bakaMsg = new ContentDialog()
                        {
                            Content = $"Бака, я не могу загружать в эту папку! Создай где-нибудь новую папку и выбери её, или попробуй выбрать другую папку",
                            PrimaryButtonText = "Я бака"
                        };
                        await bakaMsg.ShowAsync();
                        return;
                    }
            }
        }

        /// <summary>
        /// Возвращает размер файла аудио формата видео по ссылке/id
        /// </summary>
        /// <param name="videoId"></param>
        /// <returns></returns>
        public async Task<long> GetAudioSizeAsync(string videoId)
        {
            if (IsUrl(videoId)) { videoId = ParseVideoId(videoId); }
            return (await GetAudioStreamAsync(videoId)).Length;
        }
    }
}
