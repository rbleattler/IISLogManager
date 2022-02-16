using System.Diagnostics.CodeAnalysis;
using IISLogManager.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IISLogManager.CLI;

class GetIISLogsCommand : Command<Settings?> {
	public override int Execute([NotNull] CommandContext context, Settings? settings) {
		if ( settings == null ) throw new ArgumentNullException(nameof(settings));
		IISController iisController = new();
		SiteObjectCollection? targetSites = new();
		RunMode? runMode = settings != null && settings.Interactive ? null : settings?.RunMode;
		var outputMode = settings?.OutputMode;
		var outputDirectory = settings?.OutputDirectory;
		var outputUri = settings?.Uri;

		List<string> siteChoices = new();
		iisController.GetExtendedSiteList();
		CommandProcessor.instance.ProcessTargetSites(ref targetSites, iisController, settings, ref runMode);
		if ( settings is {Interactive: true} || !string.IsNullOrWhiteSpace(settings?.GetSites) ) {
			CommandProcessor.instance.ProcessSiteChoices(iisController, ref siteChoices);
			if ( settings.GetSites?.ToLower() == @"getsites" ) {
				CommandProcessor.instance.GetSites(iisController, siteChoices, settings);
			}

			RunPrompts.ExecutePrompts(ref runMode, ref outputMode, ref outputUri,
				ref outputDirectory, ref siteChoices, ref iisController,
				ref  targetSites);
		}

		// if ( runMode == RunMode.All ) {
		// 	targetSites?.AddRange(iisController.Sites);
		// }

		AnsiConsole.MarkupLine($"[DarkOrange]Run mode[/]: {runMode}");
		AnsiConsole.MarkupLine($"[DarkOrange]Output Mode[/] : {outputMode}");
		if ( outputMode == OutputMode.Local ) {
			AnsiConsole.MarkupLine($"[DarkOrange]Output Directory[/] : {outputDirectory}");
		}

		if ( outputMode == OutputMode.Remote ) {
			AnsiConsole.MarkupLine($"[DarkOrange]Output URI[/] : {outputUri}");
		}

		AnsiConsole.MarkupLine($"[DarkOrange]Target Sites[/] :");
		int i = 0;
		targetSites?.ForEach(ts => {
			i++;
			AnsiConsole.MarkupLine($"[Blue]{i} : {ts.SiteName} ({ts.SiteUrl})[/]");
		});

		if ( settings != null && settings.Interactive ) {
			var continueProcessing = AnsiConsole.Prompt(RunPrompts.ConfirmContinuePrompt);
			if ( !continueProcessing ) {
				return 1;
			}
		}


		return 0;
	}
}