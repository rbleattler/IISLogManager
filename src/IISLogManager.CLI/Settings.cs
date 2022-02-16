using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IISLogManager.CLI;

public class Settings : CommandSettings {
	private static readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Today);
	private static readonly DateOnly _oneYearAgoToday = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
	public static readonly string TodayString = _today.ToString();
	public static readonly string OneYearAgoTodayString = _oneYearAgoToday.ToString();

	[CommandArgument(0, "[GetSites]")]
	[Description("Lists all websites [DarkOrange]Name[/][Blue] (Url)[/]")]
	public string? GetSites { get; init; }

	[Description("Interactive Mode.\t\t[red]NOTE [/]:\tDISABLES ALL OTHER COMMAND LINE OPTIONS")]
	[DefaultValue(false)]
	[CommandOption("-i|--interactive")]
	public bool Interactive { get; init; }

	[Description("Run Mode.\t\t\t[blue]All[/] sites / [blue]Target[/] sites")]
	[CommandOption("-r|--runmode")]
	[DefaultValue(CLI.RunMode.All)]
	public RunMode? RunMode { get; init; }

	[Description("[DarkOrange](-r Target)[/]\t\tSite [yellow]Names[/]")]
	[CommandOption("-s|--sites")]
	public string? SiteNames { get; set; }

	[Description("[DarkOrange](-r Target)[/]\t\tSite [yellow]Urls[/]")]
	[CommandOption("-S|--Sites")]
	public string? SiteUrls { get; set; }

	[Description("Output Mode.\t\t[blue]Local[/] disk / [blue]Remote[/] endpoint")]
	[CommandOption("-O|--OutputMode")]
	[DefaultValue(CLI.OutputMode.Local)]
	public OutputMode? OutputMode { get; init; }

	[Description("[DarkOrange](-O Local)[/]")]
	[CommandOption("-o|--OutputDirectory")]
	public string? OutputDirectory { get; set; }

	[Description($"[DarkOrange](-O Remote)[/]")]
	[CommandOption("-u|--Uri")]
	[DefaultValue("localhost:45352")]
	public string? Uri { get; init; }

	[Description($"[DarkOrange]Enable Filtering Logs[/]")]
	[CommandOption("-F|--Filter")]
	[DefaultValue(false)]
	public bool Filter { get; init; }

	[Description($"[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM*dd*yyyy\"")]
	[CommandOption("-f|--FromDate")]
	// [DefaultValue(value: GetTodayString())]
	public string? FromDate { get; init; }
	
	[Description($"[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM*dd*yyyy\"")]
	[CommandOption("-t|--ToDate")]
	// [DefaultValue(value: GetTodayString())]
	public string? ToDate { get; init; }

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