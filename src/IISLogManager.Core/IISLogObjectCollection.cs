#nullable enable
using System.Text;

namespace IISLogManager.Core {
	public class IISLogObjectCollection : List<IISLogObject>, IComparable<IISLogObjectCollection>, IComparable {
		public IISLogObjectCollection() { }
		public IISLogObjectCollection(IEnumerable<IISLogObject> collection) : base(collection) { }
		public IISLogObjectCollection(int capacity) : base(capacity) { }

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
		public void WriteToFile(string filePath) {
			var byteLogs = ToJsonByteArray();
			var targetDirectory = Path.GetDirectoryName(filePath);
			if ( !Directory.Exists(targetDirectory) ) Directory.CreateDirectory(targetDirectory!);
			if ( File.Exists(filePath) ) File.Create(filePath);
			File.WriteAllBytes(filePath,
				byteLogs ?? throw new NullReferenceException("WriteToFile : byteLogs was null"));
		}

		//TODO: Fix ToJson()
		//I know there is a better way to do this... I just don't have the bandwidth at the moment... 

		public string? ToJson(string? siteUrl = null, string? siteName = null, string? hostName = null) {
			if ( !this.Any() ) return null;
			//TODO: Create an object builder for sending remote data
			StringBuilder stringBuilder = new();
			stringBuilder.AppendLine("{");
			if ( null != siteName ) {
				stringBuilder.AppendLine($"\"SiteName\" : \"{siteName}\",");
			}

			if ( null != siteUrl ) {
				stringBuilder.AppendLine($"\"SiteUrl\" : \"{siteUrl}\",");
			}

			if ( null != hostName ) {
				stringBuilder.AppendLine($"\"HostName\" : \"{hostName}\",");
			}

			stringBuilder.AppendLine("\"Logs\":[");
			var processedCount = 0;
			foreach (var site in this) {
				var json = site.ToJson();
				if ( processedCount < Count - 1 ) {
					stringBuilder.AppendFormat("{0},", json)
						.AppendLine();
				} else {
					stringBuilder.AppendFormat("{0}", json)
						.AppendLine();
				}

				processedCount++;
			}

			stringBuilder.AppendLine("]");
			stringBuilder.AppendLine("}");
			return stringBuilder.ToString();
		}

		public void FilterLogs(DateTime startDate, DateTime endDate) {
			var filteredLogs = this.Where(l => l.LogDateTime >= startDate && l.LogDateTime <= endDate);
			RemoveAll(l => !filteredLogs.Contains(l));
		}

		public byte[]? ToJsonByteArray(string? siteUrl = null, string? siteName = null, string? hostName = null) {
			var jsonOut = ToJson(siteUrl ?? null, siteName ?? null, hostName ?? null);
			if ( null != jsonOut ) return Encoding.ASCII.GetBytes(jsonOut);
			return null;
		}
	}
}