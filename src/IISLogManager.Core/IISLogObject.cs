using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace IISLogManager.Core;

[JsonSerializable(typeof(IISLogObject))]
public class IISLogObject : MPropertyAsStringSettable {
	private Guid _uniqueId = Guid.NewGuid();
	private DateTime _dateTime;
	private string _sSiteName;
	private string _siteUrl;
	private string _sComputerName;
	private string _sIp;
	private string _csMethod;
	private string _csUriStem;
	private string _csUriQuery;
	private int? _sPort;
	private string _csUsername;
	private string _cIp;
	private string _csVersion;
	private string _csUserAgent;
	private string _csCookie;
	private string _csReferer;
	private string _csHost;
	private int? _scStatus;
	private int? _scSubstatus;
	private long? _scWin32Status;
	private int? _scBytes;
	private int? _csBytes;
	private int? _timeTaken;
	private string _xForwardedFor;

	private JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {
		DateTimeZoneHandling = DateTimeZoneHandling.Local,
		DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss tt",
		PreserveReferencesHandling = PreserveReferencesHandling.All
	};

	[Key]
	public string UniqueId {
		get => _uniqueId.ToString();
		set => _uniqueId = new Guid(value);
	}


	public DateTime DateTime {
		get => _dateTime;
		set => _dateTime = value;
	}

	public string SiteName {
		get => _sSiteName;
		set => _sSiteName = value;
	}

	public string SiteUrl {
		get => _siteUrl;
		set => _siteUrl = value;
	}

	public string ComputerName {
		get => _sComputerName;
		set => _sComputerName = value;
	}

	public string ServerIp {
		get => _sIp;
		set => _sIp = value;
	}

	public string Method {
		get => _csMethod;
		set => _csMethod = value;
	}

	public string UriStem {
		get => _csUriStem;
		set => _csUriStem = value;
	}

	public string UriQuery {
		get => _csUriQuery;
		set => _csUriQuery = value;
	}

	public int? ServerPort {
		get => _sPort;
		set => _sPort = value;
	}

	public string Username {
		get => _csUsername;
		set => _csUsername = value;
	}

	public string ClientIp {
		get => _cIp;
		set => _cIp = value;
	}

	public string Version {
		get => _csVersion;
		set => _csVersion = value;
	}

	public string UserAgent {
		get => _csUserAgent;
		set => _csUserAgent = value;
	}

	public string Cookie {
		get => _csCookie;
		set => _csCookie = value;
	}

	public string Referer {
		get => _csReferer;
		set => _csReferer = value;
	}

	public string HostName {
		get => _csHost;
		set => _csHost = value;
	}

	public int? HttpStatus {
		get => _scStatus;
		set => _scStatus = value;
	}

	public int? ProtocolSubstatus {
		get => _scSubstatus;
		set => _scSubstatus = value;
	}

	public long? Win32Status {
		get => _scWin32Status;
		set => _scWin32Status = value;
	}

	public int? ServerClientBytes {
		get => _scBytes;
		set => _scBytes = value;
	}

	public int? ClientServerBytes {
		get => _csBytes;
		set => _csBytes = value;
	}

	public int? TimeTaken {
		get => _timeTaken;
		set => _timeTaken = value;
	}

	public string ForwardedFor {
		get => _xForwardedFor;
		set => _xForwardedFor = value;
	}

	public IISLogObject() { }

	public IISLogObject(string jsonObject) {
		FromJson(jsonObject);
	}
	// private From

	public IISLogObject(
		Guid uniqueId,
		DateTime dateTime,
		string sSiteName,
		string siteUrl,
		string sComputerName,
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
		UniqueId = uniqueId.ToString();
		DateTime = dateTime;
		SiteName = sSiteName;
		SiteUrl = siteUrl;
		ComputerName = sComputerName;
		ServerIp = sIp;
		Method = csMethod;
		UriStem = csUriStem;
		UriQuery = csUriQuery;
		Username = csUsername;
		ClientIp = cIp;
		Version = csVersion;
		UserAgent = csUserAgent;
		Cookie = csCookie;
		Referer = csReferer;
		HostName = csHost;
		ForwardedFor = xForwardedFor;
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

	public IISLogObject FromJson(string jsonObject) {
		JsonConvert.PopulateObject(jsonObject, this);
		return this;
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

	public string GetProperty(string propertyName) {
		return (string) typeof(IISLogObject)?.GetProperty(propertyName)?.GetValue(this, null);
	}
}