using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace HttpServer_x64.Internals
{
    public class ConfigIO
    {
        /// <summary>
        /// 
        /// </summary>
        private static string ExecutingDirectory { get => Path.GetFullPath("."); }
        public const string BasePath = "config";
        /// <summary>
        /// Creates a new config object
        /// </summary>
        /// <param name="Name">Name of the config file</param>
        /// <param name="defaultValue">The default value to use if no config exists</param>
        public ConfigIO(string Name, object defaultValue) { this.ConfigName = Name; }
        public string ConfigName { get; set; }

        public object Load()
        {
            return null;
        }

        private void CheckFile()
        {
            if (!Directory.Exists(ConfigIO.BasePath))
                Directory.CreateDirectory(ConfigIO.BasePath);

            if (!File.Exists(this.FilePath()))
                File.CreateText(this.FilePath()).Dispose();
        }
        private string FilePath()
        {
            return Path.Combine(ConfigIO.BasePath, this.ConfigName);
        }
    }
}
