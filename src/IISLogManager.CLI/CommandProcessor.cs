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
				if ( !settings!.Interactive && settings?.RunMode == RunMode.Target ) {
					if ( settings.SiteNames?.Length > 0 ) {
						AnsiConsole.MarkupLine($"Adding {settings.SiteNames?.Length} sites...");
						string[]? splitSiteNames =
							settings.SiteNames?.Split(',', StringSplitOptions.RemoveEmptyEntries);
						;
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
							sites.AddRange(tsAdd);
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

	public void ProcessLogs(CommandConfiguration config) {
		config.TargetSites?.ForEach(s => {
			
			
			//TODO: stuff
		});
		//TODO: Process Logs
	}


	public static CommandProcessor instance = new CommandProcessor();
}