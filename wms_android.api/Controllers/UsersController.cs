using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wms_android.shared.Data;
using wms_android.shared.Interfaces;
using wms_android.shared.Models;
using Microsoft.AspNetCore.Authorization;
using System;
using wms_android.cache;
using wms_android.cache.Interfaces;
using wms_android.api.Services;

namespace wms_android.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _context;
        private readonly ICacheService _cache;
        private readonly ICacheInvalidationService _cacheInvalidation;

        public UsersController(IUserService userService, AppDbContext context, ICacheService cache, ICacheInvalidationService cacheInvalidation)
        {
            _userService = userService;
            _context = context;
            _cache = cache;
            _cacheInvalidation = cacheInvalidation;
        }

        // GET: api/Users
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetUsers()
        {
            try
            {
                var cached = await _cache.GetOrSetAsync(
                    CacheKeys.Users.All(),
                    async () =>
                    {
                        var users = await _context.Users
                            .AsNoTracking()
                            .Include(u => u.Role)
                            .ToListAsync();

                        var branches = await _context.Branches
                            .AsNoTracking()
                            .ToDictionaryAsync(b => b.Id, b => b);

                        var result = new List<UserListItemDto>();
                        foreach (var user in users)
                        {
                            UserBranchDto? branchDto = null;
                            if (user.BranchId.HasValue && branches.TryGetValue(user.BranchId.Value, out var branch))
                            {
                                branchDto = new UserBranchDto
                                {
                                    Id = branch.Id,
                                    Name = branch.Name,
                                    Address = branch.Address,
                                    Phone = branch.Phone,
                                    Email = branch.Email
                                };
                            }

                            result.Add(new UserListItemDto
                            {
                                Id = user.Id,
                                Username = user.Username,
                                Email = user.Email,
                                FirstName = user.FirstName,
                                LastName = user.LastName,
                                CreatedAt = user.CreatedAt,
                                IsActive = user.IsActive,
                                CanConfirm = user.CanConfirm,
                                CanDispatch = user.CanDispatch,
                                Role = new UserRoleDto
                                {
                                    Id = user.Role.Id,
                                    Name = user.Role.Name,
                                    Description = user.Role.Description,
                                    CanConfirm = user.Role.CanConfirm,
                                    CanDispatch = user.Role.CanDispatch
                                },
                                Branch = branchDto
                            });
                        }
                        return new UserListWrapper { Users = result };
                    },
                    TimeSpan.FromHours(1),
                    new[] { CacheTags.Users, CacheTags.Branches });

                return Ok(cached.Users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        [AllowAnonymous] // For testing; consider adding authorization in production
        public async Task<ActionResult<dynamic>> GetUser(int id)
        {
            try
            {
                // Get user with role first
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Try to get branch information separately to handle potential schema mismatches
                var branchInfo = new
                {
                    Id = (int?)null,
                    Name = (string?)null,
                    Address = (string?)null,
                    Phone = (string?)null,
                    Email = (string?)null
                };
                
                try
                {
                    // Try to get branch information if BranchId is set
                    if (user.BranchId.HasValue)
                    {
                        var branch = await _context.Branches
                            .FirstOrDefaultAsync(b => b.Id == user.BranchId.Value);
                        
                        if (branch != null)
                        {
                            branchInfo = new
                            {
                                Id = (int?)branch.Id,
                                Name = branch.Name,
                                Address = branch.Address,
                                Phone = branch.Phone,
                                Email = branch.Email
                            };
                        }
                    }
                }
                catch (Exception branchEx)
                {
                    // Log the branch error but continue with user data
                    Console.WriteLine($"Error fetching branch for user {user.Id}: {branchEx.Message}");
                }

                // Format the response
                var userResponse = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.FirstName,
                    user.LastName,
                    user.CreatedAt,
                    user.IsActive,
                    user.CanConfirm,
                    user.CanDispatch,
                    Role = new
                    {
                        user.Role.Id,
                        user.Role.Name,
                        user.Role.Description,
                        user.Role.CanConfirm,
                        user.Role.CanDispatch
                    },
                    Branch = branchInfo
                };

                return Ok(userResponse);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Users/byRole/{roleName}
        [HttpGet("byRole/{roleName}")]
        [AllowAnonymous] // For testing; consider adding authorization in production
        public async Task<ActionResult<IEnumerable<dynamic>>> GetUsersByRole(string roleName)
        {
            try
            {
                // Get all users
                var users = await _userService.GetUsersAsync();
                
                // Filter by role name
                var filteredUsers = users.Where(u => u.Role.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Format the response
                var formattedUsers = new List<dynamic>();
                
                foreach (var user in filteredUsers)
                {
                    // Try to get branch information
                    var branchInfo = new
                    {
                        Name = "Unknown", // Default value
                        Address = "",
                        Phone = "",
                        Email = ""
                    };
                    
                    try
                    {
                        // Use raw SQL to query for branch info
                        var sql = @"
                            SELECT b.name, b.address, b.phone, b.email 
                            FROM ""UserBranches"" ub 
                            JOIN ""Branches"" b ON ub.branch_id = b.id 
                            WHERE ub.user_id = @userId";
                        
                        using (var command = _context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = sql;
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = "userId";
                            parameter.Value = user.Id;
                            command.Parameters.Add(parameter);
                            
                            if (command.Connection.State != System.Data.ConnectionState.Open)
                                await command.Connection.OpenAsync();
                            
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    branchInfo = new
                                    {
                                        Name = reader["name"].ToString(),
                                        Address = reader["address"].ToString(),
                                        Phone = reader["phone"].ToString(),
                                        Email = reader["email"].ToString()
                                    };
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue
                        Console.WriteLine($"Error fetching branch for user {user.Id}: {ex.Message}");
                    }
                    
                    // Add the user with branch info to our result list
                    formattedUsers.Add(new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.CreatedAt,
                        Role = new
                        {
                            user.Role.Id,
                            user.Role.Name,
                            user.Role.Description
                        },
                        Branch = branchInfo
                    });
                }

                return Ok(formattedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Users/username/{username}
        [HttpGet("username/{username}")]
        [AllowAnonymous]
        public async Task<ActionResult<UserProfileDto>> GetUserByUsername(string username)
        {
            try
            {
                var cached = await _cache.GetOrSetAsync(
                    CacheKeys.Users.ByUsername(username),
                    async () =>
                    {
                        var user = await _context.Users
                            .AsNoTracking()
                            .Include(u => u.Role)
                            .FirstOrDefaultAsync(u => u.Username == username);

                        if (user == null)
                            return null!;

                        UserBranchDto? branchDto = null;
                        if (user.BranchId.HasValue)
                        {
                            var branch = await _context.Branches
                                .AsNoTracking()
                                .FirstOrDefaultAsync(b => b.Id == user.BranchId.Value);
                            if (branch != null)
                                branchDto = new UserBranchDto
                                {
                                    Id = branch.Id,
                                    Name = branch.Name,
                                    Address = branch.Address,
                                    Phone = branch.Phone,
                                    Email = branch.Email
                                };
                        }

                        return new UserProfileDto
                        {
                            Id = user.Id,
                            Username = user.Username,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email,
                            IsActive = user.IsActive,
                            CanConfirm = user.CanConfirm,
                            CanDispatch = user.CanDispatch,
                            RoleId = user.RoleId,
                            BranchId = user.BranchId,
                            Role = user.Role != null ? new UserRoleDto
                            {
                                Id = user.Role.Id,
                                Name = user.Role.Name,
                                Description = user.Role.Description,
                                CanConfirm = user.Role.CanConfirm,
                                CanDispatch = user.Role.CanDispatch
                            } : null,
                            Branch = branchDto
                        };
                    },
                    TimeSpan.FromHours(1),
                    new[] { CacheTags.Users });

                if (cached == null)
                    return NotFound(new { message = $"User with username '{username}' not found" });

                return Ok(cached);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving user by username: {ex.Message}" });
            }
        }

        // GET: api/Users/byBranch/{branchName}
        [HttpGet("byBranch/{branchName}")]
        [AllowAnonymous] // For testing; consider adding authorization in production
        public async Task<ActionResult<IEnumerable<dynamic>>> GetUsersByBranch(string branchName)
        {
            try
            {
                Console.WriteLine($"Searching for users in branch: {branchName}");
                
                // Get all users with their roles
                var users = await _userService.GetUsersAsync();
                var formattedUsers = new List<dynamic>();
                
                // Process each user
                foreach (var user in users)
                {
                    // Try to get branch information for this user
                    var branchInfo = new
                    {
                        Name = "Unknown", // Default value
                        Address = "",
                        Phone = "",
                        Email = ""
                    };
                    
                    try
                    {
                        // Use raw SQL to get the branch info
                        var sql = @"
                            SELECT b.name, b.address, b.phone, b.email 
                            FROM ""UserBranches"" ub 
                            JOIN ""Branches"" b ON ub.branch_id = b.id 
                            WHERE ub.user_id = @userId";
                        
                        using (var command = _context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = sql;
                            var parameter = command.CreateParameter();
                            parameter.ParameterName = "userId";
                            parameter.Value = user.Id;
                            command.Parameters.Add(parameter);
                            
                            if (command.Connection.State != System.Data.ConnectionState.Open)
                                await command.Connection.OpenAsync();
                            
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    string branchNameFromDb = reader["name"].ToString();
                                    
                                    // Check if this branch matches the search criteria
                                    if (branchNameFromDb.Contains(branchName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        Console.WriteLine($"Found user {user.Username} in branch: {branchNameFromDb}");
                                        
                                        branchInfo = new
                                        {
                                            Name = branchNameFromDb,
                                            Address = reader["address"].ToString(),
                                            Phone = reader["phone"].ToString(),
                                            Email = reader["email"].ToString()
                                        };
                                        
                                        // Add this user to the results
                                        formattedUsers.Add(new
                                        {
                                            user.Id,
                                            user.Username,
                                            user.Email,
                                            user.CreatedAt,
                                            Role = new
                                            {
                                                user.Role.Id,
                                                user.Role.Name,
                                                user.Role.Description
                                            },
                                            Branch = branchInfo
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error but continue
                        Console.WriteLine($"Error fetching branch for user {user.Id}: {ex.Message}");
                    }
                }
                
                // Return result
                if (!formattedUsers.Any())
                {
                    return NotFound($"No users found in branch '{branchName}'");
                }
                
                return Ok(formattedUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Users/branch-test
        [HttpGet("branch-test")]
        [AllowAnonymous]
        public async Task<ActionResult<string>> TestBranches()
        {
            try
            {
                // Get all branches for testing
                var branches = new List<string>();
                
                try
                {
                    // Use raw SQL to get all branches
                    var sql = @"SELECT ""Name"" FROM ""Branches""";
                    
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandText = sql;
                        
                        if (command.Connection.State != System.Data.ConnectionState.Open)
                            await command.Connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string branchName = reader["Name"].ToString();
                                branches.Add(branchName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Error fetching branches: {ex.Message}";
                }
                
                return Ok($"Found {branches.Count} branches: {string.Join(", ", branches)}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/Users
        [HttpPost]
        [AllowAnonymous] // Only admins can create users
        public async Task<ActionResult<User>> CreateUser([FromBody] UserCreateDto userDto)
        {
            if (userDto == null)
            {
                return BadRequest("User data is required.");
            }

            try
            {
                CreatePasswordHash(userDto.Password, out byte[] passwordHash, out byte[] passwordSalt);

                var user = new User
                {
                    Username = userDto.Username,
                    Email = userDto.Email,
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    RoleId = userDto.RoleId,
                    BranchId = userDto.BranchId > 0 ? userDto.BranchId : (int?)null, // Assign branch directly
                    CreatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                await _cacheInvalidation.InvalidateUsersCache();

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Only admins can update users
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
        {
            try
            {
                if (id != userDto.Id)
                {
                    return BadRequest("ID mismatch");
                }

                var existingUser = await _userService.GetUserByIdAsync(id);
                if (existingUser == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Update user properties
                existingUser.Username = userDto.Username;
                existingUser.Email = userDto.Email;
                existingUser.FirstName = userDto.FirstName;
                existingUser.LastName = userDto.LastName;
                existingUser.RoleId = userDto.RoleId;
                existingUser.BranchId = userDto.BranchId > 0 ? userDto.BranchId : (int?)null;
                existingUser.IsActive = userDto.IsActive;
                existingUser.CanConfirm = userDto.CanConfirm;
                existingUser.CanDispatch = userDto.CanDispatch;

                // If password is provided, update it
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    byte[] passwordHash, passwordSalt;
                    CreatePasswordHash(userDto.Password, out passwordHash, out passwordSalt);
                    existingUser.PasswordHash = passwordHash;
                    existingUser.PasswordSalt = passwordSalt;
                }

                await _userService.UpdateUserAsync(existingUser);
                await _cacheInvalidation.InvalidateUsersCache();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/Users/{id}/change-password
        [HttpPut("{id}/change-password")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto passwordDto)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(passwordDto.NewPassword, out passwordHash, out passwordSalt);
                user.PasswordHash = passwordHash;
                user.PasswordSalt = passwordSalt;

                await _userService.UpdateUserAsync(user);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Prevent deleting the current user
                var currentUserId = User.FindFirst("userId")?.Value;
                if (currentUserId != null && int.Parse(currentUserId) == id)
                {
                    return BadRequest("Cannot delete your own account");
                }

                await _userService.DeleteUserAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/Users/{id}/activate
        [HttpPut("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Activate the user
                user.IsActive = true;
                
                await _userService.UpdateUserAsync(user);
                return Ok(new { message = "User activated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/Users/{id}/deactivate
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }

                // Prevent deactivating the current user
                var currentUserId = User.FindFirst("userId")?.Value;
                if (currentUserId != null && int.Parse(currentUserId) == id)
                {
                    return BadRequest("Cannot deactivate your own account");
                }

                // Deactivate the user
                user.IsActive = false;
                
                await _userService.UpdateUserAsync(user);
                return Ok(new { message = "User deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
    }

    public class UserCreateDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public int RoleId { get; set; }
        public int BranchId { get; set; }
    }

    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
        public int RoleId { get; set; }
        public int BranchId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool CanConfirm { get; set; } = false;
        public bool CanDispatch { get; set; } = false;
    }

    public class ChangePasswordDto
    {
        public string? NewPassword { get; set; }
    }

    public class UserRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool CanConfirm { get; set; }
        public bool CanDispatch { get; set; }
    }

    public class UserBranchDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }

    public class UserListItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public bool CanConfirm { get; set; }
        public bool CanDispatch { get; set; }
        public UserRoleDto Role { get; set; } = new();
        public UserBranchDto? Branch { get; set; }
    }

    public class UserListWrapper
    {
        public List<UserListItemDto> Users { get; set; } = new();
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public bool CanConfirm { get; set; }
        public bool CanDispatch { get; set; }
        public int RoleId { get; set; }
        public int? BranchId { get; set; }
        public UserRoleDto? Role { get; set; }
        public UserBranchDto? Branch { get; set; }
    }
} 