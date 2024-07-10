using Newtonsoft.Json;

namespace HttpServer_x64.Internals
{
    public abstract class ConfigIO<T> where T : Config, new()
    {
        /// <summary>
        /// The path to the configuration file
        /// </summary>
        public string FilePath { get; set; }
        public ConfigIO(string File)
        {
            this.FilePath = File;
        }
        public abstract bool Load(out T config);
        public abstract bool Save(in T config);
    }
    public class JsonConfigIO<T> : ConfigIO<T> where T : Config, new()
    {
        public JsonConfigIO(string File) : base(File)
        {
            this.Serializer = JsonSerializer.CreateDefault();
        }
        private JsonSerializer Serializer { get; set; }
        public string ConfigName { get; set; }
        /// <summary>
        /// Loads from file
        /// </summary>
        /// <param name="config">Config return object</param>
        /// <returns>True if file existed, false if file was created newly (file did not exist)</returns>
        public override bool Load(out T config)
        {
            T result;
            if (CheckFile())
            {
                using FileStream fs = File.OpenRead(this.FilePath);
                using TextReader tr = new StreamReader(fs);
                using JsonTextReader r = new JsonTextReader(tr);
                result = (T)(Serializer.Deserialize<T>(r) ?? new T().GetDefaultValues());
                config = result;
                return true;
            }
            else
            {
                result = new T();
                result.GetDefaultValues();
                config = result;

                // Let us create a new dummy file for this
                this.Save(config);

                return false;
            }
        }
        public override bool Save(in T config)
        {
            using FileStream fs = File.OpenWrite(this.FilePath);
            using TextWriter tw = new StreamWriter(fs);
            using JsonTextWriter jtw = new JsonTextWriter(tw);
            this.Serializer.Serialize(jtw, config);
            return true;
        }

        private bool CheckFile()
        {
            bool didFileExist = true;
            string directoryPath = Path.GetDirectoryName(this.FilePath);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (!File.Exists(this.FilePath))
                didFileExist = false;
            // File.CreateText(this.FilePath).Dispose();

            return didFileExist;
        }

    }
}
