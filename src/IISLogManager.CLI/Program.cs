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
			// Help Example
			Examples.Instance.GetExamples().ForEach(e => { config.AddExample(new[] {e}); });
		});
		// app.Configure();
		return app.Run(args);
	}
}