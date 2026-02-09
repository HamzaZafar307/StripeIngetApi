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

    /// <summary>
    /// Receives Stripe webhook events.
    /// </summary>
    /// <remarks>
    /// Send a JSON payload to simulate a Stripe event. 
    /// 
    /// **Example 1: Monthly Subscription ($100/mo)**
    /// 
    ///     POST /api/webhook
    ///     {
    ///       "id": "evt_monthly_1",
    ///       "type": "customer.subscription.created",
    ///       "created": 1704067200,
    ///       "data": {
    ///         "object": {
    ///           "id": "sub_monthly",
    ///           "customer": "cus_test",
    ///           "status": "active",
    ///           "items": {
    ///             "data": [
    ///               {
    ///                 "quantity": 1,
    ///                 "plan": {
    ///                   "id": "price_month",
    ///                   "product": "prod_month",
    ///                   "amount": 10000,
    ///                   "currency": "usd",
    ///                   "interval": "month"
    ///                 }
    ///               }
    ///             ]
    ///           }
    ///         }
    ///       }
    ///     }
    ///     
    /// **Example 2: Yearly Subscription ($1200/yr)**
    /// 
    ///     POST /api/webhook
    ///     {
    ///       "id": "evt_yearly_1",
    ///       "type": "customer.subscription.created",
    ///       "created": 1704067200,
    ///       "data": {
    ///         "object": {
    ///           "id": "sub_yearly",
    ///           "customer": "cus_test",
    ///           "status": "active",
    ///           "items": {
    ///             "data": [
    ///               {
    ///                 "quantity": 1,
    ///                 "plan": {
    ///                   "id": "price_year",
    ///                   "product": "prod_year",
    ///                   "amount": 120000,
    ///                   "currency": "usd",
    ///                   "interval": "year"
    ///                 }
    ///               }
    ///             ]
    ///           }
    ///         }
    ///       }
    ///     }
    /// </remarks>
    /// <param name="evt">The webhook event payload.</param>
    /// <response code="200">Event processed successfully</response>
    /// <response code="400">Invalid event payload</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Receive([FromBody] Dtos.StripeEventDto evt)
    {
        try
        {
            // Serialize back to JsonElement to maintain existing processing logic
            var jsonString = JsonSerializer.Serialize(evt);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            await _processor.ProcessEventAsync(jsonElement);
            return Ok(new { message = "Event processed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook");
            return StatusCode(500, new { error = "Internal Server Error" });
        }
    }
}
