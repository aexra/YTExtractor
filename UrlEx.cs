using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using YoutubeExplode.Utils.Extensions;

namespace Utils
{
    internal static class UrlEx
    {
        public static string SubstringAfter(
        this string str,
        string sub,
        StringComparison comparison = StringComparison.Ordinal)
        {
            var index = str.IndexOf(sub, comparison);

            return index < 0
                ? string.Empty
                : str.Substring(index + sub.Length, str.Length - index - sub.Length);
        }

        public static string SubstringUntil(
        this string str,
        string sub,
        StringComparison comparison = StringComparison.Ordinal)
        {
            var index = str.IndexOf(sub, comparison);

            return index < 0
                ? str
                : str[..index];
        }

        public static async ValueTask CopyToAsync(
        this Stream source,
        Stream destination,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
        {
            using var buffer = MemoryPool<byte>.Shared.Rent(81920);

            var totalBytesRead = 0L;
            while (true)
            {
                var bytesRead = await source.ReadAsync(buffer.Memory.ToArray(), 0, buffer.Memory.Length);
                if (bytesRead <= 0)
                    break;

                await destination.WriteAsync(buffer.Memory.ToArray().GetSubAry(0, (uint)bytesRead), 0, bytesRead);

                totalBytesRead += bytesRead;
                progress?.Report(1.0 * totalBytesRead / source.Length);
            }
        }

        public static byte[] GetSubAry(this byte[] array, uint start, uint end)
        {
            byte[] new_ary = new byte[end - start + 1];
            for (uint i = start; i <= end; i++)
            {
                new_ary[i] = array[i];
            }
            return new_ary;
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumerateQueryParameters(string url)
        {
            var query = url.Contains('?')
                ? url.SubstringAfter("?")
                : url;

            foreach (var parameter in query.Split('&'))
            {
                var key = WebUtility.UrlDecode(parameter.SubstringUntil("="));
                var value = WebUtility.UrlDecode(parameter.SubstringAfter("="));

                if (string.IsNullOrWhiteSpace(key))
                    continue;

                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        public static IReadOnlyDictionary<string, string> GetQueryParameters(string url) =>
            EnumerateQueryParameters(url).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        private static KeyValuePair<string, string>? TryGetQueryParameter(string url, string key)
        {
            foreach (var parameter in EnumerateQueryParameters(url))
            {
                if (string.Equals(parameter.Key, key, StringComparison.Ordinal))
                    return parameter;
            }

            return null;
        }

        public static string? TryGetQueryParameterValue(string url, string key) =>
            TryGetQueryParameter(url, key)?.Value;

        public static bool ContainsQueryParameter(string url, string key) =>
            TryGetQueryParameterValue(url, key) is not null;

        public static string RemoveQueryParameter(string url, string key)
        {
            if (!ContainsQueryParameter(url, key))
                return url;

            var urlBuilder = new UriBuilder(url);
            var queryBuilder = new StringBuilder();

            foreach (var parameter in EnumerateQueryParameters(url))
            {
                if (string.Equals(parameter.Key, key, StringComparison.Ordinal))
                    continue;

                queryBuilder.Append(
                    queryBuilder.Length > 0
                        ? '&'
                        : '?'
                );

                queryBuilder.Append(WebUtility.UrlEncode(parameter.Key));
                queryBuilder.Append('=');
                queryBuilder.Append(WebUtility.UrlEncode(parameter.Value));
            }

            urlBuilder.Query = queryBuilder.ToString();

            return urlBuilder.ToString();
        }

        public static string SetQueryParameter(string url, string key, string value)
        {
            var urlWithoutParameter = RemoveQueryParameter(url, key);
            var hasOtherParameters = urlWithoutParameter.Contains('?');

            return
                urlWithoutParameter +
                (hasOtherParameters ? '&' : '?') +
                WebUtility.UrlEncode(key) +
                '=' +
                WebUtility.UrlEncode(value);
        }
    }
}