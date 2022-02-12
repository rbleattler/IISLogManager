using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace IISLogManager.Core {
    public class ParseEngine : IDisposable {
        public string FilePath {
            get { return filePath; }
            set {
                filePath = value;
                _mbSize = (int)new FileInfo(filePath).Length / 1024 / 1024;
            }
        }
        public bool MissingRecords { get; private set; } = true;
        public int MaxRecordsToProcess { get; set; } = 1000000;
        public int CurrentFileRecord { get; private set; }
        //private readonly StreamReader _logfile;
        private string[] _headerFields;
        Hashtable dataStruct = new();
        protected int _mbSize;
        private string filePath;

        public ParseEngine(string filePath) {
            if (File.Exists(filePath)) {
                FilePath = filePath;
                _mbSize = (int)new FileInfo(filePath).Length / 1024 / 1024;
            } else {
                throw new Exception($"Could not find File {filePath}");
            }
        }
        public ParseEngine() {
        }

        public IEnumerable<IISLogObject> ParseLog() {
            if (_mbSize < 50) {
                return QuickProcess();
            } else {
                return LongProcess();
            }
        }

        private IEnumerable<IISLogObject> QuickProcess() {
            List<IISLogObject> events = new List<IISLogObject>();
            var lines = Utils.ReadAllLines(FilePath);
            foreach (string line in lines) {
                ProcessLine(line, events);
            }
            MissingRecords = false;
            return events;
        }

        private IEnumerable<IISLogObject> LongProcess() {
            List<IISLogObject> events = new List<IISLogObject>();
            MissingRecords = false;
            using (FileStream fileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (StreamReader streamReader = new StreamReader(fileStream)) {
                    while (streamReader.Peek() > -1) {
                        ProcessLine(streamReader.ReadLine(), events);
                        if (events?.Count > 0 && events?.Count % MaxRecordsToProcess == 0) {
                            MissingRecords = true;
                            break;
                        }
                    }
                }
            }
            return events;
        }

        private void ProcessLine(string line, List<IISLogObject> events) {
            if (line.StartsWith("#Fields:")) {
                _headerFields = line.Replace("#Fields: ", string.Empty).Split(' ');
            }
            if (!line.StartsWith("#") && _headerFields != null) {
                string[] fieldsData = line.Split(' ');
                FillDataStruct(fieldsData, _headerFields);
                events?.Add(NewLogObj());
                CurrentFileRecord++;
            }
        }

        private IISLogObject NewLogObj() {
            return new IISLogObject {
                LogDateTime = GetEventDateTime(),
                sSitename = dataStruct["s-sitename"]?.ToString(),
                sComputername = dataStruct["s-computername"]?.ToString(),
                sIp = dataStruct["s-ip"]?.ToString(),
                csMethod = dataStruct["cs-method"]?.ToString(),
                csUriStem = dataStruct["cs-uri-stem"]?.ToString(),
                csUriQuery = dataStruct["cs-uri-query"]?.ToString(),
                sPort = dataStruct["s-port"] != null ? int.Parse(dataStruct["s-port"]?.ToString()) : (int?)null,
                csUsername = dataStruct["cs-username"]?.ToString(),
                cIp = dataStruct["c-ip"]?.ToString(),
                csVersion = dataStruct["cs-version"]?.ToString(),
                csUserAgent = dataStruct["cs(User-Agent)"]?.ToString(),
                csCookie = dataStruct["cs(Cookie)"]?.ToString(),
                csReferer = dataStruct["cs(Referer)"]?.ToString(),
                csHost = dataStruct["cs-host"]?.ToString(),
                scStatus = dataStruct["sc-status"] != null ? int.Parse(dataStruct["sc-status"]?.ToString()) : (int?)null,
                scSubstatus = dataStruct["sc-substatus"] != null ? int.Parse(dataStruct["sc-substatus"]?.ToString()) : (int?)null,
                scWin32Status = dataStruct["sc-win32-status"] != null ? long.Parse(dataStruct["sc-win32-status"]?.ToString()) : (long?)null,
                scBytes = dataStruct["sc-bytes"] != null ? int.Parse(dataStruct["sc-bytes"]?.ToString()) : (int?)null,
                csBytes = dataStruct["cs-bytes"] != null ? int.Parse(dataStruct["cs-bytes"]?.ToString()) : (int?)null,
                timeTaken = dataStruct["time-taken"] != null ? int.Parse(dataStruct["time-taken"]?.ToString()) : (int?)null
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
