using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace IISLogManager.Core;

public class ConnectionManager {
	private static HttpClientHandler NetClientHandler = new() {
		//TODO: Add support for sending with non-default credentials
		UseDefaultCredentials = true
	};

	private HttpClient NetClient = new(NetClientHandler);
	public Uri Uri { get; set; }

	public void SetConnection(string uri) {
		//TODO: Set other defaults?
		SetUri(uri);
		NetClient
			.DefaultRequestHeaders
			.Accept
			.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		NetClient
			.DefaultRequestHeaders
			.Add("accept", "application/json");
	}

	public HttpStatusCode AddLog(IISLogObject log) {
		var jsonLog = log.ToJson();
		var postRequest = NetClient.PostAsync(
			requestUri: Uri,
			content: new StringContent(jsonLog, encoding: Encoding.GetEncoding(jsonLog))
		);
		postRequest.Wait();
		return postRequest.Result.StatusCode;
	}

	public HttpStatusCode AddLogs(IISLogObjectCollection logs) {
		var byteLogs = logs.ToJsonByteArray();
		var byteLogsMbSize = byteLogs.Length / 1024 / 1024;
		//TODO: Stream if bytelogsMbSize > *some target size*
		var postRequest = NetClient.PostAsync(
			requestUri: Uri,
			content: new ByteArrayContent(byteLogs)
		);
		postRequest.Wait();
		return postRequest.Result.StatusCode;
		//TODO: StatusCode Error Handler 
	}


	private void SetUri(string uri) {
		Uri = new Uri(uri);
	}

	public ConnectionManager() { }
}