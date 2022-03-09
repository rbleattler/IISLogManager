using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using IISLogManager.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IISLogManager.CLI;

class GetIISLogsCommand : Command<Settings> {
	public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings) {
		if ( context == null ) throw new ArgumentNullException(nameof(context));
		if ( settings == null ) throw new ArgumentNullException(nameof(settings));
		IISController iisController = new();
		SiteObjectCollection? targetSites = new();
		FilterState filter = settings.Filter.As<FilterState>();
		string? fromDate = settings.FromDate;
		string? toDate = settings.ToDate;
		FilterConfiguration filterConfiguration =
			new(
				filter,
				fromDate == null ? DateTime.Today.AddYears(-5) : DateTime.Parse(fromDate),
				// Remove 1 second, to offset to 11:59:59 the day before, and add 1 day to set to end of the target day
				toDate == null ? DateTime.Today : DateTime.Parse(toDate).AddSeconds(-1).AddDays(1)
			);
		RunMode? runMode = settings.Interactive ? null : settings.RunMode;
		OutputMode? outputMode = settings.OutputMode;
		var outputDirectory = settings.OutputDirectory;
		var outputUri = settings.Uri;
		var authMode = settings.AuthMode;
		var authToken = settings.AuthToken;
		var ignoreDefaultWebSite = settings.IgnoreDefaultWebSite;

		List<string> siteChoices = new();
		iisController.GetExtendedSiteList(ignoreDefaultWebSite);
		CommandProcessor.Instance.ProcessTargetSites(ref targetSites, iisController, settings, ref runMode);
		if ( settings is {Interactive: true} || !string.IsNullOrWhiteSpace(settings.GetSites) ) {
			CommandProcessor.Instance.ProcessSiteChoices(iisController, ref siteChoices);
			if ( settings.GetSites?.ToLower() == @"getsites" ) {
				if ( settings.CommandArgument?.ToLower() == @"id" ) {
					return CommandProcessor.Instance.GetSiteIds(ref iisController);
				}

				if ( settings.CommandArgument?.ToLower() == @"logroot" ) {
					return CommandProcessor.Instance.GetSiteLogRoots(ref iisController);
				}

				return CommandProcessor.Instance.GetSites(siteChoices);
			}


			RunPrompts.ExecutePrompts(
				runMode: ref runMode,
				outputMode: ref outputMode,
				outputUri: ref outputUri,
				outputDirectory: ref outputDirectory,
				siteChoices: ref siteChoices,
				iisController: ref iisController,
				targetSites: ref  targetSites,
				filterConfiguration: ref filterConfiguration,
				authMode: ref authMode,
				authToken: ref authToken
			);
		}

		AnsiConsole.MarkupLine($"[DarkOrange]Run mode[/]: {runMode}");
		AnsiConsole.MarkupLine($"[DarkOrange]Output Mode[/] : {outputMode}");
		switch (outputMode) {
			case OutputMode.Local:
				AnsiConsole.MarkupLine($"[DarkOrange]Output Directory[/] : {outputDirectory}");
				break;
			case OutputMode.Remote:
				AnsiConsole.MarkupLine($"[DarkOrange]Output URI[/] : {outputUri}");
				break;
		}

		AnsiConsole.MarkupLine($"[DarkOrange]Target Sites[/] :");
		var i = 0;
		targetSites?.ForEach(ts => {
			i++;
			AnsiConsole.MarkupLine($"[Blue]{i} : {ts.SiteName} ({ts.SiteUrl})[/]");
		});

		if ( settings.Interactive ) {
			var continueProcessing = AnsiConsole.Prompt(RunPrompts.ConfirmContinuePrompt);
			if ( !continueProcessing ) {
				return 1;
			}
		}

		CommandConfiguration commandConfiguration = new(
			iisController: iisController,
			targetSites: targetSites,
			runMode: runMode,
			outputMode: outputMode,
			authMode: authMode,
			authToken: authToken,
			outputDirectory: outputDirectory,
			outputUri: outputUri,
			settings: settings
		);

		AnsiConsole.MarkupLine("[DarkOrange]Beginning Log Processing...[/]");
		CommandProcessor.Instance.ProcessLogs(ref commandConfiguration);
		AnsiConsole.MarkupLine("[DarkOrange]Finished Processing Logs![/]");


		return 0;
	}
}