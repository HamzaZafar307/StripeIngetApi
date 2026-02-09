using System.Text.Json.Serialization;

namespace StripeIngest.Api.Dtos;

/// <summary>
/// Represents a Stripe Webhook Event.
/// </summary>
public class StripeEventDto
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    /// <example>evt_1</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The type of event (e.g., customer.subscription.created).
    /// </summary>
    /// <example>customer.subscription.created</example>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of when the event was created (Unix epoch).
    /// </summary>
    /// <example>1704067200</example>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// The data object containing the event details.
    /// </summary>
    [JsonPropertyName("data")]
    public StripeDataDto Data { get; set; } = new();
}

public class StripeDataDto
{
    /// <summary>
    /// The object affected by the event (e.g., the Subscription object).
    /// </summary>
    [JsonPropertyName("object")]
    public StripeObjectDto Object { get; set; } = new();
}

public class StripeObjectDto
{
    /// <summary>
    /// Unique identifier for the object (e.g., subscription ID).
    /// </summary>
    /// <example>sub_1</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the customer associated with this object.
    /// </summary>
    /// <example>cus_1</example>
    [JsonPropertyName("customer")]
    public string Customer { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the subscription.
    /// </summary>
    /// <example>active</example>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// List of subscription items.
    /// </summary>
    [JsonPropertyName("items")]
    public StripeItemsDto Items { get; set; } = new();
}

public class StripeItemsDto
{
    /// <summary>
    /// Array of subscription item objects.
    /// </summary>
    [JsonPropertyName("data")]
    public List<StripeItemDataDto> Data { get; set; } = new();
}

public class StripeItemDataDto
{
    /// <summary>
    /// Quantity of the subscription item.
    /// </summary>
    /// <example>1</example>
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Plan details associated with the item.
    /// </summary>
    [JsonPropertyName("plan")]
    public StripePlanDto Plan { get; set; } = new();
}

public class StripePlanDto
{
    /// <summary>
    /// Unique identifier for the price/plan.
    /// </summary>
    /// <example>price_1</example>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Product ID associated with this price.
    /// </summary>
    /// <example>prod_1</example>
    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    /// <summary>
    /// Amount in cents.
    /// </summary>
    /// <example>10000</example>
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., usd).
    /// </summary>
    /// <example>usd</example>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Interval of the plan (month or year).
    /// </summary>
    /// <example>month</example>
    [JsonPropertyName("interval")]
    public string Interval { get; set; } = string.Empty;
}
