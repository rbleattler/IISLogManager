using System.Text;

namespace IISLogManager.CLI;

public class Examples {
	private Examples() { }
	public static Examples Instance { get; } = new();

	public List<string> GetExamples() {
		var ex4 = new StringBuilder();

		List<string> ex = new() {
			"",
			"-h",
			"-i",
			"-r Target -s \"Default Web Site, Web Site 2\" -O Local -o C:\\Test",
			"-r Target -s Default Web Site -F -f 01/01/2021 -t 01/01/2022"
		};
		return ex;
	}
}