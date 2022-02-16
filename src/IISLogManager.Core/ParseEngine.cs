using System;
using System.Collections.Generic;
using System.IO;

namespace IISLogManager.Core {
	public class ParseEngine : IDisposable {
		public string FilePath {
			get { return filePath; }
			set {
				filePath = value;
				_mbSize = (int) new FileInfo(filePath).Length / 1024 / 1024;
			}
		}

		public bool MissingRecords { get; private set; } = true;
		public int MaxRecordsToProcess { get; set; } = 1000000;

		public int CurrentFileRecord { get; private set; }

		//private readonly StreamReader _logfile;
		private string[] _headerFields;
		Dictionary<string, string> dataStruct = new();
		protected int _mbSize;
		private string filePath;

		public ParseEngine(string filePath) {
			if ( !File.Exists(filePath) ) {
				throw new Exception($"Could not find File {filePath}");
			}

			FilePath = filePath;
			_mbSize = (int) new FileInfo(filePath).Length / 1024 / 1024;
		}

		public ParseEngine() { }

		public IEnumerable<IISLogObject> ParseLog() {
			if ( _mbSize < 50 ) {
				return QuickProcess();
			} else {
				return LongProcess();
			}
		}

		private IEnumerable<IISLogObject> QuickProcess() {
			IISLogObjectCollection logs = new IISLogObjectCollection();
			var lines = Utils.ReadAllLines(FilePath);
			foreach (string line in lines) {
				ProcessLine(line, logs);
			}

			MissingRecords = false;
			return logs;
		}

		private IEnumerable<IISLogObject> LongProcess() {
			IISLogObjectCollection logs = new IISLogObjectCollection();
			MissingRecords = false;
			using (FileStream fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				using (StreamReader streamReader = new StreamReader(fileStream)) {
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
				sSitename = dataStruct.ContainsKey("s-sitename") ? dataStruct["s-sitename"] : null,
				sComputername = dataStruct.ContainsKey("s-computername") ? dataStruct["s-computername"] : null,
				sIp = dataStruct.ContainsKey("s-ip") ? dataStruct["s-ip"] : null,
				csMethod = dataStruct.ContainsKey("cs-method") ? dataStruct["cs-method"] : null,
				csUriStem = dataStruct.ContainsKey("cs-uri-stem") ? dataStruct["cs-uri-stem"] : null,
				csUriQuery = dataStruct.ContainsKey("cs-uri-query") ? dataStruct["cs-uri-query"] : null,
				csUsername = dataStruct.ContainsKey("cs-username") ? dataStruct["cs-username"] : null,
				cIp = dataStruct.ContainsKey("c-ip") ? dataStruct["c-ip"] : null,
				csVersion = dataStruct.ContainsKey("cs-version") ? dataStruct["cs-version"] : null,
				csUserAgent = dataStruct.ContainsKey("cs(User-Agent)") ? dataStruct["cs(User-Agent)"] : null,
				csCookie = dataStruct.ContainsKey("cs(Cookie)") ? dataStruct["cs(Cookie)"] : null,
				csReferer = dataStruct.ContainsKey("cs(Referer)") ? dataStruct["cs(Referer)"] : null,
				csHost = dataStruct.ContainsKey("cs-host") ? dataStruct["cs-host"] : null,
				xForwardedFor = dataStruct.ContainsKey("X-Forwarded-For") ? dataStruct["X-Forwarded-For"] : null,
				sPort = dataStruct["s-port"] != null ? int.Parse(dataStruct["s-port"]) : (int?) null,
				scStatus = dataStruct["sc-status"] != null
					? int.Parse(dataStruct["sc-status"])
					: (int?) null,
				scSubstatus = dataStruct["sc-substatus"] != null
					? int.Parse(dataStruct["sc-substatus"])
					: (int?) null,
				scWin32Status = dataStruct["sc-win32-status"] != null
					? long.Parse(dataStruct["sc-win32-status"])
					: (long?) null,
				scBytes = dataStruct.ContainsKey("sc-bytes")
					? (dataStruct["sc-bytes"] != null
						? int.Parse(dataStruct["sc-bytes"])
						: (int?) null)
					: null,
				csBytes = dataStruct.ContainsKey("cs-bytes")
					? (dataStruct["cs-bytes"] != null
						? int.Parse(dataStruct["cs-bytes"])
						: (int?) null)
					: null,
				timeTaken = dataStruct["time-taken"] != null
					? int.Parse(dataStruct["time-taken"])
					: (int?) null
			};
		}

		private DateTime GetEventDateTime() {
			DateTime finalDate = DateTime.Parse($"{dataStruct["date"]} {dataStruct["time"]}");
			return finalDate;
		}

		private void FillDataStruct(string[] fieldsData, string[] header) {
			dataStruct.Clear();
			for (int i = 0; i < header.Length; i++) {
				dataStruct.Add(header[i], fieldsData[i] == "-" ? null : fieldsData[i]);
			}
		}

		public void Dispose() { }
	}
}