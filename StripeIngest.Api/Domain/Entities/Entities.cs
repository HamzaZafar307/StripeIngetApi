using System;
using System.ComponentModel.DataAnnotations;

namespace StripeIngest.Api.Domain.Entities;

public class RawEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
}

public class CurrentSubscription
{
    public string SubscriptionId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentProduct { get; set; }
    public string? CurrentPrice { get; set; }
    public int CurrentQuantity { get; set; }
    public decimal CurrentAmount { get; set; } // MRR
    public string? Currency { get; set; }
    public string? LastEventId { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SubscriptionHistory
{
    [Key]
    public int Id { get; set; }
    public string SubscriptionId { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty; // new, upgrade, downgrade, churn
    public decimal PreviousMRR { get; set; }
    public decimal NewMRR { get; set; }
    public decimal MRRDelta { get; set; }
    public DateTime Timestamp { get; set; }

    // New fields for detailed history
    public string? Product { get; set; }
    public string? Price { get; set; }
    public int Quantity { get; set; }
    public string? Currency { get; set; }
}

// Keyless entity for Reporting View
public class MonthlyMrrReport
{
    public string Month { get; set; } = string.Empty;
    public decimal NewMRR { get; set; }
    public decimal ExpansionMRR { get; set; }
    public decimal ContractionMRR { get; set; }
    public decimal ChurnedMRR { get; set; }
    public decimal NetMRRChange { get; set; }
}
