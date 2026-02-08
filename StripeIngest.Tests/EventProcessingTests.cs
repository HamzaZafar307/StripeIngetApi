using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StripeIngest.Api.Domain.Entities;
using StripeIngest.Api.Infrastructure.Data;
using StripeIngest.Api.Services;
using Xunit;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace StripeIngest.Tests;

public class EventProcessingTests
{
    private AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private JsonElement CreateEvent(string eventId, string type, string subId, string status, decimal amount, string productId = "prod_1", int quantity = 1)
    {
        var json = $@"
        {{
            ""id"": ""{eventId}"",
            ""type"": ""{type}"",
            ""created"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
            ""data"": {{
                ""object"": {{
                    ""id"": ""{subId}"",
                    ""customer"": ""cus_test"",
                    ""status"": ""{status}"",
                    ""items"": {{
                        ""data"": [
                            {{
                                ""quantity"": {quantity},
                                ""plan"": {{
                                    ""id"": ""price_1"",
                                    ""product"": ""{productId}"",
                                    ""amount"": {amount}, 
                                    ""currency"": ""usd"",
                                    ""interval"": ""month""
                                }}
                            }}
                        ]
                    }}
                }}
            }}
        }}";
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public async Task ProcessEvent_NewSubscription_CreatesStateAndHistory()
    {
        using var context = GetContext();
        var processor = new EventProcessor(context, NullLogger<EventProcessor>.Instance);

        var evt = CreateEvent("evt_1", "customer.subscription.created", "sub_1", "active", 100);

        await processor.ProcessEventAsync(evt);

        var sub = await context.CurrentSubscriptions.FindAsync("sub_1");
        Assert.NotNull(sub);
        Assert.Equal("active", sub.Status);
        Assert.Equal(100, sub.CurrentAmount);
        Assert.Equal("new", (await context.SubscriptionHistory.FirstAsync()).ChangeType);
    }

    [Fact]
    public async Task ProcessEvent_Upgrade_UpdatesStateAndHistory()
    {
        using var context = GetContext();
        var processor = new EventProcessor(context, NullLogger<EventProcessor>.Instance);

        // 1. Create
        await processor.ProcessEventAsync(CreateEvent("evt_1", "customer.subscription.created", "sub_1", "active", 100));

        // 2. Upgrade (Amount 200)
        await processor.ProcessEventAsync(CreateEvent("evt_2", "customer.subscription.updated", "sub_1", "active", 200));

        var sub = await context.CurrentSubscriptions.FindAsync("sub_1");
        Assert.Equal(200, sub.CurrentAmount);

        var history = await context.SubscriptionHistory.ToListAsync();
        Assert.Equal(2, history.Count);
        Assert.Equal("new", history[0].ChangeType);
        Assert.Equal("upgrade", history[1].ChangeType);
        Assert.Equal(100, history[1].MRRDelta);
    }

    [Fact]
    public async Task ProcessEvent_DuplicateEvent_IsIdempotent()
    {
        using var context = GetContext();
        var processor = new EventProcessor(context, NullLogger<EventProcessor>.Instance);

        var evt = CreateEvent("evt_1", "customer.subscription.created", "sub_1", "active", 100);

        // First Process
        await processor.ProcessEventAsync(evt);
        
        // Second Process (Duplicate)
        await processor.ProcessEventAsync(evt);

        var history = await context.SubscriptionHistory.ToListAsync();
        Assert.Single(history); // Should still be 1
    }

    [Fact]
    public async Task ProcessEvent_Churn_SetsStatusAndZeroMRR()
    {
        using var context = GetContext();
        var processor = new EventProcessor(context, NullLogger<EventProcessor>.Instance);

        await processor.ProcessEventAsync(CreateEvent("evt_1", "customer.subscription.created", "sub_1", "active", 100));
        await processor.ProcessEventAsync(CreateEvent("evt_2", "customer.subscription.deleted", "sub_1", "canceled", 100)); // Stripe still sends last known plan info usually

        var sub = await context.CurrentSubscriptions.FindAsync("sub_1");
        Assert.Equal("canceled", sub.Status);
        Assert.Equal(0, sub.CurrentAmount);

        var lastHistory = await context.SubscriptionHistory.LastAsync();
        Assert.Equal("churn", lastHistory.ChangeType);
        Assert.Equal(-100, lastHistory.MRRDelta);
    }
}
