using IISLogManager.Core;

namespace IISLogManager.CLI;

public class CommandConfiguration {
	public IISController IISController { get; set; }
	public SiteObjectCollection SiteObjectCollection { get; set; }
	public RunMode RunMode { get; set; }
	public OutputMode OutputMode { get; set; }
	public string OutputDirectory { get; set; }
	public string OutputUri { get; set; }


	public CommandConfiguration(
		IISController iisController,
		SiteObjectCollection siteObjectCollection,
		RunMode runMode,
		OutputMode outputMode,
		string outputDirectory,
		string outputUri
	) {
		this.IISController = iisController;
		SiteObjectCollection = siteObjectCollection;
		RunMode = runMode;
		OutputMode = outputMode;
		OutputDirectory = outputDirectory;
		OutputUri = outputUri;
	}
}