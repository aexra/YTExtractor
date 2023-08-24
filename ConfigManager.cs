using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YTExtractor
{
    internal class IConfigManager
    {
        public static string ConfigPath;

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
