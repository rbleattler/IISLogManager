using System.Diagnostics;
using System.Text;
using IISLogManager.Core;
using Microsoft.Web.Administration;
using System.Threading.Tasks;
using Sharprompt;
using System.Collections.Concurrent;
using FluentAssertions.Common;
using IISLogManager.Cli;

namespace IISLogManager.Cli {

    static class Program {

        private static IISController IISController = new();
        private static readonly ServerManager ServerManager = new IISController().ServerManager;
        private static readonly SiteObjectFactory ObjectFactory = new();
        //TODO: Client FileWriter

        static void Main(string[] args) {
            var siteNameList = new List<string>();
            IEnumerable<string>? sites;
            string? exportLocation;
            foreach (Site site in ServerManager.Sites) {
                siteNameList.Add(site.Name);
            }

            Debug.WriteLine("IIS website count : " + siteNameList.Count);
            sites = RunModeSelectPrompt(siteNameList).ToList();

            exportLocation = RunExportLocationPrompt();


            int siteCount = sites.Count();
            Debug.WriteLine($"Converting {siteCount} sites from <Site> to <SiteObject>...");
            foreach (var site in sites) {
                var websites = ServerManager.Sites.Where(s => s.Name == site);
                var siteObject = ObjectFactory.BuildSite(websites.First());
                IISController.Sites.Add(siteObject);
            }
            Debug.WriteLine($"Finished converting {siteCount} sites from <Site> to <SiteObject>...");
            // Prompt: Choose output directory
            //exportLocation = RunExportLocationPrompt();
            Directory.CreateDirectory(exportLocation);
            foreach (var site in IISController.Sites) {
                Console.WriteLine("Parsing Logs for " + site.SiteName + "...");
                //TODO: SiteObjectCollection
                site.ParseAllLogs();
                GC.Collect();
                Console.WriteLine($"Finished parsing {site.Logs.Count} logs for {site.SiteName}!");
            }
            GC.Collect();
            experimentalWriteJsonLogsToFile(exportLocation);
        }

        static void WriteJsonLogsToFile(string exportLocation) {
            string consoleOut;
            // TODO: Parallelize site processing, but not logs (for now)
            foreach (var site in IISController.Sites) {
                List<string> jsonLogs = new();
                var safeSiteName = Utils.MakeSafeFilename(site.SiteName, '-');
                var siteFile = string.Join("\\", exportLocation, safeSiteName) ??
                               throw new ArgumentNullException("args");
                var activeSiteFile = File.CreateText(siteFile);
                int i = 0;
                int x = site.Logs.Count;
                foreach (var log in site.Logs.ToList()) {
                    jsonLogs.Add(log.ToJson());
                    site.Logs.Remove(log);
                    consoleOut = $"Processing {i}\\{x - 1}...\r";
                    Console.Write(consoleOut);
                    i++;
                }
                Console.WriteLine($"Finished Processing {x} Logs!");
                //File.WriteAllLinesAsync(siteFile, jsonLogs).Wait();
                GC.Collect();
            }
            GC.Collect();
        }
        static void experimentalWriteJsonLogsToFile(string exportLocation) {
            // TODO: Parallelize site processing, but not logs (for now)
            foreach (var site in IISController.Sites) {
                var safeSiteName = Utils.MakeSafeFilename(site.SiteName, '-');
                var siteFile = string.Join("\\", exportLocation, safeSiteName) ??
                               throw new ArgumentNullException("args");
                var activeSiteFile = File.CreateText(siteFile);
                Console.WriteLine($"Processing {site.Logs.Count} Logs!");
                site.Logs.OutFile(siteFile);
                Console.WriteLine($"Finished Processing {site.Logs.Count} Logs!");
            }
            GC.Collect();
        }

        private static string RunExportLocationPrompt() {
            return Prompt.Input<string>(message: "Where do you want to save the output? (Directory)",
            $"{Environment.GetEnvironmentVariable("USERPROFILE")}\\IISLogManager_{DateTime.Now:yyMMdd}"
);
        }

        private static IEnumerable<string> RunModeSelectPrompt(List<string> siteNameList) {
            var mode = Prompt.Select("Do you want to get all sites, or specific sites?", new[] { "All", "Specific" });
            Console.WriteLine($"You selected {mode}");
            if (mode != "Specific") {
                return siteNameList;
            } else {
                return RunSiteSelectPrompt(siteNameList);
            }
        }

        private static IEnumerable<string> RunSiteSelectPrompt(List<string> siteNameList) {
            var selectedSites = Prompt.MultiSelect("Choose which sites to work with: ", siteNameList, pageSize: 5)
                .ToList();
            Console.WriteLine($"You picked {string.Join(", ", selectedSites)}");
            return selectedSites;
        }

        public static Task ProcessWrite(string filePath, string text) {
            return WriteTextAsync(filePath, text);
        }

        /// <summary>
        /// Borrowed from https://stackoverflow.com/questions/11774827/writing-to-a-file-asynchronously
        /// </summary>
        /// <param name="filePath"> Path to the file which is to be appended</param>
        /// <param name="text"> Text to be appended</param>
        /// <returns></returns>
        static async Task WriteTextAsync(string filePath, string text) {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true)) {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }
    }

    enum Modes {
        All,
        Specific
    }
}