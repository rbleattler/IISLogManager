#nullable enable
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;

namespace IISLogManager.Core;

[Serializable]
public class IISLogObjectCollection : List<IISLogObject>, IComparable<IISLogObjectCollection>, IComparable,
	IDisposable {
	public IISLogObjectCollection() { }
	public IISLogObjectCollection(IEnumerable<IISLogObject> collection) : base(collection) { }
	public IISLogObjectCollection(int capacity) : base(capacity) { }

	private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {
		DateTimeZoneHandling = DateTimeZoneHandling.Local,
		DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss tt",
		PreserveReferencesHandling = PreserveReferencesHandling.All,
		NullValueHandling = NullValueHandling.Include,
		MaxDepth = 3,
		ReferenceLoopHandling = ReferenceLoopHandling.Serialize
	};

	public int CompareTo(IISLogObjectCollection other) {
		throw new NotImplementedException();
	}

	public int CompareTo(object? obj) {
		if ( ReferenceEquals(null, obj) ) return 1;
		if ( ReferenceEquals(this, obj) ) return 0;
		return obj is IISLogObjectCollection other
			? CompareTo(other)
			: throw new ArgumentException($"Object must be of type {nameof(IISLogObjectCollection)}");
	}

	//TODO: https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentbag-1?view=net-6.0
	public void WriteToFile(string filePath, string? siteUrl = null, string? siteName = null,
		string? hostName = null) {
		UpdateLogData(siteUrl, siteName, hostName);
		var targetDirectory = Path.GetDirectoryName(filePath);
		if ( !Directory.Exists(targetDirectory) ) Directory.CreateDirectory(targetDirectory!);
		using (StreamWriter file = File.AppendText(filePath)) {
			Console.WriteLine("[DEBUG] Writing to file...");
			var jsonSerializer = JsonSerializer.Create(_jsonSerializerSettings);
			jsonSerializer.Serialize(file, this);
		}
	}

	//TODO: Fix ToJson()
	//I know there is a better way to do this... I just don't have the bandwidth at the moment... 

	public string? ToJson(string? siteUrl = null, string? siteName = null, string? hostName = null) {
		UpdateLogData(siteUrl, siteName, hostName);
		if ( !this.Any() ) return null;
		TrimExcess();
		return ToIISLogData(siteUrl, siteName, hostName).ToJson();
		//TODO: Create an object builder for sending remote data
		//TODO: TEST
	}

	public void FilterLogs(DateTime startDate, DateTime endDate) {
		var filteredLogs = this.Where(l => l.DateTime >= startDate && l.DateTime <= endDate);
		RemoveAll(l => !filteredLogs.Contains(l));
		TrimExcess();
	}

	private void UpdateLogData(string? siteUrl = null, string? siteName = null,
		string? hostName = null) {
		Parallel.ForEach(this, log => {
			log.SiteName = siteName;
			log.SiteUrl = siteUrl;
			log.HostName = hostName;
		});
	}

	public IISLogData ToIISLogData(string? siteUrl = null, string? siteName = null,
		string? hostName = null) {
		TrimExcess();
		UpdateLogData(siteUrl, siteName, hostName);
		var iisLogObjectCollection = this;
		IISLogData toOut = new IISLogData(siteUrl, siteName, hostName, ref iisLogObjectCollection);
		TrimExcess();
		GC.Collect();
		return toOut;
	}

	public byte[] ToJsonByteArray(string? siteUrl = null, string? siteName = null, string? hostName = null) {
		UpdateLogData(siteUrl, siteName, hostName);
		var jsonOut = ToJson(siteUrl ?? null, siteName ?? null, hostName ?? null);
		Clear();
		var bytesOut = Encoding.ASCII.GetBytes(jsonOut);
		jsonOut = null;
		GC.Collect();
		return bytesOut;
		// return null;
	}

	public void Dispose() {
		Clear();
		GC.Collect();
		GC.SuppressFinalize(this);
	}
}