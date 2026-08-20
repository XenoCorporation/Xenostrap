using System;

namespace Xenostrap.Exceptions;

internal class ChecksumFailedException(string message) : Exception(message)
{
}
