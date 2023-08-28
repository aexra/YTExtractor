using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using sy = Syroot.Windows.IO;
using Newtonsoft.Json;

namespace YTExtractor
{
    internal class ConfigManager
    {
        private static StorageFolder ConfigFolder = ApplicationData.Current.LocalFolder;
        private static Dictionary<string, object> DefaultDict = new Dictionary<string, object>
        {
            {"downloadPath", sy.KnownFolders.Downloads.Path}
        };
        public static Dictionary<string, object> Config;

        public static Dictionary<string, object> LoadConf()
        {
            if (CreateConfigIfNotFound()) return Config;
            StorageFile file = Task.Run(async () => await ConfigFolder.GetFileAsync("config.txt")).Result;
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(Task.Run(async () => await FileIO.ReadTextAsync(file)).Result);
            Debug.Log("Конфиг загружен");
            return Config = dict;
        }
        
        public static void SaveConf()
        {
            if (CreateConfigIfNotFound()) return;
            StorageFile file = Task.Run(async () => await ConfigFolder.GetFileAsync("config.txt")).Result;
            Task.Run(async () => await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(Config)));
            Debug.Log("Конфиг сохранен");
        }

        public static void ResetConf()
        {
            if (CreateConfigIfNotFound()) return;
            StorageFile file = Task.Run(async () => await ConfigFolder.GetFileAsync("config.txt")).Result;
            Config = new Dictionary<string, object>(DefaultDict);
            Task.Run(async () => await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(Config)));
            Debug.Log("Конфиг сброшен");
        }

        private static bool CreateConfigIfNotFound()
        {
            if (!File.Exists(ConfigFolder.Path + "\\" + "config.txt"))
            {
                StorageFile file = Task.Run(async () => await ConfigFolder.CreateFileAsync("config.txt", CreationCollisionOption.ReplaceExisting)).Result;
                Task.Run(async () => await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(DefaultDict)));
                Debug.Log("config.txt создан");
                Config = new Dictionary<string, object>(DefaultDict);
                return true;
            }
            return false;
        }
    }
}
