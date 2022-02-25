using System;

namespace IISLogManager.Core;

/// <inheritdoc />
public class UriNotSpecifiedException : Exception {
	/// <inheritdoc />
	public override string Message => "The uri was not specified";
}