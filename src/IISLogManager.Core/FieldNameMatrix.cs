namespace IISLogManager.Core;

public readonly struct FieldNameMatrix {
	public readonly string Date = "date";
	public readonly string Time = "time";
	public readonly string ServerIp = "s-ip";
	public readonly string Method = "cs-method";
	public readonly string UriStem = "cs-uri-stem";
	public readonly string UriQuery = "cs-uri-query";
	public readonly string ServerPort = "s-port";
	public readonly string UserName = "cs-username";
	public readonly string ClientIp = "c-ip";
	public readonly string UserAgent = "cs(User-Agent)";
	public readonly string Referrer = "cs(Referer)";
	public readonly string HttpStatus = "sc-status";
	public readonly string Cookie = "cs(Cookie)";
	public readonly string ProtocolSubstatus = "sc-substatus";
	public readonly string Win32Status = "sc-win32-status";
	public readonly string TimeTaken = "time-taken";
	public readonly string ForwardedFor = "X-Forwarded-For";
	public static readonly FieldNameMatrix Instance = new FieldNameMatrix();

	public string GetProperty(string propertyName) {
		return typeof(FieldNameMatrix)?.GetProperty(propertyName)?.GetValue(this).ToString();
	}
}