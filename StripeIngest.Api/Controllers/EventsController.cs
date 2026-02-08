using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StripeIngest.Api.Infrastructure.Data;
using StripeIngest.Api.Domain.Entities;

namespace StripeIngest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllEvents()
    {
        var events = await _context.RawEvents
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(events);
    }
}
