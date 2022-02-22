using System;
using System.Collections;
using System.ComponentModel;

namespace IISLogManager.Core;

/// <inheritdoc />
public class UriNotSpecifiedException : Exception {
	public new string Message = "The uri was not specified";
	public UriNotSpecifiedException() { }
}