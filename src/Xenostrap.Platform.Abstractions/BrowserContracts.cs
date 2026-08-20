using System;

namespace Xenostrap.Platform;

public sealed class BrowserMessageReceivedEventArgs : EventArgs
{
	public BrowserMessageReceivedEventArgs(string message)
	{
		Message = message;
	}

	public string Message { get; }
}
