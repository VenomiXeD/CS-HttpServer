using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;

namespace HttpServer_x64
{
    public class HttpServer
    {
        // == HTTP related == //
        /// <summary>
        /// The HTTP listener instance, listens for requests
        /// </summary>
        public HttpListener Listener { get; set; }

        public string RootWebDirectory { get; set; }

        public Dictionary<string, string> MimeTypesFileAssociations { get; set; } = new Dictionary<string, string>();
        // == Utility related == //
        private readonly Logger Log;
        public static string ExecutingPath { get => Path.GetFullPath("."); }

        static void Main(string[] args)
        {
            HttpServer p = new HttpServer("D:\\Projects\\CS\\web\\www",8080);
            p.Start().ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.ReadKey();
        }

        public HttpServer(string rootWebDirectory, int port)
        {
            // == Utility related == //
            // Create Logs directory
            Directory.CreateDirectory(Path.Combine(HttpServer.ExecutingPath, "logs"));
            // Start a log instance
            this.Log = new Logger(Path.Combine(HttpServer.ExecutingPath, "logs", "log"));
            this.RootWebDirectory = rootWebDirectory;

            // == HTTP related == //
            // Creates a new listener instance
            this.Listener = new HttpListener();
            // Configures the listener
            this.Listener.Prefixes.Add($"http://*:{port}/");

            this.RootWebDirectory = rootWebDirectory;

            // Load in mimetypes
            // We need to determine the MIME-type and set it to the output
            Log.Info("Reading MIMETYPES and Deserializing");
#pragma warning disable
            using (Stream mimeTypesStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(x => x.EndsWith("mimetypes.json"))))
            {
                using (TextReader reader = new StreamReader(mimeTypesStream))
                {
                    using (JsonTextReader r = new JsonTextReader(reader))
                    {
                        MimeTypesFileAssociations = JsonSerializer.CreateDefault().Deserialize<Dictionary<string, string>>(r);
                        Log.Info("Mimetypes loaded: {0}", MimeTypesFileAssociations.Count);
                        foreach(KeyValuePair<string,string> file_mimetype in  MimeTypesFileAssociations)
                        {
                            Log.Info($"{file_mimetype.Key} - {file_mimetype.Value}");
                        }
                    }
                }
            }
#pragma warning restore
        }

        public async Task Start()
        {
            this.Log.Info("Starting server instance...");
            // Starts listening
            this.Listener.Start();
            while (this.Listener.IsListening)
            {
                try
                {
                    // Request handling
                    IAsyncResult asynchronousResult = this.Listener.BeginGetContext(new AsyncCallback(this.Process), this.Listener);
                    asynchronousResult.AsyncWaitHandle.WaitOne();
                }
                catch (Exception ex)
                {
                    this.Log.Error(ex.Message);
                }
            }
        }

        private async void Process(IAsyncResult result)
        {
            try
            {
                HttpListenerContext ctx = ((HttpListener)result.AsyncState).EndGetContext(result);
                //if(ctx.Request.RawUrl is null) { ctx.Response.OutputStream.Close(); }
                string RawAssetPath = ctx.Request.RawUrl.TrimStart('/');
                string FullAssetPath = Path.Combine(this.RootWebDirectory, RawAssetPath);
                this.Log.Info("Incoming request [/{0}]; asset delivery: {1}", RawAssetPath, FullAssetPath);

                // Handle mime types
                string fileType = Path.GetExtension(FullAssetPath);
                if (this.MimeTypesFileAssociations.ContainsKey(fileType))
                    ctx.Response.ContentType = this.MimeTypesFileAssociations[fileType];
                else
                    ctx.Response.ContentType = "application/octet-stream";

                ctx.Response.SendChunked = false;
                ctx.Response.KeepAlive = true;
                if (File.Exists(FullAssetPath))
                {
                    using (Stream fs = File.OpenRead(FullAssetPath))
                    {
                        ctx.Response.ContentLength64 = fs.Length;
                        await fs.CopyToAsync(ctx.Response.OutputStream);
                    }

                }

                ctx.Response.StatusCode = (int)HttpStatusCode.OK;

                // ctx.Response.OutputStream.Write(Encoding.UTF8.GetBytes("Hello world!"), 0, Encoding.UTF8.GetByteCount("Hello world!"));
                await ctx.Response.OutputStream.FlushAsync();
                ctx.Response.Close();
            }
            catch(Exception ex)
            {
                this.Log.Info(ex.Message);
            }
        }
    }
}
