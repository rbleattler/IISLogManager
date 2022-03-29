using Newtonsoft.Json;

namespace IISLogManager.Core;

public class ParseEngine : IDisposable {
	public string FilePath {
		get { return _filePath; }
		set {
			_filePath = value;
			MbSize = (int) new FileInfo(_filePath).Length / 1024 / 1024;
		}
	}

	public bool MissingRecords { get; private set; } = true;
	public int MaxRecordsToProcess { get; set; } = 100000000;

	public int CurrentFileRecord { get; private set; }

	//private readonly StreamReader _logfile;
	private string[] _headerFields;
	Dictionary<string, string> _dataStruct = new();
	protected int MbSize;
	private string _filePath;

	public ParseEngine(string filePath) {
		if ( !File.Exists(filePath) ) {
			throw new($"Could not find File {filePath}");
		}

		FilePath = filePath;
		MbSize = (int) new FileInfo(filePath).Length / 1024 / 1024;
	}

	public ParseEngine() { }

	public IEnumerable<IISLogObject> ParseLog() {
		if ( MbSize < 50 ) {
			return QuickProcess();
		}

		return LongProcess();
	}

	internal void ParsePartialLog(int startLine, int count, ref IISLogObjectCollection logs, string headerLine) {
		PartialProcess(startLine, count, ref logs, headerLine);
	}

	private IEnumerable<IISLogObject> QuickProcess() {
		IISLogObjectCollection logs = new();
		var lines = Utils.ReadAllLines(FilePath);
		foreach (string line in lines) {
			ProcessLine(line, logs);
		}

		MissingRecords = false;
		logs.TrimExcess();
		return logs;
	}

	private IEnumerable<IISLogObject> LongProcess() {
		IISLogObjectCollection logs = new();
		MissingRecords = false;
		using (FileStream fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
			using (StreamReader streamReader = new(fileStream)) {
				while (streamReader.Peek() > -1) {
					ProcessLine(streamReader.ReadLine(), logs);
					if ( logs?.Count > 0 && logs?.Count % MaxRecordsToProcess == 0 ) {
						MissingRecords = true;
						break;
					}
				}
			}
		}

		logs.TrimExcess();
		return logs;
	}

	private void PartialProcess(int startLine, int processCount, ref IISLogObjectCollection logs, string headerLine) {
		var skipCount = startLine - 1;
		var lines = File
			.ReadLines(FilePath)
			.Skip(skipCount)
			.Take(processCount);
		foreach (var line in lines) {
			ProcessLine(headerLine, line, logs);
		}
	}

	private void ProcessLine(string headerLine, string line, IISLogObjectCollection logs) {
		_headerFields = headerLine
			.Replace("#Fields: ", string.Empty)
			.Split(' ');

		if ( !line.StartsWith("#") && _headerFields != null ) {
			string[] fieldsData = line.Split(' ');
			FillDataStruct(fieldsData, _headerFields);
			logs?.Add(NewLogObj());
			CurrentFileRecord++;
		}
	}

	private void ProcessLine(string line, IISLogObjectCollection logs) {
		if ( line.StartsWith("#Fields:") ) {
			_headerFields = line
				.Replace("#Fields: ", string.Empty)
				.Split(' ');
		}

		if ( !line.StartsWith("#") && _headerFields != null ) {
			string[] fieldsData = line.Split(' ');
			FillDataStruct(fieldsData, _headerFields);
			logs?.Add(NewLogObj());
			CurrentFileRecord++;
		}
	}

	private IISLogObject NewLogObj() {
		return new()  {
			//TODO: How can I make this dynamic?
			DateTime = GetEventDateTime(),
			SiteName = _dataStruct.ContainsKey("s-sitename") ? _dataStruct["s-sitename"] : null,
			ComputerName = _dataStruct.ContainsKey("s-computername") ? _dataStruct["s-computername"] : null,
			ServerIp = _dataStruct.ContainsKey("s-ip") ? _dataStruct["s-ip"] : null,
			Method = _dataStruct.ContainsKey("cs-method") ? _dataStruct["cs-method"] : null,
			UriStem = _dataStruct.ContainsKey("cs-uri-stem") ? _dataStruct["cs-uri-stem"] : null,
			UriQuery = _dataStruct.ContainsKey("cs-uri-query") ? _dataStruct["cs-uri-query"] : null,
			Username = _dataStruct.ContainsKey("cs-username") ? _dataStruct["cs-username"] : null,
			ClientIp = _dataStruct.ContainsKey("c-ip") ? _dataStruct["c-ip"] : null,
			Version = _dataStruct.ContainsKey("cs-version") ? _dataStruct["cs-version"] : null,
			UserAgent = _dataStruct.ContainsKey("cs(User-Agent)") ? _dataStruct["cs(User-Agent)"] : null,
			Cookie = _dataStruct.ContainsKey("cs(Cookie)") ? _dataStruct["cs(Cookie)"] : null,
			Referer = _dataStruct.ContainsKey("cs(Referer)") ? _dataStruct["cs(Referer)"] : null,
			HostName = _dataStruct.ContainsKey("cs-host") ? _dataStruct["cs-host"] : null,
			ForwardedFor = _dataStruct.ContainsKey("X-Forwarded-For") ? _dataStruct["X-Forwarded-For"] : null,
			ServerPort = _dataStruct["s-port"] != null ? int.Parse(_dataStruct["s-port"]) : (int?) null,
			HttpStatus = _dataStruct["sc-status"] != null
				? int.Parse(_dataStruct["sc-status"])
				: (int?) null,
			ProtocolSubstatus = _dataStruct["sc-substatus"] != null
				? int.Parse(_dataStruct["sc-substatus"])
				: (int?) null,
			Win32Status = _dataStruct["sc-win32-status"] != null
				? long.Parse(_dataStruct["sc-win32-status"])
				: (long?) null,
			ServerClientBytes = _dataStruct.ContainsKey("sc-bytes")
				? (_dataStruct["sc-bytes"] != null
					? int.Parse(_dataStruct["sc-bytes"])
					: (int?) null)
				: null,
			ClientServerBytes = _dataStruct.ContainsKey("cs-bytes")
				? (_dataStruct["cs-bytes"] != null
					? int.Parse(_dataStruct["cs-bytes"])
					: (int?) null)
				: null,
			TimeTaken = _dataStruct["time-taken"] != null
				? int.Parse(_dataStruct["time-taken"])
				: (int?) null
		};
	}

	public IISLogObject NewLogObj(bool asJson = true) {
		if ( false == asJson ) {
			return NewLogObj();
		}

		return new IISLogObject(JsonConvert.SerializeObject(_dataStruct));
	}

	private DateTime GetEventDateTime() {
		DateTime finalDate = DateTime.Parse($"{_dataStruct["date"]} {_dataStruct["time"]}");
		return finalDate;
	}

	private void FillDataStruct(string[] fieldsData, string[] header) {
		_dataStruct.Clear();
		for (int i = 0; i < header.Length; i++) {
			_dataStruct.Add(header[i], fieldsData[i] == "-" ? null : fieldsData[i]);
		}
	}

	public void Dispose() { }
}