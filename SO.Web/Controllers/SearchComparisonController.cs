using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using SO.Data;
using SO.Core;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SO.Web.Controllers;

public class SearchComparisonController : Controller
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;

    public SearchComparisonController(AppDbContext dbContext, IMemoryCache memoryCache, IDistributedCache redisCache)
    {
        _context = dbContext;
        _memoryCache = memoryCache;
        _redisCache = redisCache;
    }

    [HttpGet("/search-comparison")]
    public async Task<IActionResult> Index()
    {
        var totalUsers = await _context.Users.CountAsync();

        var viewModel = new SearchComparisonViewModel
        {
            TotalUsers = totalUsers
        };

        return View(viewModel);
    }

    [HttpPost("/search-comparison/cached")]
public async Task<IActionResult> CachedSearch(string query)
{
    query = query?.Trim();
    if (string.IsNullOrEmpty(query))
    {
        var totalUsers = await _context.Users.CountAsync();
        var emptyViewModel = new SearchComparisonViewModel
        {
            TotalUsers = totalUsers
        };
        return View("Index", emptyViewModel);
    }

    string cacheKey = $"search_{query.ToLower()}";
    List<UserEntity>? cachedFiltered = null;
    var stopwatch = Stopwatch.StartNew();

    if (!_memoryCache.TryGetValue(cacheKey, out cachedFiltered))
    {
        var redisData = await _redisCache.GetStringAsync(cacheKey);
        if (redisData != null)
        {
            cachedFiltered = JsonSerializer.Deserialize<List<UserEntity>>(redisData);
            _memoryCache.Set(cacheKey, cachedFiltered, TimeSpan.FromMinutes(5));
        }
        else
        {
            var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var users = await _context.Users.AsNoTracking().ToListAsync();
            cachedFiltered = users.Where(u => terms.All(t =>
                (u.FirstName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.LastName?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.College?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.University?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.TechStack?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Program?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (u.Address?.Contains(t, StringComparison.OrdinalIgnoreCase) ?? false)
            )).ToList();

            await _redisCache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cachedFiltered),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30) });
            _memoryCache.Set(cacheKey, cachedFiltered, TimeSpan.FromMinutes(5));
        }
    }

    stopwatch.Stop();

    var viewModel = new SearchComparisonViewModel
    {
        Query = query,
        CachedTime = stopwatch.ElapsedMilliseconds,
        CachedData = cachedFiltered ?? new List<UserEntity>(),
        TotalUsers = await _context.Users.CountAsync()
    };

    return View("Index", viewModel);
}
    public class SearchComparisonViewModel
    {
        public string Query { get; set; } = "";
        public long CachedTime { get; set; }
        public int TotalUsers { get; set; }
        public List<UserEntity> CachedData { get; set; } = new();
    }
}
