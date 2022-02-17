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
			new FilterConfiguration(
				filterState: filter,
				DateTime.Parse(fromDate ?? string.Empty),
				DateTime.Parse(toDate ?? string.Empty)
			);
		RunMode? runMode = settings.Interactive ? null : settings.RunMode;
		OutputMode? outputMode = settings.OutputMode;
		var outputDirectory = settings.OutputDirectory;
		var outputUri = settings.Uri;

		List<string> siteChoices = new();
		iisController.GetExtendedSiteList();
		CommandProcessor.instance.ProcessTargetSites(ref targetSites, iisController, settings, ref runMode);
		if ( settings is {Interactive: true} || !string.IsNullOrWhiteSpace(settings.GetSites) ) {
			CommandProcessor.instance.ProcessSiteChoices(iisController, ref siteChoices);
			if ( settings.GetSites?.ToLower() == @"getsites" ) {
				CommandProcessor.instance.GetSites(siteChoices);
			}

			RunPrompts.ExecutePrompts(ref runMode, ref outputMode, ref outputUri,
				ref outputDirectory, ref siteChoices, ref iisController,
				ref  targetSites, ref filterConfiguration);
		}

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

		if ( settings.Interactive ) {
			var continueProcessing = AnsiConsole.Prompt(RunPrompts.ConfirmContinuePrompt);
			if ( !continueProcessing ) {
				return 1;
			}
		}

		CommandConfiguration commandConfiguration = new(
			iisController,
			targetSites,
			runMode,
			outputMode,
			outputDirectory,
			outputUri,
			settings
		);

		//region: Connect and Send for Network Connection

		//endregion


		return 0;
	}

	public FilterConfiguration FilterConfiguration { get; set; }
}