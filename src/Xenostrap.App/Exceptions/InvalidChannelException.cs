using System;
using System.Net;

namespace Xenostrap.Exceptions;

public class InvalidChannelException(HttpStatusCode? statusCode) : Exception
{
	public HttpStatusCode? StatusCode = statusCode;
}
