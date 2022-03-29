using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using Spectre.Console;
using System.Net;
using IISLogManager.Core;
using Newtonsoft.Json;
using Spectre.Console.Cli;

namespace IISLogManager.CLI;

public class Settings : CommandSettings {
	// private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
	// private static readonly DateOnly OneYearAgoToday = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));


	// Default ctor
	public Settings() { }

	// Support configuration files of type JSON/YAML
	public Settings(
		string configurationFilePath,
		ConfigurationFileType configurationFileType = ConfigurationFileType.Json
	) {
		ImportConfiguration(configurationFilePath, configurationFileType);
	}

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
	[DefaultValue(Core.RunMode.All)]
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
	[DefaultValue(Core.OutputMode.Local)]
	public OutputMode? OutputMode { get; set; }


	[Description(
		"[DarkOrange](-O Remote)[/]\t\tRemote Authorization Mode.\nUse [blue]DefaultCredentials[/] or provide a [blue]BearerToken[/]"
	)]
	[CommandOption("-A|--AuthMode")]
	[DefaultValue(Core.AuthMode.DefaultCredentials)]
	public AuthMode? AuthMode { get; set; }

	[Description(
		"[DarkOrange](-O Remote)[/]\t\tRemote Authorization token.\nProvide a [blue]BearerToken[/] (Exclude \"Bearer \")")]
	[CommandOption("-a|--AuthToken")]
	public string? AuthToken { get; set; }


	[Description("[DarkOrange](-O Local)[/]")]
	[CommandOption("-o|--OutputDirectory")]
	public string? OutputDirectory { get; set; }

	[Description("[DarkOrange](-O Remote)[/]")]
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

	[Description("[DarkOrange](-O *Db)[/]\t\t\t\"FieldName1,FieldName2,FieldName3\"")]
	[CommandOption("-E|--ExcludeFields")]
	public string? ExcludeFields { get; set; }
	//TODO: Explicit Include

	[Description("[Magenta]Enable Filtering Logs[/]")]
	[CommandOption("-F|--Filter")]
	[DefaultValue(false)]
	public bool Filter { get; set; }

	[Description("[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM*dd*yyyy\"")]
	[CommandOption("-f|--FromDate")]
	public string? FromDate { get; set; }

	[Description("[DarkOrange](-F true)[/]\t\t\tFormat:\t\"MM*dd*yyyy\"")]
	[CommandOption("-t|--ToDate")]
	public string? ToDate { get; set; }

	[Description("[Magenta]Configuration File Path[/]\t\t[DarkOrange]Load settings from a configuration file[/]")]
	[CommandOption("-C|--Config", IsHidden = false)]
	public string? ConfigurationFile { get; init; }

	[Description(
		$"[DarkOrange](-Parallel-Experimental true)[/]\t\tExperimental parallel processing.[red]\nMay cause unexpected results![/]")]
	[CommandOption("--Parallel-Experimental", IsHidden = true)]
	[DefaultValue(false)]
	public bool? Parallel { get; set; }


	public override ValidationResult Validate() {
		//TODO: Add ParameterSet validation
		if ( string.IsNullOrWhiteSpace(OutputDirectory) &&  OutputMode ==  Core.OutputMode.Local ) {
			var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
			OutputDirectory = $"{userProfilePath}\\IISLogManager\\{DateTime.Now.ToString("yyyy-MM-dd")}";
		}

		return ValidationResult.Success();
	}

	// private void ImportConfiguration(FileInfo fileInfo, ConfigurationFileType configurationFileType) {
	// 	ImportConfiguration(fileInfo.FullName, configurationFileType);
	// }	
	public void ImportConfiguration(
		FileInfo fileInfo,
		ConfigurationFileType configurationFileType = ConfigurationFileType.Json
	) {
		ImportConfiguration(fileInfo.FullName, configurationFileType);
	}

	private void ConvertConfigFromJson(string filePath) {
		var fileString = File.ReadAllText(filePath);
		JsonConvert.PopulateObject(fileString, this);
	}

	// private void ConvertConfigFromYaml(string filePath) {
	// 	var fileString = File.ReadAllText(filePath);
	// 	// var serializerSettings = Serializer(SerializationOptions);
	// 	JsonConvert.PopulateObject(fileString, this);
	// }

	public void ImportConfiguration(
		string configurationFilePath,
		ConfigurationFileType configurationFileType = ConfigurationFileType.Json
	) {
		switch (configurationFileType) {
			case ConfigurationFileType.Json:
				ConvertConfigFromJson(configurationFilePath);
				break;
			case ConfigurationFileType.Yaml:
				throw new NotImplementedException("Yaml config files are not yet supported.");
			// ConvertConfigFromJson(configurationFilePath);
			//break;
		}
	}

	// public void NewFromConfiguration(string filePath) {
	// 	var fileString = File.ReadAllText(filePath);
	// 	JsonConvert.PopulateObject(fileString, this);
	// }
}