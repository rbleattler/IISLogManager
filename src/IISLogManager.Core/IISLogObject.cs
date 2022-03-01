using Newtonsoft.Json;

namespace IISLogManager.Core;

[Serializable]
public class IISLogObject : MPropertyAsStringSettable {
	public Guid UniqueId { get; set; } = Guid.NewGuid();

	public DateTime LogDateTime { get; set; }

	public string sSitename { get; set; }

	public string sComputername { get; set; }

	public string sIp { get; set; }

	public string csMethod { get; set; }

	public string csUriStem { get; set; }

	public string csUriQuery { get; set; }

	public int? sPort { get; set; }

	public string csUsername { get; set; }

	public string cIp { get; set; }

	public string csVersion { get; set; }

	public string csUserAgent { get; set; }

	public string csCookie { get; set; }

	public string csReferer { get; set; }

	public string csHost { get; set; }

	public int? scStatus { get; set; }

	public int? scSubstatus { get; set; }

	public long? scWin32Status { get; set; }

	public int? scBytes { get; set; }

	public int? csBytes { get; set; }

	public int? timeTaken { get; set; }

	public string xForwardedFor { get; set; }
	public IISLogObject() { }

	public IISLogObject(
		Guid uniqueId,
		DateTime logDateTime,
		string sSitename,
		string sComputername,
		string sIp,
		string csMethod,
		string csUriStem,
		string csUriQuery,
		string csUsername,
		string cIp,
		string csVersion,
		string csUserAgent,
		string csCookie,
		string csReferer,
		string csHost,
		string xForwardedFor
	) {
		UniqueId = uniqueId;
		LogDateTime = logDateTime;
		this.sSitename = sSitename;
		this.sComputername = sComputername;
		this.sIp = sIp;
		this.csMethod = csMethod;
		this.csUriStem = csUriStem;
		this.csUriQuery = csUriQuery;
		this.csUsername = csUsername;
		this.cIp = cIp;
		this.csVersion = csVersion;
		this.csUserAgent = csUserAgent;
		this.csCookie = csCookie;
		this.csReferer = csReferer;
		this.csHost = csHost;
		this.xForwardedFor = xForwardedFor;
	}

	/// <summary> 
	/// This is meant to build an IISLogObject from a hashtable / dictionary
	/// Currently the SetProperty function *seems to be broken*  
	/// </summary>
	/// <param name="inputArgs"></param>
	// public IISLogObject(Dictionary<string, string> inputArgs) {
	// 	var type = GetType();
	// 	var properties = type.GetProperties();
	// 	var propertyNames = new string[properties.Count()];
	// 	for (var i = 0; i < properties.Count(); i++) {
	// 		if ( propertyNames.Contains(properties[i].Name) ) {
	// 			// properties.SetValue(properties[i].Name, i);
	// 			SetProperty(properties[i].Name, i);
	// 		}
	// 	}
	//
	// 	if ( inputArgs["date"] != null && inputArgs["time"] != null ) {
	// 		DateTime finalDate = DateTime.Parse($"{inputArgs["date"]} {inputArgs["time"]}");
	// 	}
	//
	// 	foreach (var key in inputArgs.Keys) {
	// 		if ( propertyNames.Contains(key.ToString()) ) {
	// 			var value = inputArgs[key];
	// 			var thisProperty = type.GetProperty(key.ToString());
	// 			thisProperty.SetValue(this, value);
	// 		}
	// 	}
	// }
	public string ToJson() {
		string serializedObject = JsonConvert.SerializeObject(this, Formatting.None);
		return serializedObject;
	}

	public void AppendToFile(string filePath) {
		AppendToFile(filePath, true);
	}

	public void AppendToFile(string filePath, bool withComma) {
		string jsonLog;
		if ( !withComma ) {
			jsonLog = ToJson();
		} else {
			jsonLog = string.Format("{0},", ToJson());
		}

		File.AppendAllText(filePath, jsonLog);
	}

	public void SetProperty(string propertyName, object value) {
		typeof(IISLogObject)?.GetProperty(propertyName)?.SetValue(this, value);
	}

	public dynamic GetProperty(string propertyName) {
		return typeof(IISLogObject)?.GetProperty(propertyName)?.GetValue(this, null);
	}
}