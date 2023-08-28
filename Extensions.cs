using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace YTExtractor.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Заменяет запрещенные символы в пути на выбранный знак
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="replacer"></param>
        /// <returns></returns>
        public static string ReplaceInvalidChars(this string filename, string replacer = "")
        {
            var str = string.Join(replacer, filename.Split(Path.GetInvalidFileNameChars()));
            if (str[^1] == '.')
                str = str[..^1];
            return str;
        }
    }

    public static class StreamExtensions
    {
        public static async Task CopyToAsync(
            this Stream source, 
            Stream destination, 
            IProgress<int> percentProgress,
            IProgress<long> dataProgress = null,
            CancellationToken cancellationToken = default(CancellationToken), 
            int bufferSize = 0x1000)
        {
            var buffer = new byte[bufferSize];
            int bytesRead;
            long totalRead = 0;
            long fileSize = source.Length;
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                totalRead += bytesRead;
                //Thread.Sleep(10);
                percentProgress.Report((int) (totalRead / (double)fileSize * 100));
                dataProgress?.Report(totalRead);
            }
        }
    }

    public sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;
        public SynchronousProgress(Action<T> callback) { _callback = callback; }
        void IProgress<T>.Report(T data) => _callback(data);
    }
}
