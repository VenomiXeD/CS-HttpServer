using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpServer_x64
{
    public class Logger
    {
        private TextWriter LogWriter {  get; set; }
        public bool DoEmitToConsole { get; set; } = true;
        public bool DoEmitToLogFile { get; set; } = true;
        /// <summary>
        /// 
        /// </summary>
        private ConsoleColor _previousConsoleColor;
        /// <summary>
        /// Path to the log file
        /// </summary>
        private string _logFilePath;
        private Queue<string> logContentQueue = new Queue<string>();
        private bool isOutputWorking = false;
        public Logger(string LoggerFilePath)
        {
            this._logFilePath = LoggerFilePath + "-" + DateTime.Now.ToString().Replace(":", "-").Replace(" ", "-") + ".log";
            LogWriter = new StreamWriter(this._logFilePath);
        }

        private void CacheConsoleColor()
        {
            this._previousConsoleColor = Console.ForegroundColor;
        }
        private void RevertConsoleColor()
        {
            Console.ForegroundColor = this._previousConsoleColor;
        }
        private string GetLogTimestamp()
        {
            return DateTime.Now.ToLongTimeString();
        }
        private void Output(string Output)
        {
            if (this.DoEmitToConsole)
            {
                Console.WriteLine(Output);
            }
            if (this.DoEmitToLogFile)
            {
                this.logContentQueue.Enqueue(Output);
                this.HandleOutputToFile();
            }
        }
        private void HandleOutputToFile()
        {
            this.isOutputWorking = true;
            if (this.logContentQueue.Count > 0)
            {
                while (logContentQueue.TryPeek(out string _))
                {
                    LogWriter.WriteLine(this.logContentQueue.Dequeue());
                }
            }
        }
        public void Info(string format, params object[] args)
        {
            this.CacheConsoleColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            this.Output($"[{this.GetLogTimestamp()} INFO]: {string.Format(format, args)}");
            this.RevertConsoleColor();
        }

        public void Warn(string format, params object[] args)
        {
            this.CacheConsoleColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            this.Output($"[{this.GetLogTimestamp()} WARN]: {string.Format(format, args)}");
            this.RevertConsoleColor();
        }

        public void Error(string format, params object[] args)
        {
            this.CacheConsoleColor();
            Console.ForegroundColor = ConsoleColor.Cyan;
            this.Output($"[{this.GetLogTimestamp()} ERRO]: {string.Format(format, args)}");
            this.RevertConsoleColor();
        }
    }
}
