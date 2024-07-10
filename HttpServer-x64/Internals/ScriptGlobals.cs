using System.Net;
using System.Text;

namespace HttpServer_x64.Internals
{
    public class ScriptGlobals
    {
        public ScriptHelper ScriptHelper { get; set; }
        public HttpListenerContext Context { get; internal set; }
        public ScriptGlobals()
        {
        }

        private void Test()
        {
            Console.WriteLine("Test");
        }
    }
    public class ScriptHelper
    {
        public HttpListenerContext _ctx { get; set; }

        public string ReadInputStreamAsString()
        {
            Console.WriteLine("Reading body... [String]");
            using (StreamReader r = new StreamReader(this._ctx.Request.InputStream))
            {
                return r.ReadToEnd();
            }
        }

        public MemoryStream ReadInputStreamAsStream()
        {
            Console.WriteLine("Reading body... [Stream]");
            MemoryStream ms = new MemoryStream();

            _ctx.Request.InputStream.CopyTo(ms);
            return new MemoryStream(ms.ToArray());
        }

        public void WriteOutputStreamWithString(string str)
        {
            _ctx.Response.ContentLength64 = str.Length;
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            _ctx.Response.OutputStream.Write(buffer,0, buffer.Length);
        }
    }
}