using IISLogManager.Core;
using Spectre.Console;

namespace IISLogManager.CLI;

public class CommandConfiguration {
	public IISController? IISController { get;  }
	public SiteObjectCollection? TargetSites { get;  }
	public static RunMode? RunMode { get; set; }
	public OutputMode? OutputMode { get;  }
	public AuthMode? AuthMode { get;  }
	public string? AuthToken { get;  }
	public string? OutputDirectory { get;  }
	public string? OutputUri { get;  }
	public Settings? Settings { get;  }

	public static AnsiConsoleOutput? NerdStats { get; set; }

	public CommandConfiguration() { }

	public CommandConfiguration(Settings settings) {
		IISController = new();
		TargetSites = new();
		RunMode = settings.RunMode;
		OutputMode = settings.OutputMode;
		AuthMode = settings.AuthMode;
		AuthToken = settings.AuthToken;
		OutputDirectory = settings.OutputDirectory;
		OutputUri = settings.Uri;
		Settings = settings;
	}

	public CommandConfiguration(
		IISController iisController,
		SiteObjectCollection? targetSites,
		RunMode? runMode,
		OutputMode? outputMode,
		AuthMode? authMode,
		string? authToken,
		string? outputDirectory,
		string? outputUri,
		Settings? settings
	) {
		IISController = iisController;
		TargetSites = targetSites;
		RunMode = runMode;
		OutputMode = outputMode;
		AuthMode = authMode;
		AuthToken = authToken;
		OutputDirectory = outputDirectory;
		OutputUri = outputUri;
		Settings = settings;
	}
}