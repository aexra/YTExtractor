using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using sy = Syroot.Windows.IO;
using Newtonsoft.Json;

namespace YTExtractor
{
    internal class IConfigManager
    {
        private static StorageFolder ConfigFolder = ApplicationData.Current.LocalFolder;
        private static Dictionary<string, object> DefaultDict = new Dictionary<string, object>
        {
            {"downloadPath", sy.KnownFolders.Downloads.Path}
        };
        public static Dictionary<string, object> Config;

        public static Dictionary<string, object> LoadConf()
        {
            if (!File.Exists(ConfigFolder.Path + "\\" + "config.txt"))
            {
                StorageFile file = Task.Run(async () => await ConfigFolder.CreateFileAsync("config.txt", CreationCollisionOption.ReplaceExisting)).Result;
                Task.Run(async () => await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(DefaultDict)));
                Debug.Log("config.txt создан");
                return Config = DefaultDict;
            }
            else
            {
                StorageFile file = Task.Run(async () => await ConfigFolder.GetFileAsync("config.txt")).Result;
                Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(Task.Run(async () => await FileIO.ReadTextAsync(file)).Result);
                Debug.Log("Конфиг загружен");
                return Config = dict;
            }
        }
        
        public static void SaveConf()
        {
            StorageFile file = Task.Run(async () => await ConfigFolder.GetFileAsync("config.txt")).Result;
            Task.Run(async () => await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(Config)));
            Debug.Log("Конфиг сохранен");
        }
    }
}
