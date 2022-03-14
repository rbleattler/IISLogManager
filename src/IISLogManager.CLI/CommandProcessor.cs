using IISLogManager.Core;
using System.Text;
using Spectre.Console;

namespace IISLogManager.CLI;

public class CommandProcessor {
	public void ProcessCommand() { }

	public void ProcessTargetSites(ref SiteObjectCollection? targetSites, IISController iisController,
		Settings? settings, ref RunMode? runMode) {
		var sites = targetSites;
		var runmode = runMode;
		AnsiConsole.Status()
			.Start("Adding Sites...", ctx => {
				if ( !settings!.Interactive && settings.RunMode == RunMode.Target ) {
					if ( settings.SiteNames?.Length > 0 ) {
						AnsiConsole.MarkupLine($"Adding {settings.SiteNames?.Length} sites...");
						string[]? splitSiteNames =
							settings.SiteNames?.Split(',', StringSplitOptions.RemoveEmptyEntries);
						foreach (string siteName in splitSiteNames!) {
							var tsAdd = iisController.Sites.Where(
								sO => sO.SiteName == siteName.Trim()
							);
							sites?.AddRange(tsAdd);
						}
					}

					if ( settings.SiteUrls?.Length > 0 ) {
						AnsiConsole.MarkupLine($"Adding {settings.SiteUrls?.Length} sites...");
						foreach (string siteUrl in
						         settings.SiteUrls?.Split(',', StringSplitOptions.RemoveEmptyEntries)!) {
							var tsAdd = iisController.Sites.Where(sO => sO.SiteUrl == siteUrl.Trim());
							sites?.AddRange(tsAdd);
						}
					}
				}

				if ( runmode == RunMode.All ) {
					sites?.AddRange(iisController.Sites);
				}
			});
		targetSites = sites;
		runMode = runmode;
	}

	public void ProcessSiteChoices(IISController iisController, ref List<string> siteChoices) {
		foreach (SiteObject site in iisController.Sites) {
			var siteString = string.Format("{0}\t({1})", site.SiteName, site.SiteUrl);
			siteChoices.Add(siteString);
		}
	}

	public int GetSites(List<string> siteChoices) {
		AnsiConsole.MarkupLine("[DarkOrange]Site Name[/]\t([Blue]Site Url[/])");
		siteChoices.ForEach((s) => { AnsiConsole.MarkupLine($"{s}"); });
		return 0;
	}

	public int GetSiteIds(ref IISController iiSController) {
		AnsiConsole.MarkupLine("[DarkOrange]Site Ids[/]");
		iiSController.Sites.ForEach(s => { AnsiConsole.MarkupLine($"{s.Id}"); });
		return 0;
	}

	//TODO: Ensure there *ARE* logs to process before beginning processing... 
	public void ProcessLogs(ref CommandConfiguration config) {
		//TODO: Add verbose output
		// AnsiConsole.MarkupLine("[[DEBUG]]  Checking if TargetSites is null...");
		if ( config.TargetSites != null ) {
			// AnsiConsole.MarkupLine("[[DEBUG]] TargetSites is not null...");
			if ( config.Settings is {Filter: true} ) {
				int pathCount = 0;
#if DEBUG
				config.TargetSites.ForEach(p => pathCount += p.LogFilePaths.Count);
				AnsiConsole.MarkupLine($"[[DEBUG]] Filtering enabled...");
				AnsiConsole.MarkupLine($"[[DEBUG]] FromDate : {config.Settings.FromDate} ...");
				AnsiConsole.MarkupLine($"[[DEBUG]] ToDate : {config.Settings.ToDate} ...");
				AnsiConsole.MarkupLine($"[[DEBUG]] Filtering Files... ( Count : {pathCount})");
				pathCount = 0;
#endif
				config.TargetSites.FilterAllLogFiles(
					DateTime.Parse(config.Settings.FromDate),
					DateTime.Parse(config.Settings.ToDate)
				);
				config.TargetSites.ForEach(p => pathCount += p.LogFilePaths.Count);
				AnsiConsole.MarkupLine(
					$"[[DEBUG]] Finished Filtering Files... (New Count : {pathCount})");
			}

			foreach (var site in config.TargetSites) {
				AnsiConsole.MarkupLine($"[[DEBUG]] Processing {site.SiteName}...");
				site.ParseAllLogs();
				if ( site.Logs.Count <= 0 ) {
					AnsiConsole.MarkupLine($"[DarkOrange]{site.SiteName}[/] has no logs from the target date range...");
					continue;
				}

				if ( config.OutputMode == OutputMode.Local ) {
					AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Local...");
					var outFile = site.GetLogFileName(config.OutputDirectory);
					site.Logs.TrimExcess();
					site.Logs.WriteToFile(outFile);
					site.Logs.Clear();
					site.Logs.Dispose();
					AnsiConsole.MarkupLine($"[DarkOrange]Output File :[/] {outFile}");
				}

				if ( config.OutputMode == OutputMode.Remote ) {
					AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Remote...");
					if ( string.IsNullOrWhiteSpace(config.OutputUri) ) {
						throw new UriNotSpecifiedException();
					}

					if ( config.OutputUri != null ) {
						ConnectionManager connectionManager = new();
						connectionManager.SetConnection(config.OutputUri);
						if ( config.AuthMode == AuthMode.BearerToken ) {
							if ( config.AuthToken != null ) connectionManager.BearerToken = config.AuthToken;
							connectionManager.SetConnection(
								config.OutputUri,
								connectionManager.BearerToken ?? throw new NullAuthTokenException()
							);
						}

						var response = connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName);
						// var response = connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName,
						// 	true);
						AnsiConsole.MarkupLine($"[DarkOrange]Server Response :[/]{response}");
					}


					//TODO: Process Logs for remote output
				}

				site.Dispose();
			}
		}
	}


	public static CommandProcessor Instance = new();
}