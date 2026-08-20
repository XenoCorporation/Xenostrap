using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace XenostrapClient.WebServer.Controllers.Game;

[ApiController]
[Route("Game/KeepAlivePinger.ashx")]
public class KeepAlivePingerController : ControllerBase
{
	private readonly ILogger<KeepAlivePingerController> _logger;

	public KeepAlivePingerController(ILogger<KeepAlivePingerController> logger)
	{
		_logger = logger;
	}

	[HttpGet]
	public IActionResult Handle()
	{
		return Ok();
	}
}
