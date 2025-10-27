using Microsoft.AspNetCore.Mvc;
using SO.Data;

namespace SO.Web.ApiControllers;

[ApiController]
[Route("api/")]
public class SearchApiController: ControllerBase
{
    private readonly AppDbContext _context;
    public SearchApiController(AppDbContext dbContext)
    {
        _context = dbContext;
    }

    [HttpGet("Normal Search")]
    public IActionResult Index()
    {
        var users = _context.Users.FirstOrDefault(u=>u.Id == 1);
        return Ok(users);
    }
}