﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System.Net;
using System.Reflection;
using System.Text;

namespace HttpServer_x64.Internals
{
    public class HttpServer
    {
        // == HTTP related == //
        /// <summary>
        /// The HTTP listener instance, listens for requests
        /// </summary>
        public HttpListener Listener { get; set; }

        public string RootWebDirectory { get; set; }

        private Dictionary<string, string> MimeTypesFileAssociations { get; set; } = new Dictionary<string, string>();
        // == Utility related == //
        private readonly Logger Log;
        public static string ExecutingPath { get => Path.GetFullPath("."); }

        private Assembly[] externalReferences;
        public HttpServer(string rootWebDirectory, int[] Ports, Logger log = null, params Assembly[] externalReferences)
        {
            // == Utility related == //
            // Create Logs directory
            Directory.CreateDirectory(Path.Combine(HttpServer.ExecutingPath, "logs"));
            // Start a log instance
            this.Log = log ?? new Logger(Path.Combine(HttpServer.ExecutingPath, "logs", "log"));
            this.RootWebDirectory = rootWebDirectory;

            // == HTTP related == //
            // Creates a new listener instance
            this.Listener = new HttpListener();
            // Configures the listener
            foreach (int p in Ports)
            {
                Log.Info("Registering port: " + p);
                this.Listener.Prefixes.Add($"http://*:{p}/");
            }

            this.RootWebDirectory = rootWebDirectory;

            // Load in mimetypes
            // We need to determine the MIME-type and set it to the output
            Log.Info("Reading MIMETYPES and Deserializing");
#pragma warning disable
            using (Stream mimeTypesStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.ManifestFullNameFromPartialName("mimetypes.json")))
            {
                using (TextReader reader = new StreamReader(mimeTypesStream))
                {
                    using (JsonTextReader r = new JsonTextReader(reader))
                    {
                        MimeTypesFileAssociations = JsonSerializer.CreateDefault().Deserialize<Dictionary<string, string>>(r);
                        Log.Info("Mimetypes loaded: {0}", MimeTypesFileAssociations.Count);
                        /*foreach (KeyValuePair<string, string> file_mimetype in MimeTypesFileAssociations)
                        {
                            Log.Info($"{file_mimetype.Key} - {file_mimetype.Value}");
                        }*/
                    }
                }
            }

            this.externalReferences = externalReferences ?? [];
#pragma warning restore
        }

        public static bool IsServerEnvironment()
        {
            return isServerEnvironment;
        }
        private static bool isServerEnvironment;

        /// <summary>
        /// VARNAME: Context
        /// </summary>
        public static HttpListenerContext DummyContext { get => null; }
        private static HttpListenerContext __thelistenerContext = null;
        /// <summary>
        /// VARNAME: ScriptHelper
        /// </summary>
        public static ScriptHelper DummyHelper { get => null; }
        private static ScriptHelper __thescripthelper = null;

