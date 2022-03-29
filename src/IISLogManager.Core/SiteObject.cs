using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Web.Administration;
using Newtonsoft.Json;


namespace IISLogManager.Core;

[Serializable]
public class SiteObject : IDisposable {
	public string HostName;
	public string SiteName;
	public string SiteUrl;
	public long Id;
	public string LogRoot;
	public string IntrinsicLogRoot;
	public LogFormat LogFormat;
	public List<string> LogFilePaths;
	public IISLogObjectCollection Logs = new();
	public ConcurrentBag<IISLogObject> ConcurrentLogs = new();
	public List<string> CompressedLogs = new();
	private ParseEngine _logParser = new();

	private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {
		DateTimeZoneHandling = DateTimeZoneHandling.Local,
		DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss tt",
		PreserveReferencesHandling = PreserveReferencesHandling.All
	};

	public void ParseAllLogs() {
		//TODO: Calculate total size of logs, account for total size in memory, if over 50% of available memory will be used
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

		if ( !dirExists ) Console.WriteLine($"The log root {IntrinsicLogRoot} does not exist. Skipped site logs...");
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

	public void ParseLogs(string filePath, int startLine, int count, string? headerLine) {
		var logParserInstance = new ParseEngine();
		logParserInstance.FilePath = filePath;
		logParserInstance.ParsePartialLog(startLine, count, ref Logs, headerLine);
	}


	public Task ParseLogsAsync(string filePath, CancellationToken? cancellationToken) {
		var logParserInstance = new ParseEngine();
		logParserInstance.FilePath = filePath;
		// Debug.WriteLine("Parsing" + filePath);
		var parsedLogs = logParserInstance.ParseLog();
		foreach (IISLogObject logObject in parsedLogs) {
			ConcurrentLogs.Add(logObject);
		}

		return Task.CompletedTask;
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

	public void CompressLog(IISLogObject log, bool removeFromLogs) {
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
			return info.LastWriteTime.Ticks >= startDate.Ticks && info.LastWriteTime.Ticks <= endDate.Ticks;
		});
		LogFilePaths.RemoveAll(path => !filteredLogFiles.Contains(path));
	}
	//endregion Filter Log paths

	public void WriteToFile(string filePath) {
		var targetDirectory = Path.GetDirectoryName(filePath);
		if ( !Directory.Exists(targetDirectory) ) Directory.CreateDirectory(targetDirectory!);
		using (StreamWriter file = File.AppendText(filePath)) {
			Console.WriteLine("[DEBUG] Writing to file...");
			var jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);
			jsonSerializer.Serialize(file, this);
		}
	}

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
		Logs.WriteToFile(filePath, SiteUrl, SiteName, HostName);
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