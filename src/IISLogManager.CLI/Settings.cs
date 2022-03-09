using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IISLogManager.CLI;

public class Settings : CommandSettings {
	private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
	private static readonly DateOnly OneYearAgoToday = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
	public static readonly string TodayString = Today.ToString();
	public static readonly string OneYearAgoTodayString = OneYearAgoToday.ToString();

	[CommandArgument(0, "[GetSites]")]
	[Description("List all Websites\t\t[DarkOrange]Name[/][Blue] (Url)[/]")]
	public string? GetSites { get; set; }

	[CommandArgument(1, "[CommandArgument]")]
	[Description("Argument\t\t\t[DarkOrange]Id | LogRoot[/]")]
	public string? CommandArgument { get; set; }

	// TODO: Implement Getting Other Info About Sites
	// [CommandArgument(1, "[LogRoot]")]
	// [Description("Lists all Website Log Path Roots\t\t[DarkOrange]GetSites logroot[/]")]
	// public string? LogRoot { get; set; }	
	//
	// [CommandArgument(1, "[Id]")]
	// [Description("Lists all Website LogPaths\t\t[DarkOrange]GetSites Info SiteName/SiteUrl[/]")]
	// public string? Info { get; set; }
	//
	// [CommandArgument(2, "[Id]")]
	// [Description("Target Site\t\t[DarkOrange]Name or Url[/]")]
	// public string? TargetSite { get; set; }
	// 


	[Description("[DarkOrange]Interactive Mode.[/]\t\t[red]NOTE [/]:\tDISABLES ALL OTHER COMMAND LINE OPTIONS")]
	[DefaultValue(false)]
	[CommandOption("-i|--interactive")]
	public bool Interactive { get; set; }

	[Description("[DarkOrange]Run Mode.[/]\t\t\t[blue]All[/] sites / [blue]Target[/] sites")]
	[CommandOption("-r|--runmode")]
	[DefaultValue(CLI.RunMode.All)]
	public RunMode? RunMode { get; set; }

	[Description("[DarkOrange](-r Target)[/]\t\t\tSite [yellow]Names[/]\t\'a.com,b.com\'")]
	[CommandOption("-s|--sites")]
	public string? SiteNames { get; set; }

	[Description("[DarkOrange](-r Target)[/]\t\t\tSite [yellow]Urls[/]\t\'a.com,b.com\'")]
	[CommandOption("-S|--Sites")]
	public string? SiteUrls { get; set; }

	[Description("[DarkOrange]Output Mode.[/]\t\t\t[blue]Local[/] disk / [blue]Remote[/] endpoint")]
	[CommandOption("-O|--OutputMode")]
	[DefaultValue(CLI.OutputMode.Local)]
	public OutputMode? OutputMode { get; set; }

	[Description(
		"[DarkOrange](-O Remote)[/]\t\t\tUse [blue]DefaultCredentials[/] or provide a [blue]BearerToken[/]")]
	[CommandOption("-A|--AuthMode")]
	[DefaultValue(CLI.AuthMode.DefaultCredentials)]
	public AuthMode? AuthMode { get; set; }

	[Description("[DarkOrange](-O Remote -A BearerToken)[/]\tProvide a [blue]BearerToken[/] (Excluide \"Bearer \")")]
	[CommandOption("-a|--AuthToken")]
	public string? AuthToken { get; set; }

	[Description("[DarkOrange](-O Local)[/]")]
	[CommandOption("-o|--OutputDirectory")]
	public string? OutputDirectory { get; set; }

	[Description($"[DarkOrange](-O Remote)[/]")]
	[CommandOption("-u|--Uri")]
	[DefaultValue("localhost:45352")]
	public string? Uri { get; set; }

	[Description($"[DarkOrange]Enable Filtering Logs[/]")]
	[CommandOption("-F|--Filter")]
	[DefaultValue(false)]
	public bool Filter { get; set; }

	[Description($"[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM/dd/yyyy\"")]
	[CommandOption("-f|--FromDate")]
	// [DefaultValue(value: GetTodayString())]
	public string? FromDate { get; set; }

	[Description($"[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM/dd/yyyy\"")]
	[CommandOption("-t|--ToDate")]
	// [DefaultValue(value: GetTodayString())]
	public string? ToDate { get; set; }

	[Description($"[DarkOrange]Ignore Default Web Site[/]\t\tDefault : true")]
	[CommandOption("-z|--IgnoreDefaultSite")]
	[DefaultValue(true)]
	// [DefaultValue(value: GetTodayString())]
	public bool IgnoreDefaultWebSite { get; set; }

	public Settings() { }

	public override ValidationResult Validate() {
		//TODO: Add ParameterSet validation
		if ( string.IsNullOrWhiteSpace(OutputDirectory) &&  OutputMode ==  CLI.OutputMode.Local ) {
			var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
			OutputDirectory = $"{userProfilePath}\\IISLogManager\\{DateTime.Now.ToString("yyyy-MM-dd")}";
		}

		return ValidationResult.Success();
	}
}

// TODO: Rebuild using this method of settings construction : https://spectreconsole.net/cli/composing