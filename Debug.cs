using Windows.Storage;
using System;
using System.Threading.Tasks;
using System.IO;

namespace YTExtractor
{
    static class Debug
    {
        /// <summary>
        /// Имя файла для сохранения лога с последней сессии приложения
        /// </summary>
        private static readonly string fileName = "LastSessionLog.txt";

        /// <summary>
        /// Корневая папка приложения
        /// </summary>
        private static readonly StorageFolder folder = ApplicationData.Current.LocalFolder;

        /// <summary>
        /// Текстовый файл
        /// </summary>
        private static StorageFile file;

        /// <summary>
        /// Логгер типа
        /// </summary>
        private static StreamWriter sw;

        /// <summary>
        /// Логает сообщение с префиксом [INFO  ]
        /// </summary>
        public static void Log(string msg, string prefix = "INFO")
        {
            string line = DateTime.Now.TimeOfDay.ToString() + $" [{prefix}\t] " + msg;
            System.Diagnostics.Debug.WriteLine(line);
            sw.WriteLine(line);
            sw.Flush();
        }

        /// <summary>
        /// Логает предупреждение с префиксом [WARNING  ]
        /// </summary>
        public static void Warning(string msg)
        {
            string line = DateTime.Now.TimeOfDay.ToString() + " [WARN\t] " + msg;
            System.Diagnostics.Debug.WriteLine(line);
            sw.WriteLine(line);
            sw.Flush();
        }

        /// <summary>
        /// Логает ошибку с префиксом [ERROR    ]
        /// </summary>
        public static void Error(string msg)
        {
            string line = DateTime.Now.TimeOfDay.ToString() + " [ERROR\t] " + msg;
            System.Diagnostics.Debug.WriteLine(line);
            sw.WriteLine(line);
            sw.Flush();
        }

        /// <summary>
        /// Генерирует поток для ввода логов
        /// </summary>
        public static void GenerateLogFile()
        {
            file = Task.Run(async () => await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting)).Result;
            sw = new StreamWriter(Task.Run(async () => await file.OpenStreamForWriteAsync()).Result);
            Log("LastSessionLog.txt создан");
        }
    }
}
