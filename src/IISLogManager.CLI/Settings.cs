using System.ComponentModel;
using Spectre.Console;
using IISLogManager.Core;
using Spectre.Console.Cli;

namespace IISLogManager.CLI;

public class Settings : CommandSettings {
	private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
	private static readonly DateOnly OneYearAgoToday = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
	public static readonly string TodayString = Today.ToString();
	public static readonly string OneYearAgoTodayString = OneYearAgoToday.ToString();

	[CommandArgument(0, "[GetSites]")]
	[Description("[Magenta]Lists all websites\t\t[DarkOrange]Name[/][Blue] (Url)[/][/]")]
	public string? GetSites { get; set; }

	[CommandArgument(1, "[Id]")]
	[Description("[Magenta]Lists all website Ids\t\t[DarkOrange]GetSites Id[/][/]")]
	public string? Id { get; set; }

	[Description("[Magenta]Interactive Mode.[/]\t\t[red]NOTE [/]:\tDISABLES ALL OTHER COMMAND LINE OPTIONS")]
	[DefaultValue(false)]
	[CommandOption("-i|--interactive")]
	public bool Interactive { get; set; }

	[Description("[Magenta]Run Mode.[/]\t\t\t[blue]All[/] sites / [blue]Target[/] sites")]
	[CommandOption("-r|--runmode")]
	[DefaultValue(CLI.RunMode.All)]
	public RunMode? RunMode { get; set; }

	[Description("[DarkOrange](-r Target)[/]\t\tSite [yellow]Names[/]")]
	[CommandOption("-s|--sites")]
	public string? SiteNames { get; set; }

	[Description("[DarkOrange](-r Target)[/]\t\tSite [yellow]Urls[/]")]
	[CommandOption("-S|--Sites")]
	public string? SiteUrls { get; set; }

	[Description(
		"[Magenta]Output Mode.[/]\t\t[blue]Local[/] disk, [blue]Remote[/] endpoint, [blue]LocalDb[/], [blue]RemoteDb[/]")]
	[CommandOption("-O|--OutputMode")]
	[DefaultValue(CLI.OutputMode.Local)]
	public OutputMode? OutputMode { get; set; }


	[Description(
		"[DarkOrange](-O Remote)[/]\t\tRemote Authorization Mode.\nUse [blue]DefaultCredentials[/] or provide a [blue]BearerToken[/]"
	)]
	[CommandOption("-A|--AuthMode")]
	[DefaultValue(CLI.AuthMode.DefaultCredentials)]
	public AuthMode? AuthMode { get; set; }

	[Description(
		"[DarkOrange](-O Remote)[/]\t\tRemote Authorization token.\nProvide a [blue]BearerToken[/] (Exclude \"Bearer \")")]
	[CommandOption("-a|--AuthToken")]
	public string? AuthToken { get; set; }


	[Description("[DarkOrange](-O Local)[/]")]
	[CommandOption("-o|--OutputDirectory")]
	public string? OutputDirectory { get; set; }

	[Description($"[DarkOrange](-O Remote)[/]")]
	[CommandOption("-u|--Uri")]
	[DefaultValue("localhost:45352")]
	public string? Uri { get; set; }

	[Description("[DarkOrange](-O *Db)[/]")]
	[CommandOption("-c|--ConnectionString")]
	public String? ConnectionString { get; set; }

	[Description("[DarkOrange](-O *Db)[/]")]
	[CommandOption("-T|--TableName")]

	public String? TableName { get; set; }

	[Description("[DarkOrange](-O *Db)[/]\t\t\tSQL, Sqlite, MySQL, PostgreSQL, Oracle ")]
	[CommandOption("-p|--Provider")]
	public DatabaseProvider? DatabaseProvider { get; set; }

	[Description($"[Magenta]Enable Filtering Logs[/]")]
	[CommandOption("-F|--Filter")]
	[DefaultValue(false)]
	public bool Filter { get; set; }

	[Description($"[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM*dd*yyyy\"")]
	[CommandOption("-f|--FromDate")]
	// [DefaultValue(value: GetTodayString())]
	public string? FromDate { get; set; }

	[Description($"[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM*dd*yyyy\"")]
	[CommandOption("-t|--ToDate")]
	// [DefaultValue(value: GetTodayString())]
	public string? ToDate { get; set; }

	[Description(
		$"[DarkOrange](-Parallel-Experimental true)[/]\t\tExperimental parallel processing.[red]May cause unexpected results![/]")]
	[CommandOption("--Parallel-Experimental")]
	[DefaultValue(false)]
	// [DefaultValue(value: GetTodayString())]
	public bool? Parallel { get; set; }

	public override ValidationResult Validate() {
		//TODO: Add ParameterSet validation
		if ( string.IsNullOrWhiteSpace(OutputDirectory) &&  OutputMode ==  CLI.OutputMode.Local ) {
			var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
			OutputDirectory = $"{userProfilePath}\\IISLogManager\\{DateTime.Now.ToString("yyyy-MM-dd")}";
		}

		return ValidationResult.Success();
	}
}