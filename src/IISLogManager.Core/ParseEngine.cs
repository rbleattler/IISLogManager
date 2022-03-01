namespace IISLogManager.Core {
	public class ParseEngine : IDisposable {
		public string FilePath {
			get { return _filePath; }
			set {
				_filePath = value;
				MbSize = (int) new FileInfo(_filePath).Length / 1024 / 1024;
			}
		}

		public bool MissingRecords { get; private set; } = true;
		public int MaxRecordsToProcess { get; set; } = 1000000;

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
			} else {
				return LongProcess();
			}
		}

		private IEnumerable<IISLogObject> QuickProcess() {
			IISLogObjectCollection logs = new();
			var lines = Utils.ReadAllLines(FilePath);
			foreach (string line in lines) {
				ProcessLine(line, logs);
			}

			MissingRecords = false;
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

			return logs;
		}

		private void ProcessLine(string line, IISLogObjectCollection logs) {
			if ( line.StartsWith("#Fields:") ) {
				_headerFields = line.Replace("#Fields: ", string.Empty).Split(' ');
			}

			if ( !line.StartsWith("#") && _headerFields != null ) {
				string[] fieldsData = line.Split(' ');
				FillDataStruct(fieldsData, _headerFields);
				logs?.Add(NewLogObj());
				CurrentFileRecord++;
			}
		}

		public IISLogObject NewLogObj() {
			return new()  {
				//TODO: How can I make this dynamic?
				LogDateTime = GetEventDateTime(),
				sSitename = _dataStruct.ContainsKey("s-sitename") ? _dataStruct["s-sitename"] : null,
				sComputername = _dataStruct.ContainsKey("s-computername") ? _dataStruct["s-computername"] : null,
				sIp = _dataStruct.ContainsKey("s-ip") ? _dataStruct["s-ip"] : null,
				csMethod = _dataStruct.ContainsKey("cs-method") ? _dataStruct["cs-method"] : null,
				csUriStem = _dataStruct.ContainsKey("cs-uri-stem") ? _dataStruct["cs-uri-stem"] : null,
				csUriQuery = _dataStruct.ContainsKey("cs-uri-query") ? _dataStruct["cs-uri-query"] : null,
				csUsername = _dataStruct.ContainsKey("cs-username") ? _dataStruct["cs-username"] : null,
				cIp = _dataStruct.ContainsKey("c-ip") ? _dataStruct["c-ip"] : null,
				csVersion = _dataStruct.ContainsKey("cs-version") ? _dataStruct["cs-version"] : null,
				csUserAgent = _dataStruct.ContainsKey("cs(User-Agent)") ? _dataStruct["cs(User-Agent)"] : null,
				csCookie = _dataStruct.ContainsKey("cs(Cookie)") ? _dataStruct["cs(Cookie)"] : null,
				csReferer = _dataStruct.ContainsKey("cs(Referer)") ? _dataStruct["cs(Referer)"] : null,
				csHost = _dataStruct.ContainsKey("cs-host") ? _dataStruct["cs-host"] : null,
				xForwardedFor = _dataStruct.ContainsKey("X-Forwarded-For") ? _dataStruct["X-Forwarded-For"] : null,
				sPort = _dataStruct["s-port"] != null ? int.Parse(_dataStruct["s-port"]) : (int?) null,
				scStatus = _dataStruct["sc-status"] != null
					? int.Parse(_dataStruct["sc-status"])
					: (int?) null,
				scSubstatus = _dataStruct["sc-substatus"] != null
					? int.Parse(_dataStruct["sc-substatus"])
					: (int?) null,
				scWin32Status = _dataStruct["sc-win32-status"] != null
					? long.Parse(_dataStruct["sc-win32-status"])
					: (long?) null,
				scBytes = _dataStruct.ContainsKey("sc-bytes")
					? (_dataStruct["sc-bytes"] != null
						? int.Parse(_dataStruct["sc-bytes"])
						: (int?) null)
					: null,
				csBytes = _dataStruct.ContainsKey("cs-bytes")
					? (_dataStruct["cs-bytes"] != null
						? int.Parse(_dataStruct["cs-bytes"])
						: (int?) null)
					: null,
				timeTaken = _dataStruct["time-taken"] != null
					? int.Parse(_dataStruct["time-taken"])
					: (int?) null
			};
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
}