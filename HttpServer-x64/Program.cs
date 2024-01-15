using HttpServer_x64.Internals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Serialization;

namespace HttpServer_x64
{
    public class Program
    {
        static void Main(string[] args)
        {
            Logger logger = new Logger(Path.Combine(".", "Logs", "server"));

            // Read configuration values
            JsonConfigIO<OperationConfig> OperationConfigIO = new JsonConfigIO<OperationConfig>(Path.Combine(".", "Config", "OperationConfig.json"));
            if(!OperationConfigIO.Load(out OperationConfig OperationCFG))
            {
                logger.Error("First-time starting up, halting...");
            }
            else
            {
                if(OperationCFG.WebserverDirectory == "-")
                {
                    logger.Error("Web server directory not set; Halting...");
                }
                else
                {
                    HttpServer server = new HttpServer(OperationCFG.WebserverDirectory, OperationCFG.Ports, logger);
                    server.Start().ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }

            Console.ReadKey();

            /*HttpServer p = new HttpServer("D:\\Projects\\CS\\web\\www", 8080);
            p.Start().ConfigureAwait(false).GetAwaiter().GetResult();
            //Console.ReadKey()*/
        }
    }
}
