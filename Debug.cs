using Windows.Storage;
using System;
using System.Threading.Tasks;

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
        /// Примитивный логгер
        /// </summary>
        private static StorageFile file;

        /// <summary>
        /// Логает сообщение с префиксом [INFO  ]
        /// </summary>
        public static void Log(string msg, string prefix = "INFO")
        {
            string line = DateTime.Now.TimeOfDay.ToString() + $" [{prefix}\t] " + msg;
            System.Diagnostics.Debug.WriteLine(line);
            Task.Run(() => FileIO.AppendTextAsync(file, line + "\n"));
        }

        /// <summary>
        /// Логает предупреждение с префиксом [WARNING  ]
        /// </summary>
        public static void Warning(string msg)
        {
            string line = DateTime.Now.TimeOfDay.ToString() + " [WARNING\t] " + msg;
            System.Diagnostics.Debug.WriteLine(line);
            Task.Run(() => FileIO.AppendTextAsync(file, line + "\n"));
        }

        /// <summary>
        /// Логает ошибку с префиксом [ERROR    ]
        /// </summary>
        public static void Error(string msg)
        {
            string line = DateTime.Now.TimeOfDay.ToString() + " [ERROR\t] " + msg;
            System.Diagnostics.Debug.WriteLine(line);
            Task.Run(() => FileIO.AppendTextAsync(file, line + "\n"));
        }

        /// <summary>
        /// Генерирует поток для ввода логов
        /// </summary>
        public static void GenerateLogFile()
        {
            file = Task.Run(async () => await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting)).Result;
            Log("LastSessionLog.txt создан");
        }
    }
}
