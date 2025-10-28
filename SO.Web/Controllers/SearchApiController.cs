using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using SO.Data;
using System.Diagnostics;
using System.Text.Json;
using SO.Core;

namespace SO.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;

    public SearchApiController(AppDbContext dbContext, IMemoryCache memoryCache, IDistributedCache redisCache)
    {
        _context = dbContext;
        _memoryCache = memoryCache;
        _redisCache = redisCache;
    }

    [HttpGet("normal-search")]
    public IActionResult NormalSearch()
    {
        var stopwatch = Stopwatch.StartNew();

        var user = _context.Users.FirstOrDefault(u => u.Id == 1);

        stopwatch.Stop();
        return Ok(new
        {
            Source = "Database",
            TimeTakenMs = stopwatch.ElapsedMilliseconds,
            Data = user
        });
    }

    [HttpGet("cached-search")]
    public async Task<IActionResult> CachedSearch()
    {
        var stopwatch = Stopwatch.StartNew();
        string cacheKey = "user_1";

        // 🧠 Try MemoryCache first
        if (!_memoryCache.TryGetValue(cacheKey, out UserEntity? user))
        {
            // 🧠 Try Redis next
            var redisData = await _redisCache.GetStringAsync(cacheKey);
            if (redisData != null)
            {
                user = JsonSerializer.Deserialize<UserEntity>(redisData);
                _memoryCache.Set(cacheKey, user, TimeSpan.FromMinutes(5)); // short-term memory cache
            }
            else
            {
                // ⛏️ Finally get from database
                user = _context.Users.FirstOrDefault(u => u.Id == 1);

                if (user != null)
                {
                    // Save in Redis (long-term)
                    await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    });

                    // Also save in MemoryCache
                    _memoryCache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
                }
            }
        }

        stopwatch.Stop();
        return Ok(new
        {
            Source = user == null ? "None" : "Cache or Database",
            TimeTakenMs = stopwatch.ElapsedMilliseconds,
            Data = user
        });
    }
}
