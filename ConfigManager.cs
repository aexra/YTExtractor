using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace YTExtractor
{
    internal class IConfigManager
    {
        public static StorageFolder ConfigFolder = ApplicationData.Current.LocalFolder;

        public static Dictionary<string, object> LoadConf()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();

            return dict;
        }

        public static void SaveConf()
        {

        }
    }
}
