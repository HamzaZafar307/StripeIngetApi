using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StripeIngest.Api.Infrastructure.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StripeIngest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves the events and entire history for a specific customer.
    /// </summary>
    /// <param name="customerId">The Stripe Customer ID (e.g. cus_123).</param>
    /// <returns>A list of all events and their history for a specific customer.</returns>
    [HttpGet("customer/{customerId}/history")]
    public async Task<IActionResult> GetCustomerHistory(string customerId)
    {
        var history = await _context.SubscriptionHistory
            .Where(h => _context.CurrentSubscriptions.Any(c => c.CustomerId == customerId && c.SubscriptionId == h.SubscriptionId))
            .OrderBy(h => h.Timestamp)
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>
    /// Retrieves the Monthly Recurring Revenue (MRR) report.
    /// </summary>
    /// <returns>A list of monthly MRR stats including New, Churn, Expansion, and Contraction MRR.</returns>
    [HttpGet("mrr/monthly")]
    public async Task<IActionResult> GetMonthlyMrr()
    {
        // Query the SQL View directly
        var report = await _context.MonthlyMrrReports
            .OrderBy(r => r.Month)
            .ToListAsync();

        return Ok(report);
    }

    /// <summary>
    /// Retrieves the Yearly Recurring Revenue (ARR/MRR) report.
    /// </summary>
    /// <returns>A list of yearly aggregation stats.</returns>
    [HttpGet("mrr/yearly")]
    public async Task<IActionResult> GetYearlyMrr()
    {
        var rawData = await _context.SubscriptionHistory
             .Select(h => new { 
                 Year = h.Timestamp.Year,
                 h.ChangeType,
                 h.MRRDelta 
             })
             .ToListAsync();

        var report = rawData
            .GroupBy(x => x.Year)
            .Select(g => new
            {
                Year = g.Key,
                NewMRR = g.Where(x => x.ChangeType == "new").Sum(x => x.MRRDelta),
                ExpansionMRR = g.Where(x => x.ChangeType == "upgrade").Sum(x => x.MRRDelta),
                ContractionMRR = g.Where(x => x.ChangeType == "downgrade").Sum(x => x.MRRDelta),
                ChurnedMRR = g.Where(x => x.ChangeType == "churn").Sum(x => x.MRRDelta),
                NetMRRChange = g.Sum(x => x.MRRDelta)
            })
            .OrderBy(r => r.Year)
            .ToList();

        return Ok(report);
    }
    /// <summary>
    /// Retrieves a summary of all subscription statuses.
    /// </summary>
    /// <returns>Counts of active, canceled, etc. subscriptions.</returns>
    [HttpGet("subscriptions/summary")]
    public async Task<IActionResult> GetSubscriptionSummary()
    {
        var summary = await _context.CurrentSubscriptions
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), TotalMRR = g.Sum(s => s.CurrentAmount) })
            .ToListAsync();

        var totalActive = summary.Where(x => x.Status == "active").Sum(x => x.Count);
        var totalActiveMrr = summary.Where(x => x.Status == "active").Sum(x => x.TotalMRR);

        return Ok(new
        {
            TotalActiveSubscriptions = totalActive,
            TotalActiveMRR = totalActiveMrr,
            Details = summary
        });
    }
}
