using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;
using IISLogManager.Core;

namespace IISLogManager.CLI;

public class Fascia {
	private LiveDisplay LiveDisplay { get; set; }

	public Fascia() {
		LiveDisplay.Overflow = VerticalOverflow.Crop;
		LiveDisplay.Start(context => {
			
		});
	}
	
	
	
}