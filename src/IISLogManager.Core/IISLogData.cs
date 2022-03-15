using Newtonsoft.Json;

namespace IISLogManager.Core;

[Serializable]
public class IISLogData {
	public string SiteName { get; }
	public string SiteUrl { get; }
	public string HostName { get; }
	public static IISLogObjectCollection Logs { get; set; } = Logs ?? new IISLogObjectCollection();

	private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {
		DateTimeZoneHandling = DateTimeZoneHandling.Local,
		DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss tt",
		PreserveReferencesHandling = PreserveReferencesHandling.All
	};

	public string ToJson() {
		return JsonConvert.SerializeObject(this, _jsonSerializerSettings);
	}

	public void AddLog(IISLogObject log) {
		Logs.Add(log);
	}

	public IISLogData(string? siteName, string? siteUrl, string? hostName,
		ref IISLogObjectCollection? logs) {
		SiteName = siteName;
		SiteUrl = siteUrl;
		HostName = hostName;
		Logs = logs;
	}

	public void WriteToFile(string filePath, string? siteUrl = null, string? siteName = null,
		string? hostName = null) {
		// UpdateLogData(siteUrl, siteName, hostName);
		var targetDirectory = Path.GetDirectoryName(filePath);
		if ( !Directory.Exists(targetDirectory) ) Directory.CreateDirectory(targetDirectory!);
		using (StreamWriter file = File.AppendText(filePath)) {
			Console.WriteLine("[[DEBUG]] Writing to file...");
			var jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);
			jsonSerializer.Serialize(file, this);
		}

		Console.WriteLine("[[DEBUG]] Finished writing to file!");
		// fileStream.Close();
		// fileStream.Dispose();
	}
}