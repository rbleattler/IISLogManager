#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace IISLogManager.Core;

public class ConnectionManager {
	private static HttpClientHandler _netClientHandler = new() {
		//TODO: Add support for sending with non-default credentials
		UseDefaultCredentials = true
	};

	private HttpClient _netClient = new(_netClientHandler);

	public Uri? Uri { get; set; }

	public string? Uristring { get; private set; }

	public string? BearerToken { get; set; }


	public void SetConnection(string uri) {
		//TODO: Set other defaults?
		SetUri(uri);
		_netClient
			.DefaultRequestHeaders
			.Accept
			.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_netClient
			.DefaultRequestHeaders
			.Add("accept", "application/json");
	}

	public void SetConnection(string uri, string authToken) {
		//TODO: Set other defaults?
		SetUri(uri);
		SetAuthToken(authToken);
		_netClient
			.DefaultRequestHeaders
			.Accept
			.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		_netClient
			.DefaultRequestHeaders
			.Add("accept", "application/json");
		_netClient
			.DefaultRequestHeaders
			.Add("Authorization", "Bearer " + BearerToken);
	}

	public HttpStatusCode AddLog(IISLogObject log) {
		var jsonLog = log.ToJson();
		var postRequest = _netClient.PostAsync(
			requestUri: Uri,
			content: new StringContent(jsonLog, encoding: Encoding.GetEncoding(jsonLog))
		);
		postRequest.Wait();
		return postRequest.Result.StatusCode;
	}

	public HttpStatusCode AddLogs(IISLogObjectCollection logs, string? siteUrl = null,
		string? siteName = null,
		string? hostName = null) {
		var byteLogs = logs.ToJsonByteArray(siteUrl, siteName, hostName);
		//TODO: URGENT Mirror C:\Repos\IISLogManager\IISLogManager\Public\Compress-Data.ps1
		//TODO: var byteLogsMbSize = byteLogs?.Length / 1024 / 1024;
		//TODO: Stream if bytelogsMbSize > *some target size*
		var postRequest = _netClient.PostAsync(
			requestUri: Uri,
			content: new ByteArrayContent(byteLogs),
			CancellationToken.None //TODO: CancellationTokens
		);
		postRequest.Wait();
		return postRequest.Result.StatusCode;
		//TODO: StatusCode Error Handler 
	}

	private void SetUri(string uri) {
		Uri = new Uri(uri);
		Uristring = Uri.ToString();
	}

	private void SetAuthToken(string authToken) {
		BearerToken = authToken;
	}

	public ConnectionManager() { }

	public ConnectionManager(string uri) {
		SetConnection(uri);
	}

	public ConnectionManager(string uri, string authToken) {
		SetConnection(uri, authToken);
	}
}