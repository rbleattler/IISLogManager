using IISLogManager.Core;

namespace IISLogManager.CLI;

public class CommandConfiguration {
	public IISController? IISController { get; set; }
	public SiteObjectCollection? TargetSites { get; set; }
	public static RunMode? RunMode { get; set; }
	public static OutputMode? OutputMode { get; set; }
	public string? OutputDirectory { get; set; }
	public string? OutputUri { get; set; }
	public Settings? Settings { get; set; }

	public CommandConfiguration() { }

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