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
using System.Diagnostics;

namespace YTExtractor
{
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
        public string downloadPath = Syroot.Windows.IO.KnownFolders.Downloads.Path;

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
            //return await youtubeClient.Videos.GetAsync(videoId);
            VideosResource.ListRequest listRequest = youtubeService.Videos.List("snippet");
            listRequest.Id = videoId;
            VideoListResponse response = listRequest.Execute();
            var r = response.Items.First().Snippet;
            VideoData vd = new VideoData();
            vd.id = videoId;
            vd.title = r.Title;
            vd.thumbnail = r.Thumbnails.High.Url;
            vd.channelId = r.ChannelId;
            vd.channelTitle = r.ChannelTitle;
            ChannelsResource.ListRequest request = youtubeService.Channels.List("snippet");
            request.Id = r.ChannelId;
            vd.channelThumbnail = request.Execute().Items.First().Snippet.Thumbnails.High.Url;
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
        public async Task Extract(string videoId, IProgress<int> progress = null)
        {
            VideoData info = GetVideoInfo(videoId);

            string fileName = info.title.ReplaceInvalidChars();
            StorageFile outputFile = await MakeOutputFile(fileName);

            Stream outputStream = await GetOutputStream(outputFile);
            Stream audioStream = await GetAudioStreamAsync(videoId);

            await audioStream.CopyToAsync(outputStream);

            await outputFile.RenameAsync(fileName + ".mp3", NameCollisionOption.GenerateUniqueName);
        }

        /// <summary>
        /// Создает новый файл в директории загрузки downloadPath для записи из потока аудио
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public async Task<StorageFile> MakeOutputFile(string title)
        {
            StorageFile destination = await (await StorageFolder.GetFolderFromPathAsync(downloadPath)).CreateFileAsync(title, CreationCollisionOption.GenerateUniqueName);
            return destination;
        }

        /// <summary>
        /// Открывает выбранный файл для записи и возвращает поток
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public async Task<Stream> GetOutputStream(StorageFile dest)
        {
            Stream dstream = await dest.OpenStreamForWriteAsync();
            return dstream;
        }

        /// <summary>
        /// Возвращает информацию о плейлисте по введенной ссылке
        /// </summary>
        /// <returns></returns>
        public PlaylistData GetPlaylistData(string url)
        {
            var playlistData = new PlaylistData();

            NameValueCollection parsed = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
            string id = parsed["list"].ToString();

            if (id == "LL") return null;

            { 
                PlaylistsResource.ListRequest request = youtubeService.Playlists.List("snippet");
                request.Id = id;

                var response = request.Execute().Items.First().Snippet;

                playlistData.title = response.Title;
                playlistData.thumbnail = response.Thumbnails.High.Url;
                playlistData.description = response.Description;
                playlistData.channelId = response.ChannelId;
                playlistData.channelTitle = response.ChannelTitle;
            }
            {
                ChannelsResource.ListRequest request = youtubeService.Channels.List("snippet");
                request.Id = playlistData.channelId;

                playlistData.channelThumbnail = request.Execute().Items.First().Snippet.Thumbnails.High.Url;
            }
            {
                PlaylistItemsResource.ListRequest request = youtubeService.PlaylistItems.List("snippet");
                request.PlaylistId = id;
                request.MaxResults = 50;
                PlaylistItemListResponse response;
                do
                {
                    response = request.Execute();

                    foreach (var video in response.Items)
                        playlistData.ids.Add(video.Id);

                    request.PageToken = response.NextPageToken;
                }
                while (response.NextPageToken is not null);

                playlistData.n = (int)response.PageInfo.TotalResults;
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
    }
}
