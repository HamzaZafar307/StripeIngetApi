using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StripeIngest.Api.Domain.Entities;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StripeIngest.Api.Infrastructure.Data;

namespace StripeIngest.Api.Services;

public interface IEventProcessor
{
    Task ProcessEventAsync(JsonElement json);
}

public class EventProcessor : IEventProcessor
{
    private readonly AppDbContext _context;
    private readonly ILogger<EventProcessor> _logger;

    public EventProcessor(AppDbContext context, ILogger<EventProcessor> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ProcessEventAsync(JsonElement json)
    {
        var eventId = json.GetProperty("id").GetString();
        var type = json.GetProperty("type").GetString();
        var created = json.GetProperty("created").GetInt64(); // unix timestamp
        var createdDate = DateTimeOffset.FromUnixTimeSeconds(created).UtcDateTime;

        if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(type))
        {
            _logger.LogError("Invalid event payload: Missing ID or Type");
            return;
        }

        // 1. Idempotency Check (Check if already processed)
        // Note: RawEvent insertion should happen in the same transaction to ensure consistency.
        // But for high ingestion, we might insert RawEvent first. 
        // Here we do it all in one transaction for simplicity and safety.

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var existingEvent = await _context.RawEvents.FindAsync(eventId);
            if (existingEvent != null)
            {
                _logger.LogInformation("Event {EventId} already processed.", eventId);
                return;
            }

            // 2. Persist Raw Event
            var rawEvent = new RawEvent
            {
                EventId = eventId,
                EventType = type,
                CreatedAt = createdDate,
                Payload = json.GetRawText(),
                ProcessedAt = DateTime.UtcNow
            };
            _context.RawEvents.Add(rawEvent);
            await _context.SaveChangesAsync(); // save to get ID if needed, but here ID is string

            // 3. Process Subscription Changes
            if (type.StartsWith("customer.subscription."))
            {
                await HandleSubscriptionEvent(json, eventId, createdDate);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventId}", eventId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task HandleSubscriptionEvent(JsonElement json, string eventId, DateTime eventTimestamp)
    {
        var data = json.GetProperty("data").GetProperty("object");
        var subId = data.GetProperty("id").GetString();
        var customerId = data.GetProperty("customer").GetString();
        var status = data.GetProperty("status").GetString();
        
        // Extract items (assuming single item for now as per requirements)

        // Extract items and calculate total MRR
        var items = data.GetProperty("items").GetProperty("data");
        if (items.GetArrayLength() == 0) return; 

        decimal totalMrr = 0;
        string firstProductId = "";
        string firstPriceId = "";
        int totalQuantity = 0;
        string firstCurrency = "";

        foreach (var item in items.EnumerateArray())
        {
            var plan = item.GetProperty("plan"); 
            var amount = plan.GetProperty("amount").GetDecimal(); 
            var itemQuantity = item.GetProperty("quantity").GetInt32();
            
            // Handle optional interval
            string? interval = null;
            if (plan.TryGetProperty("interval", out var intervalProp))
            {
                interval = intervalProp.GetString();
            }
           
            if (string.IsNullOrEmpty(firstProductId))
            {
                firstProductId = plan.GetProperty("product").GetString() ?? "";
                firstPriceId = plan.GetProperty("id").GetString() ?? "";
                firstCurrency = plan.GetProperty("currency").GetString() ?? "";
            }

            // Convert Amount from Cents to Dollars (Stripe sends amount in lowest currency unit)
            amount = amount / 100m;

            decimal itemMrr = amount * itemQuantity;
            if (interval == "year")
            {
                itemMrr = itemMrr / 12m;
            }
            totalMrr += itemMrr;
            totalQuantity += itemQuantity;
        }

        decimal mrr = totalMrr;
        string productId = firstProductId;
        string priceId = firstPriceId;
        int quantity = totalQuantity;
        string currency = firstCurrency;

        // Fetch Current State
        var currentState = await _context.CurrentSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subId);

        string changeType = "no_change";
        decimal previousMrr = 0;

        if (currentState == null)
        {
            // NEW Subscription
            changeType = "new";
            currentState = new CurrentSubscription
            {
                SubscriptionId = subId!,
                CustomerId = customerId!,
                Status = status!,
                CurrentProduct = productId,
                CurrentPrice = priceId,
                CurrentQuantity = quantity,
                CurrentAmount = mrr,
                Currency = currency,
                LastEventId = eventId,
                LastUpdated = eventTimestamp
            };
            _context.CurrentSubscriptions.Add(currentState);
        }
        else
        {
            // UPDATE
            previousMrr = currentState.CurrentAmount;

            // Determine Change Type
            if (status != "active" && status != "trialing" && status != "past_due") 
            {
                 // e.g. canceled, unpaid, incomplete_expired
                 if (currentState.Status == "active" || currentState.Status == "trialing" || currentState.Status == "past_due")
                 {
                     changeType = "churn";
                     mrr = 0; // Churned MRR is 0
                 }
                 else
                 {
                     changeType = "no_change"; // Already churned or in bad state?
                 }
            }
            else
            {
                // Status is active-like
                if (mrr > previousMrr) changeType = "upgrade";
                else if (mrr < previousMrr) changeType = "downgrade";
                else changeType = "no_change";
            }

            // Update Current State
            currentState.Status = status!;
            currentState.CurrentProduct = productId;
            currentState.CurrentPrice = priceId;
            currentState.CurrentQuantity = quantity;
            currentState.CurrentAmount = mrr;
            currentState.LastEventId = eventId;
            currentState.LastUpdated = eventTimestamp;
            
            _context.CurrentSubscriptions.Update(currentState);
        }

        // Record History if there's a meaningful change (or it's new)
        // We might want to record every event for audit, but prompt emphasizes change classification.
        // Let's record all valid subscription events to be safe, with "no_change" if applicable.
        
        var history = new SubscriptionHistory
        {
            SubscriptionId = subId!,
            EventId = eventId,
            ChangeType = changeType,
            PreviousMRR = previousMrr,
            NewMRR = mrr,
            MRRDelta = mrr - previousMrr,
            Timestamp = eventTimestamp,
            Product = productId,
            Price = priceId,
            Quantity = quantity,
            Currency = currency
        };

        _context.SubscriptionHistory.Add(history);
    }

