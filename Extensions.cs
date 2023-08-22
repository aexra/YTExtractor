using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace YTExtractor.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Заменяет запрещенные символы в пути на выбранный знак
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string ReplaceInvalidChars(this string filename, string replacer = "")
        {
            return string.Join(replacer, filename.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
