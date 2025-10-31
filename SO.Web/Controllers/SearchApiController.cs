using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using SO.Core;
using SO.Data;
using SO.Web.Services;

[ApiController]
[Route("api/[controller]")]
public class SearchApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly UserSearchService _searchService;
    private readonly ElasticUserSearchService _elasticUserSearchService;

    public SearchApiController(AppDbContext dbContext, IMemoryCache memoryCache, IDistributedCache redisCache,
        UserSearchService userSearchService, ElasticUserSearchService elasticUserSearchService)
    {
        _context = dbContext;
        _memoryCache = memoryCache;
        _redisCache = redisCache;
        _searchService = userSearchService;
        _elasticUserSearchService = elasticUserSearchService;
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

        if (!_memoryCache.TryGetValue(cacheKey, out UserEntity? user))
        {
            var redisData = await _redisCache.GetStringAsync(cacheKey);
            if (redisData != null)
            {
                user = JsonSerializer.Deserialize<UserEntity>(redisData);
                _memoryCache.Set(cacheKey, user, TimeSpan.FromMinutes(5));
            }
            else
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == 1);
                if (user != null)
                {
                    await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(user), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                    });

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

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var results = await _elasticUserSearchService.SearchUsersAsync(q);
        return Ok(results);
    }
}
