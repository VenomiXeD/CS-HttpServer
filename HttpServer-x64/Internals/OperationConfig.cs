using Newtonsoft.Json;

namespace HttpServer_x64.Internals
{
    internal class OperationConfig : Config
    {
        [JsonProperty("Ports")]
        public int[] Ports { get; set; }
        [JsonProperty("www")]
        public string WebserverDirectory { get; set; }
        public override Config GetDefaultValues()
        {
            Ports = [8080];
            WebserverDirectory = Path.Combine(Path.GetFullPath("."), "www");
            return this;
        }
    }
}
