using System;

namespace Xenostrap.Exceptions;

internal class InvalidHTTPResponseException(string message) : Exception(message)
{
}
