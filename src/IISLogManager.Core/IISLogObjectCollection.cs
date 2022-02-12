using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Concurrent;

namespace IISLogManager.Core {
    public class IISLogObjectCollection : List<IISLogObject>, IComparable<IISLogObjectCollection>, IComparable {
        public IISLogObjectCollection() { }
        public IISLogObjectCollection(IEnumerable<IISLogObject> collection) : base(collection) { }
        public IISLogObjectCollection(int capacity) : base(capacity) { }

        public int CompareTo(IISLogObjectCollection other) {
            throw new System.NotImplementedException();
        }

        public int CompareTo(object obj) {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is IISLogObjectCollection other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(IISLogObjectCollection)}");
        }

        //TODO: https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentbag-1?view=net-6.0

        public void OutFile(string filePath) {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var jsonLogs = ToStringCollection();
            StreamWriter streamWriter = new(File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite));
            streamWriter.AutoFlush = true;
            jsonLogs.ForEach(l => streamWriter.WriteLine(l));
            stopwatch.Stop();
            Console.WriteLine($"processed {jsonLogs.Count} logs in {stopwatch.Elapsed}");
        }

        public List<string> ToStringCollection() {
            var list = new List<string>();
            if (!this.Any()) return null;
            foreach (var site in this) {
                list.Add(site.ToJson());
            }
            return list;
        }

        /// <summary>
        /// This is really slow... 
        /// </summary>
        /// <param name="streamWriter"></param>
        /// <param name="content"></param>
        /// <param name="iterator"></param>
        /// <param name="originalCount"></param>
        /// <param name="list"></param>
        private void verboseWriteLine(ref StreamWriter streamWriter, string content, ref int iterator, int originalCount, ref List<string> list) {
            Console.Write($"Processing {iterator} of {originalCount}...\r");
            streamWriter.WriteLine(content);
            streamWriter.Flush();
            list.Remove(content);
            iterator++;
        }


        // I know there is a better way to do this... I just don't have the bandwidth at the moment... 
        public string ToJson() {
            if (!this.Any()) return null;
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("[");
            foreach (var site in this) {
                var json = site.ToJson();
                stringBuilder.AppendFormat("{0},", json)
                    .AppendLine();
            }
            stringBuilder.AppendLine("]");
            stringBuilder.AppendLine("}");
            return stringBuilder.ToString();
        }
    }
}