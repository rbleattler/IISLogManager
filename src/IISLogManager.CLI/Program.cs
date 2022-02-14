using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using IISLogManager.Core;

namespace IISLogManager.CLI;

/// <summary>
/// Working on creating the program but with cmd line args added
/// </summary>
public class Program {
	public static int Main(string[] args) {
		var app = new CommandApp<GetIISLogsCommand>();
		app.Configure(config => {
			config.Settings.ApplicationName = "IISLogManager";
			//TODO: Explore
			// config.AddBranch();
			//TODO: For old systems
			// config.Settings.Console.Profile.Capabilities.Legacy
		});
		return app.Run(args);
	}

	private sealed class GetIISLogsCommand : Command<Settings> {
		//TODO: Remove if moving out of file does not break it
		// public class Settings : CommandSettings {
		// 	[Description("Interactive Mode.\t\t[red]NOTE [/]:\tDISABLES ALL OTHER COMMAND LINE OPTIONS")]
		// 	[DefaultValue(false)]
		// 	[CommandOption("-i|--interactive")]
		// 	public bool Interactive { get; init; }
		//
		//
		// 	[Description("Run Mode.\t\t\t[blue]All[/] sites / [blue]Target[/] sites")]
		// 	[CommandOption("-r|--runmode")]
		// 	[DefaultValue(CLI.RunMode.All)]
		// 	public RunMode? RunMode { get; init; }
		//
		// 	[Description("[DarkOrange](-r Target)[/]\t\tSite [yellow]Names[/]")]
		// 	[CommandOption("-s|--sites")]
		// 	public string[]? SiteNames { get; set; }
		//
		// 	[Description("[DarkOrange](-r Target)[/]\t\tSite [yellow]Urls[/]")]
		// 	[CommandOption("-S|--Sites")]
		// 	public string[]? SiteUrls { get; set; }
		//
		// 	[Description("Output Mode.\t\t[blue]Local[/] disk / [blue]Remote[/] endpoint")]
		// 	[CommandOption("-O|--OutputMode")]
		// 	[DefaultValue(CLI.OutputMode.Local)]
		// 	public OutputMode? OutputMode { get; init; }
		//
		// 	[Description("[DarkOrange](-O Local)[/]")]
		// 	[CommandOption("-o|--OutputDirectory")]
		// 	public string? OutputDirectory { get; set; }
		//
		// 	[Description($"[DarkOrange](-O Remote)[/]")]
		// 	[CommandOption("-u|--Uri")]
		// 	[DefaultValue("localhost:45352")]
		// 	public string? Uri { get; init; }
		//
		//
		// 	public override ValidationResult Validate() {
		// 		//TODO: Add ParameterSet validation
		// 		if ( string.IsNullOrWhiteSpace(OutputDirectory) &&  OutputMode ==  CLI.OutputMode.Local ) {
		// 			var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
		// 			OutputDirectory = $"{userProfilePath}\\IISLogManager\\{DateTime.Now.ToString("yyyy-MM-dd")}";
		// 		}
		//
		// 		return ValidationResult.Success();
		// 	}
		// }


		public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings) {
			IISController iisController = new();
			SiteObjectFactory siteObjectFactory = new();
			var runMode = settings.RunMode;
			var outputMode = settings.OutputMode;
			var outputDirectory = settings.OutputDirectory;
			// var sitesList = iisController.ServerManager.Sites.ToList();
			var siteChoices = new List<string>();
			iisController.ServerManager.Sites.ToList().ForEach((s) => {
				iisController.Sites.Add(siteObjectFactory.BuildSite(s));
			});
			// var siteUrlList = new List<string>();
			foreach (SiteObject site in iisController.Sites) {
				var siteString = string.Format("{0}\t({1})", site.SiteName, site.SiteUrl);
				siteChoices.Add(siteString);
			}

			if ( settings.GetSites?.ToLower() == @"getsites" ) {
				AnsiConsole.MarkupLine("[DarkOrange]Site Name[/]\t([Blue]Site Url[/])");
				siteChoices.ForEach((s) => { AnsiConsole.MarkupLine($"{s}"); });
				return 0;
			}

			var runModePrompt = new SelectionPrompt<RunMode>()
				.Title("Do you want to get [green] All [/] sites, or [green]Target (specific)[/] sites?")
				.PageSize(3)
				.AddChoices(RunMode.All, RunMode.Target);
			var siteSelectPrompt = new MultiSelectionPrompt<string>()
				.Title("Choose sites to process : ")
				.PageSize(10)
				.InstructionsText(
					"[grey](Press [blue]<space>[/] to toggle a site, " +
					"[green]<enter>[/] to accept choices)[/]")
				.MoreChoicesText("[grey](Move up and down to reveal more sites)[/]")
				.AddChoices(siteChoices);
			if ( settings.Interactive ) {
				runMode = AnsiConsole.Prompt(runModePrompt);
				if ( runMode == RunMode.Target ) {
					AnsiConsole.Prompt(siteSelectPrompt);
				}
			}


			AnsiConsole.MarkupLine($"[DarkOrange]Run mode[/]: {runMode}");
			AnsiConsole.MarkupLine($"[DarkOrange]Output Mode[/] : {outputMode}");
			AnsiConsole.MarkupLine($"[DarkOrange]Output Directory[/] : {outputDirectory}");
			// var searchOptions = new EnumerationOptions {
			// 	AttributesToSkip = settings.IncludeHidden
			// 		? FileAttributes.Hidden | FileAttributes.System
			// 		: FileAttributes.System
			// };

			// var searchPattern = settings.SearchPattern ?? "*.*";
			// var searchPath = settings.SearchPath ?? Directory.GetCurrentDirectory();
			// var files = new DirectoryInfo(searchPath)
			// 	.GetFiles(searchPattern, searchOptions);
			//
			// var totalFileSize = files
			// 	.Sum(fileInfo => fileInfo.Length);


			return 0;
		}
	}
}