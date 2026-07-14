using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using wms_android.shared.Data;
using wms_android.shared.Models;
using wms_android.cache;
using wms_android.cache.Interfaces;
using wms_android.api.Services;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;

        public BranchesController(AppDbContext context, ICacheService cache, ICacheInvalidationService cacheInvalidation)
        {
            _context = context;
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Branch>>> GetBranches()
        {
            var branches = await _cache.GetOrSetAsync(
                CacheKeys.Branches.All(),
                async () => await _context.Branches.AsNoTracking().OrderBy(b => b.Name).ToListAsync(),
                TimeSpan.FromHours(1),
                new[] { CacheTags.Branches });
            return Ok(branches);
        }
    }
} 