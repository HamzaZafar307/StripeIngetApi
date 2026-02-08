using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StripeIngest.Api.Services;

namespace StripeIngest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IEventProcessor _processor;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IEventProcessor processor, ILogger<WebhookController> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement json)
    {
        try
        {
            await _processor.ProcessEventAsync(json);
            return Ok(new { message = "Event processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook");
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
}
