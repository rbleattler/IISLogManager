using IISLogManager.Core;
using Spectre.Console;

namespace IISLogManager.CLI;

public class RunPrompts {
	private static FilterConfiguration filterConfig = new();
	// private static Settings settings;

	public static SelectionPrompt<RunMode> RunModePrompt = new SelectionPrompt<RunMode>()
		.Title("Do you want to get [green] All [/] sites, or [green]Target (specific)[/] sites?")
		.PageSize(3)
		.AddChoices(RunMode.All, RunMode.Target);

	public static SelectionPrompt<OutputMode> OutputModePrompt = new SelectionPrompt<OutputMode>()
		.Title("Do you want to output to a [green] Local [/] file, or [green] Remote (specific)[/] endpoint?")
		.PageSize(3)
		.AddChoices(OutputMode.Local, OutputMode.Remote);

	public static MultiSelectionPrompt<string> SiteSelectPrompt = new MultiSelectionPrompt<string>()
		.Title("Choose sites to process : ")
		.PageSize(10)
		.InstructionsText(
			"[grey](Press [blue]<space>[/] to toggle a site, " +
			"[green]<enter>[/] to accept choices)[/]")
		.MoreChoicesText("[grey](Move up and down to reveal more sites)[/]");

	public static TextPrompt<string?> OutDirectoryPrompt =
		new TextPrompt<string?>("Where do you want to save the file?")
			.DefaultValue(
				$"{Environment.GetEnvironmentVariable("USERPROFILE")}\\IISLogManager\\{DateTime.Now.ToString("yyyy-MM-dd")}"
			);

	public static SelectionPrompt<string> FilterPrompt =
		new SelectionPrompt<string>()
			.Title("Would you like to filter the logs by date?")
			.PageSize(3)
			.AddChoices("Yes", "No")
			// Convert choice to bool value
			.UseConverter(s => {
				string output = string.Empty;
				switch (s) {
					case "Yes":
					case "yes":
					case "Y":
					case "y":
						output = "Yes";
						break;
					case "No":
					case "no":
					case "N":
					case "n":
					case null:
						output = "No";
						break;
				}

				return output;
			});

	public static TextPrompt<string> StartDatePrompt = new(@"Enter the start date to parse logs from (MM/dd/yyyy)");
	public static TextPrompt<string> EndDatePrompt = new(@"Enter the end date to parse logs from (MM/dd/yyyy)");


	public static TextPrompt<string> OutUriPrompt =
		new TextPrompt<string>("Enter the remote endpoint URI (including protocol & port if necessary)");

	public static ConfirmationPrompt ConfirmContinuePrompt = new("Ready to continue?");

	public static void ExecutePrompts(ref RunMode? runMode, ref OutputMode? outputMode, ref string? outputUri,
		ref string? outputDirectory, ref List<string> siteChoices, ref IISController iisController,
		ref SiteObjectCollection? targetSites, ref FilterConfiguration filterConfiguration) {
		runMode = AnsiConsole.Prompt(RunModePrompt);
		outputMode = AnsiConsole.Prompt(OutputModePrompt);
		if ( outputMode == OutputMode.Remote ) {
			outputUri = AnsiConsole.Prompt(OutUriPrompt);
		}

		if ( outputMode == OutputMode.Local ) {
			outputDirectory = AnsiConsole.Prompt(OutDirectoryPrompt);
		}

		if ( runMode == RunMode.Target ) {
			List<string> rawTargetSites =
				AnsiConsole.Prompt(SiteSelectPrompt.AddChoices(siteChoices));
			Dictionary<string, string> splitSites = new();
			rawTargetSites.ForEach(raw => {
				var splitString = raw.Split('\t');
				var trimmedUrl = Utils.ExtractUrl(raw);
				splitSites.Add(splitString[0], trimmedUrl);
			});
			var filteredSites = iisController.Sites.Where(s => {
				var url = s.SiteUrl;
				var name = s.SiteName;
				if ( splitSites.ContainsKey(name) ) return true;
				if ( splitSites.ContainsValue(url) ) return true;
				return false;
			});
			targetSites?.AddRange(filteredSites);
		}

		if ( runMode == RunMode.All ) {
			targetSites = iisController.Sites;
		}

		var promptResponse = AnsiConsole.Prompt(FilterPrompt);
		filterConfiguration.FilterState = promptResponse == "Yes" ? FilterState.Enabled : FilterState.Disabled;
		if ( filterConfiguration.FilterState == FilterState.Enabled ) {
			filterConfiguration.SetFromDate(AnsiConsole.Prompt(StartDatePrompt));
			filterConfiguration.SetToDate(AnsiConsole.Prompt(EndDatePrompt));
			foreach (var site in targetSites!) {
				var fromDate = filterConfiguration.FromDate;
				var toDate = filterConfiguration.ToDate;
				var allPaths = site.LogFilePaths.ToList();
				var filteredPaths = allPaths.Where(path => {
					var lwt = File.GetLastWriteTime(path);
					return lwt > fromDate && lwt < toDate;
				});
				site.LogFilePaths = filteredPaths.ToArray();
			}
		}
	}
}