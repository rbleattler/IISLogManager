using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Web.Administration;


namespace IISLogManager.Core {
    [Serializable]
    public class SiteObject {
        public string HostName;
        public string SiteName;
        public string SiteUrl;
        public long Id;
        public object LogFileData;
        public string LogRoot;
        public string IntrinsicLogRoot;
        public LogFormat LogFormat;
        public string[] LogFilePaths;
        public IISLogObjectCollection Logs = new();

        public System.Collections.Generic.List<string> CompressedLogs = new();
        //TODO: Filter Logs
        // private bool LogsParsed = false;
        // private string ParsedLogsPath;
        private ParseEngine _logParser = new();

        public void ParseAllLogs() {
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (var logFile in LogFilePaths) {
                ParseLogs(logFile);
            }
            stopwatch.Stop();
            Console.WriteLine($"processed {Logs.Count} logs in {stopwatch.Elapsed}");
            System.GC.Collect();
        }

        public void ParseLogs(string filePath) {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            _logParser.FilePath = filePath;
            Debug.WriteLine("Parsing" + filePath);
            var parsedLogs = _logParser.ParseLog();
            foreach (IISLogObject logObject in parsedLogs) {
                Logs.Add(logObject);
            }
            //stopwatch.Stop();
            //Debug.WriteLine($"processed {filePath} in {stopwatch.Elapsed}");
        }

        // TODO: Explore this?
        // public void ParallelParseAllLogs() {
        //     Parallel.ForEach(LogFilePaths, ParallelParseLogs);
        //     System.GC.Collect();
        // }

        public void CompressLog(IISLogObject log) {
            // Remove from Logs by default
            CompressLog(log, true);
        }

        public void CompressAllLogs() {
            while (Logs.Count > 0) {
                CompressLog(Logs[0]);
            }

            System.GC.Collect();
        }

        public void CompressLog(IISLogObject log, bool removeFromLogs) {
            Debug.WriteLine("Compressing" + log.UniqueId);
            string jLog = log.ToJson();
            string compressedLog = Utils.CompressString(jLog);
            CompressedLogs.Add(compressedLog);
            if (removeFromLogs) {
                Logs.Remove(log);
            }
        }



        // TODO: Explore this?
        // public void ParallelParseLogs(string filePath) {
        //     LogParser.FilePath = filePath;
        //     Debug.WriteLine("Parsing" + filePath);
        //     var parsedLogs = LogParser.ParseLog();
        //     Parallel.ForEach(parsedLogs, logObject => Logs.Add(logObject));
        // }

        public void SaveLogs(string filePath) {
            var file = File.Open(filePath, FileMode.OpenOrCreate);
            StreamWriter stream = new StreamWriter(file);
            var content = BuildJsonFileContent();
            stream.Write(content);
            stream.Close();
            stream.Dispose();
        }

        private string BuildJsonFileContent() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("[");
            foreach (IISLogObject logObject in Logs) {
                stringBuilder.AppendLine(logObject.ToJson());
                stringBuilder.Append(",");
            }

            stringBuilder.AppendLine("]");
            stringBuilder.AppendLine("}");
            return stringBuilder.ToString();
        }

        public SiteObject() { }

        bool LogsIsNull() => null == Logs;
    }
}