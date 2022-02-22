using IISLogManager.Core;
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

	//TODO: Ensure there *ARE* logs to process before beginning processing... 
	public void ProcessLogs(ref CommandConfiguration config) {
		//TODO: Add verbose output
		// AnsiConsole.MarkupLine("[[DEBUG]]  Checking if TargetSites is null...");
		if ( config.TargetSites != null ) {
			// AnsiConsole.MarkupLine("[[DEBUG]] TargetSites is not null...");
			foreach (var site in config.TargetSites) {
				AnsiConsole.MarkupLine($"[[DEBUG]] Processing {site.SiteName}...");
				site.ParseAllLogs();
				if ( site.Logs.Count <= 0 ) {
					AnsiConsole.MarkupLine($"[DarkOrange]{site.SiteName}[/] has no logs from the target date range...");
					break;
				}

				if ( config.Settings != null && config.Settings.Filter ) {
					AnsiConsole.MarkupLine($"[[DEBUG]] Filtering enabled...");

					site.Logs.FilterLogs(
						DateTime.Parse(config.Settings.FromDate!),
						DateTime.Parse(config.Settings.ToDate!)
					);
				}

				if ( config.OutputMode == OutputMode.Local ) {
					AnsiConsole.MarkupLine($"[[DEBUG]] Output mode Local...");
					var outFile = site.GetLogFileName(config.OutputDirectory);
					site.Logs.WriteToFile(outFile);
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
							if ( config.AuthToken != null )
								connectionManager.SetConnection(config.OutputUri, config.AuthToken);
						}

						//TODO: Chunk processing
						//TODO: Is returning 401/400, need to work on this
						var response = connectionManager.AddLogs(site.Logs, site.SiteUrl, site.SiteName, site.HostName);
						AnsiConsole.MarkupLine($"[DarkOrange]Server Response :[/]{response}");
					}


					//TODO: Process Logs for remote output
				}
			}
		}
	}


	public static CommandProcessor Instance = new();
}