    private async Task HandleInvoiceEvent(JsonElement json, string eventId, DateTime eventTimestamp)
    {
        var data = json.GetProperty("data").GetProperty("object");
        
        // Invoices have "subscription" field directly, not "id" of subscription
        if (!data.TryGetProperty("subscription", out var subIdProp)) return;
        
        string? subId = subIdProp.GetString();
        if (string.IsNullOrEmpty(subId)) return;

        // Fetch Current State
        var currentState = await _context.CurrentSubscriptions
            .FirstOrDefaultAsync(s => s.SubscriptionId == subId);

        if (currentState == null)
        {
            _logger.LogWarning("Received invoice for unknown subscription {SubscriptionId}", subId);
            return;
        }

        // Record Renewal History
        // Renewal doesn't change MRR, so Delta is 0.
        var history = new SubscriptionHistory
        {
            SubscriptionId = subId,
            EventId = eventId,
            ChangeType = "renewal", // Explicitly mark as renewal
            PreviousMRR = currentState.CurrentAmount,
            NewMRR = currentState.CurrentAmount,
            MRRDelta = 0,
            Timestamp = eventTimestamp,
            Product = currentState.CurrentProduct,
            Price = currentState.CurrentPrice,
            Quantity = currentState.CurrentQuantity,
            Currency = currentState.Currency
        };

        _context.SubscriptionHistory.Add(history);
        
        // Update LastUpdated on CurrentSubscription to reflect activity
        currentState.LastEventId = eventId;
        currentState.LastUpdated = eventTimestamp;
        _context.CurrentSubscriptions.Update(currentState);
    }
}
