using Spectre.Console;

namespace IISLogManager.CLI;

public class Fascia {
	private LiveDisplay LiveDisplay { get; set; }

	public Fascia() {
		LiveDisplay.Overflow = VerticalOverflow.Crop;
		LiveDisplay.Start(context => { });
	}
}