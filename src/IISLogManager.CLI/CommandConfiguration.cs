using IISLogManager.Core;
using Spectre.Console;

namespace IISLogManager.CLI;

public class CommandConfiguration {
	public IISController? IISController { get; set; }
	public SiteObjectCollection? TargetSites { get; set; }
	public static RunMode? RunMode { get; set; }
	public static OutputMode? OutputMode { get; set; }
	public string? OutputDirectory { get; set; }
	public string? OutputUri { get; set; }
	public Settings? Settings { get; set; }

	public static AnsiConsoleOutput NerdStats { get; set; }

	public CommandConfiguration() { }

	public CommandConfiguration(Settings settings) {
		IISController = new();
		TargetSites = new();
		RunMode = settings.RunMode;
		OutputMode = settings.OutputMode;
		OutputDirectory = settings.OutputDirectory;
		OutputUri = settings.Uri;
		Settings = settings;
	}

	public CommandConfiguration(
		IISController iisController,
		SiteObjectCollection? targetSites,
		RunMode? runMode,
		OutputMode? outputMode,
		string? outputDirectory,
		string? outputUri,
		Settings? settings
	) {
		IISController = iisController;
		TargetSites = targetSites;
		RunMode = runMode;
		OutputMode = outputMode;
		OutputDirectory = outputDirectory;
		OutputUri = outputUri;
		Settings = settings;
	}
}