        public string ManifestFullNameFromPartialName(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(name));
        }

        public async void Start()
        {
            this.Log.Info("Starting server instance...");
            // Starts listening
            this.Listener.Start();
            this.Log.Info($"Listening: {string.Join(";", this.Listener.Prefixes)}");
            this.Log.Info($"Serving: {this.RootWebDirectory}");

            while (this.Listener.IsListening)
            {
                try
                {

                    // Request handling
                    IAsyncResult r = this.Listener.BeginGetContext(new AsyncCallback(this.Process), this.Listener);
                    r.AsyncWaitHandle.WaitOne(-1, true);
                    //this.Process(await this.Listener.GetContextAsync());
                }
                catch (Exception ex)
                {
                    this.Log.Error(ex.Message);
                }
            }
        }

        private async void Process(IAsyncResult result)
        {
            HttpListenerContext ctx = ((HttpListener)result.AsyncState).EndGetContext(result);
            new Thread(async delegate ()
            {
                try
                {
                    ctx.Response.SendChunked = false;
                    ctx.Response.KeepAlive = true;

                    //if(ctx.Request.RawUrl is null) { ctx.Response.OutputStream.Close(); }
                    string RawAssetPath = ctx.Request.Url.LocalPath.TrimStart('/');
                    string FullAssetPath = Path.Combine(this.RootWebDirectory, RawAssetPath);
                    this.Log.Info("Incoming request [/{0}]; asset delivery: {1}", RawAssetPath, FullAssetPath);

                    #region File handling
                    if (File.Exists(FullAssetPath))
                    {
                        // Handle mime types
                        string fileType = Path.GetExtension(FullAssetPath);
                        if (this.MimeTypesFileAssociations.ContainsKey(fileType))
                            ctx.Response.ContentType = this.MimeTypesFileAssociations[fileType];
                        else
                            ctx.Response.ContentType = "application/octet-stream";


                        if (Path.GetExtension(FullAssetPath) != ".httphandle")
                        {
                            using (FileStream fs = File.OpenRead(FullAssetPath))
                            {
                                Log.Info("Writing to stream...");
                                ctx.Response.ContentLength64 = fs.Length;
                                await fs.CopyToAsync(ctx.Response.OutputStream);
                                await ctx.Response.OutputStream.FlushAsync();

                            }
                            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                        }
                        else
                        {
                            isServerEnvironment = true;
                            #region Dynamic scripting
                            Script<object> css = CSharpScript.Create<object>(File.ReadAllText(FullAssetPath), ScriptOptions.Default.WithReferences(this.externalReferences).WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location)).Select(x => MetadataReference.CreateFromFile(x.Location)).ToArray()), typeof(ScriptGlobals));

                            ScriptGlobals scriptGlobals = new ScriptGlobals() { Context = ctx };
                            scriptGlobals.ScriptHelper = new ScriptHelper() { _ctx = ctx };

                            __thelistenerContext = ctx;
                            __thescripthelper = scriptGlobals.ScriptHelper;

                            ScriptState<object> v = css.RunAsync(scriptGlobals)?.Result;
                            Console.WriteLine(v.ReturnValue);
                            #endregion
                        }
                    }
                    #endregion
                    #region Directory listing handling
                    else if (Directory.Exists(FullAssetPath))
                    {
                        if (!ctx.Request.Url.LocalPath.EndsWith("/"))
                        {
                            ctx.Response.Redirect(ctx.Request.Url.LocalPath + "/");
                        }
                        else
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                            // Generate directory listing
                            string[] directories = Directory.GetDirectories(FullAssetPath).Select(x => Path.GetRelativePath(FullAssetPath, x)).ToArray();
                            string[] files = Directory.GetFiles(FullAssetPath).Select(x => Path.GetRelativePath(FullAssetPath, x)).ToArray();

                            StringBuilder directoryListing = new StringBuilder();
                            foreach (string dir in directories)
                            {
                                directoryListing.Append($"<a href=\"{dir}\">[{dir}]</a><br>");
                            }

                            StringBuilder filesListing = new StringBuilder();
                            foreach (string file in files)
                            {
                                filesListing.Append($"<a href=\"{file}\">[{file}]</a><br>");
                            }

                            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(this.ManifestFullNameFromPartialName("listing.html")))
                            {
                                using (TextReader tr = new StreamReader(s))
                                {

                                    string responseText = string.Format(await tr.ReadToEndAsync(), directoryListing.ToString(), filesListing.ToString());
                                    using (TextWriter tw = new StreamWriter(ctx.Response.OutputStream))
                                    {
                                        ctx.Response.ContentLength64 = Encoding.UTF8.GetByteCount(responseText);
                                        await tw.WriteAsync(responseText);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        this.Log.Warn($"Requested asset does not exist\nMETHOD: {ctx.Request.HttpMethod}\nIP: {ctx.Request.RemoteEndPoint.Address}\nUser: {ctx.Request.UserAgent}");
                        if(ctx.Request.HasEntityBody)
                        {
                            using StreamReader r = new StreamReader(ctx.Request.InputStream);
                            this.Log.Warn("Data: " + r.ReadToEnd());
                        }
                    }
                    #endregion



                    // ctx.Response.OutputStream.Write(Encoding.UTF8.GetBytes("Hello world!"), 0, Encoding.UTF8.GetByteCount("Hello world!"));

                }
                catch (Exception ex)
                {
                    this.Log.Info(ex.Message + "\n" + ex.StackTrace);
                }
                finally
                {
                    try
                    {
                        ctx.Response.Close();
                    }
                    catch
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        ctx.Response.Close();
                    }
                }

            }).Start();
        }
    }

}