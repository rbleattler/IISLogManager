#nullable enable
using System.Net;
using System.Text;

namespace IISLogManager.Core;

public class ConnectionManager {
	// private static HttpClientHandler _netClientHandler = new() {
	// 	//TODO: Add support for sending with non-default credentials
	// 	UseDefaultCredentials = true
	// };

	// private HttpClient _netClient = new(_netClientHandler);
	private readonly HttpClientHandler _clientHandler = new();

	private  HttpClient _netClient;
	public Uri? Uri { get; set; }
	public string? UriString { get; private set; }

	public int LogChunkSize { get; set; } = 25000;

	public string? BearerToken { get; set; }
	private readonly Dictionary<Guid, HttpStatusCode> _responseCodes = new();

	public void SetConnection(string uri, string? authToken = null, int timeOut = 30,
		NetworkCredential? credential = null, bool? useDefaultCredential = true) {
		if ( null != credential ) {
			_clientHandler.Credentials = credential;
		}

		if ( true == useDefaultCredential ) {
			_clientHandler.UseDefaultCredentials = true;
		}

		_netClient = new HttpClient(_clientHandler);
		//TODO: Set other defaults?
		SetUri(uri);
		if ( !string.IsNullOrWhiteSpace(authToken) && null != authToken ) {
			SetAuthToken(authToken);
		}

		_netClient
			.DefaultRequestHeaders
			.Accept
			.Add(new("application/json"));
		_netClient.MaxResponseContentBufferSize = 2147483647;

		if ( null != BearerToken ) {
			_netClient
				.DefaultRequestHeaders
				.Add("Authorization", "Bearer " + BearerToken);
		}

		_netClient.Timeout = TimeSpan.FromSeconds(timeOut);
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

	async Task<Guid> SubmitLogs(
		IISLogObjectCollection logs,
		string? siteUrl = null,
		string? siteName = null,
		string? hostName = null
	) {
		var jLogs = logs.ToJson(siteUrl, siteName, hostName);
		var compressedLogs = Utils.CompressString(jLogs);
		var requestContent = $"{{\"RawContent\" : \"{compressedLogs}\"}}";
		Guid taskGuid = Guid.NewGuid();
		var httpResponse =
			await SendRequestAsync(Uri?.ToString() ?? throw new InvalidOperationException(), requestContent);
		_responseCodes.Add(taskGuid, httpResponse.StatusCode);
		return taskGuid;
	}

	private HttpStatusCode ChunkSubmit(
		IISLogObjectCollection logs,
		string? siteUrl = null,
		string? siteName = null,
		string? hostName = null
	) {
		var startIndex = 0;
		var reqCount = 0;

		while (startIndex < logs.Count()) {
			var logCount = logs.Count() - startIndex;
			if ( logs.Count() - startIndex > LogChunkSize ) {
				logCount = LogChunkSize;
			}

			IISLogObjectCollection logChunk = new(logs.GetRange(startIndex, logCount));
			SubmitLogs(logChunk, siteUrl, siteName, hostName).Wait();
			startIndex += logCount;
			reqCount++;
		}

		WaitResponseTimer(_netClient.Timeout.Milliseconds, reqCount);

		if ( _responseCodes.ContainsValue(HttpStatusCode.Unauthorized) ) {
			throw new HttpRequestException("One or more Requests returned : 401 Not Authorized");
		}

		return HttpStatusCode.OK;
	}

	public HttpStatusCode AddLogs(
		IISLogObjectCollection logs,
		string? siteUrl = null,
		string? siteName = null,
		string? hostName = null,
		bool? chunkIfHeavy = true
	) {
		if ( true != chunkIfHeavy || logs.Count() < LogChunkSize ) {
			Console.WriteLine("[DEBUG] ChunkIfHeavy Disabled!");
			SubmitLogs(logs, siteUrl, siteName, hostName).Wait();
			WaitResponseTimer(_netClient.Timeout.Milliseconds, 1);
		}

		Console.WriteLine("[DEBUG] ChunkIfHeavy Enabled!");
		return ChunkSubmit(logs, siteUrl, siteName, hostName);
	}

	private void WaitResponseTimer(int milliseconds, int requestCount) {
		var timeOutTimer = 250;
		while (_responseCodes.Count < requestCount) {
			Thread.Sleep(250);
			timeOutTimer += 250;
			if ( timeOutTimer > milliseconds ) {
				throw new HttpRequestException(
					$"Http requests failed due to timeout ({_netClient.Timeout.Seconds} seconds)");
			}
		}
	}

	private void SetUri(string uri) {
		Uri = new(uri);
		UriString = Uri.ToString();
	}

	private void SetAuthToken(string authToken) {
		BearerToken = authToken;
	}

	public ConnectionManager() { }

	public ConnectionManager(string uri) {
		SetConnection(uri);
	}

	public ConnectionManager(string uri, string? authToken, int timeOut = 30) {
		SetConnection(uri, authToken, timeOut);
	}

	private async Task<HttpResponseMessage> SendRequestAsync(string adaptiveUri, string resquestContent) {
		var httpClient = _netClient;
		// StringContent httpContent = new StringContent(resquestContent, Encoding.UTF8);
		StringContent httpContent = new(resquestContent);

		HttpResponseMessage responseMessage = null!;
		try {
			responseMessage = await httpClient.PostAsync(adaptiveUri, httpContent);
		} catch (Exception ex) {
			if ( responseMessage == null ) {
				responseMessage = new();
			}

			responseMessage.StatusCode = HttpStatusCode.InternalServerError;
			var reason = ex.Message ?? "No Reason Provided";
			responseMessage.ReasonPhrase = $"RestHttpClient.SendRequest failed: {reason}";
		}

		return responseMessage;
	}
}