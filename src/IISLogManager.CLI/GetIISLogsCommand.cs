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
		if ( !string.IsNullOrWhiteSpace(settings.ConfigurationFile) ) {
			settings.ImportConfiguration(settings.ConfigurationFile); // TODO: Add Yaml Support
		}

		IISController iisController = new();
		SiteObjectCollection? targetSites = new();
		FilterState filter = settings.Filter.As<FilterState>();
		string? fromDate = settings.FromDate;
		string? toDate = settings.ToDate;
		FilterConfiguration filterConfiguration =
			new(
				filter,
				fromDate == null ? DateTime.Today.AddYears(-5) : DateTime.Parse(fromDate),
				toDate == null ? DateTime.Today : DateTime.Parse(toDate)
			);
		RunMode? runMode = settings.Interactive ? null : settings.RunMode;
		OutputMode? outputMode = settings.OutputMode;
		var outputDirectory = settings.OutputDirectory;
		var connectionString = settings.ConnectionString;
		var databaseProvider = settings.DatabaseProvider;
		var outputUri = settings.Uri;
		var authMode = settings.AuthMode;
		var authToken = settings.AuthToken;

		List<string> siteChoices = new();
		iisController.GetExtendedSiteList();
		CommandProcessor.Instance.ProcessTargetSites(ref targetSites, iisController, settings, ref runMode);
		if ( settings is {Interactive: true} || !string.IsNullOrWhiteSpace(settings.GetSites) ) {
			CommandProcessor.Instance.ProcessSiteChoices(iisController, ref siteChoices);
			if ( settings.GetSites?.ToLower() == @"getsites" ) {
				if ( settings.Id?.ToLower() == @"id" ) {
					return CommandProcessor.Instance.GetSiteIds(ref iisController);
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
				authToken: ref authToken,
				connectionString: ref connectionString,
				databaseProvider: ref databaseProvider
			);
		}

		AnsiConsole.MarkupLine($"[DarkOrange]Run mode[/]: {runMode}");
		AnsiConsole.MarkupLine($"[DarkOrange]Output Mode[/] : {outputMode}");
		switch (outputMode) {
			case OutputMode.Local:
				AnsiConsole.MarkupLine($"[DarkOrange]Output Directory[/] : {outputDirectory}");
				break;
			case OutputMode.LocalDb:
				AnsiConsole.MarkupLine($"[DarkOrange]Local Db ConnectionString[/] : {connectionString}");
				break;
			case OutputMode.Remote:
				AnsiConsole.MarkupLine($"[DarkOrange]Output URI[/] : {outputUri}");
				break;
			case OutputMode.RemoteDb:
				AnsiConsole.MarkupLine(
					$"[DarkOrange]Remote Db ConnectionString[/] : {connectionString}");
				break;
			case null:
				break;
			default:
				throw new ArgumentOutOfRangeException();
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
			settings: settings,
			connectionString: connectionString,
			databaseProvider: databaseProvider
		);

		AnsiConsole.MarkupLine("[DarkOrange]Beginning Log Processsing...[/]");
		if ( settings.Parallel == true && outputMode == OutputMode.RemoteDb ) {
			CommandProcessor.Instance.ProcessLogsAsync(ref commandConfiguration).Wait();
		} else {
			CommandProcessor.Instance.ProcessLogs(ref commandConfiguration);
		}

		AnsiConsole.MarkupLine("[DarkOrange]Finished Processing Logs![/]");


		return 0;
	}
}