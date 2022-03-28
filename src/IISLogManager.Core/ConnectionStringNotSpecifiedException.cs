namespace IISLogManager.Core;

/// <inheritdoc />
public class ConnectionStringNotSpecifiedException : Exception {
	/// <inheritdoc />
	public override string Message => "The connection string was not specified";
}