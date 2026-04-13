using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using wms_android.shared.Data;
using wms_android.shared.Models;
using Microsoft.AspNetCore.OutputCaching;

namespace wms_android.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BranchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Branches
        [HttpGet]
        [OutputCache(PolicyName = "Branches")]
        public async Task<ActionResult<IEnumerable<Branch>>> GetBranches()
        {
            return await _context.Branches.AsNoTracking().ToListAsync();
        }
    }
} 