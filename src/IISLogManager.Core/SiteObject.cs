using System.Diagnostics;
using Microsoft.Web.Administration;


namespace IISLogManager.Core {
	[Serializable]
	public class SiteObject : IDisposable {
		public string HostName;
		public string SiteName;
		public string SiteUrl;
		public long Id;
		public object LogFileData;
		public string LogRoot;
		public string IntrinsicLogRoot;
		public LogFormat LogFormat;
		public List<string> LogFilePaths;
		public IISLogObjectCollection Logs = new();

		public List<string> CompressedLogs = new();
		private ParseEngine _logParser = new();

		public void ParseAllLogs() {
			var dirExists = Directory.Exists(IntrinsicLogRoot);
			if ( dirExists ) {
				Stopwatch stopwatch = Stopwatch.StartNew();
				foreach (var logFile in LogFilePaths) {
					if ( !File.Exists(logFile) ) {
						Console.WriteLine($"{logFile} does not exist...");
						continue;
					}

					ParseLogs(logFile);
				}

				stopwatch.Stop();
				Console.WriteLine($"processed {Logs.Count} logs in {stopwatch.Elapsed}");
				GC.Collect();
			}

			if ( !dirExists )
				Console.WriteLine($"The log root {IntrinsicLogRoot} does not exist. Skipped site logs...");
		}

		public void ParseLogs(string filePath) {
			//Stopwatch stopwatch = Stopwatch.StartNew();
			_logParser.FilePath = filePath;
			// Debug.WriteLine("Parsing" + filePath);
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

			GC.Collect();
		}

		public Span<IISLogObject> ToSpan() {
			return new Span<IISLogObject>(Logs.ToArray());
		}

		public void CompressLog(IISLogObject log, bool removeFromLogs) {
			// Debug.WriteLine("Compressing" + log.UniqueId);
			string jLog = log.ToJson();
			string compressedLog = Utils.CompressString(jLog);
			CompressedLogs.Add(compressedLog);
			if ( removeFromLogs ) {
				Logs.Remove(log);
			}
		}

		//region Filter Log Paths
		public void FilterLogFiles(DateTime startDate, DateTime endDate) {
			var filteredLogFiles = this.LogFilePaths.Where(l => {
				var info = new FileInfo(l);
				// Console.WriteLine("LastWriteTime : {0}", info.LastWriteTime);
				// Console.WriteLine("StartDate : {0}", startDate);
				// Console.WriteLine("EndDate : {0}", endDate);
				// Console.WriteLine($">= StartDate : {info.LastWriteTime.Ticks >= startDate.Ticks}");
				// Console.WriteLine($"<= EndDate : {info.LastWriteTime.Ticks <= endDate.Ticks}");
				return info.LastWriteTime.Ticks >= startDate.Ticks && info.LastWriteTime.Ticks <= endDate.Ticks;
			});
			LogFilePaths.RemoveAll(path => !filteredLogFiles.Contains(path));
		}
		//endregion Filter Log paths

		// TODO: Explore this?
		// public void ParallelParseLogs(string filePath) {
		//     LogParser.FilePath = filePath;
		//     Debug.WriteLine("Parsing" + filePath);
		//     var parsedLogs = LogParser.ParseLog();
		//     Parallel.ForEach(parsedLogs, logObject => Logs.Add(logObject));
		// }

		/// <summary>
		/// Save logs to file as JSON
		/// </summary>
		/// <param name="filePath"></param>
		public void SaveLogs(string filePath) {
			var file = File.Open(filePath, FileMode.OpenOrCreate);
			StreamWriter stream = new(file);
			var content = Logs.ToJson();
			stream.Write(content);
			stream.Close();
			stream.Dispose();
		}

		public string GetLogFileName(string targetDirectory) {
			if ( targetDirectory == null ) {
				targetDirectory =
					$"{Environment.GetEnvironmentVariable("USERPROFILE")}\\IISLogManager\\{DateTime.Now.ToString("yyyy-MM-dd")}";
			}

			var outFileName =
				$"{targetDirectory}\\{Utils.MakeSafeFilename(SiteName, '-')}-{Utils.GetRandom(5)}";
			return outFileName;
		}

		bool LogsIsNull() => null == Logs;

		public void Dispose() {
			Logs?.Dispose();
			_logParser?.Dispose();
		}
	}
}