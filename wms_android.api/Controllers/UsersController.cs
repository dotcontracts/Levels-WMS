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

namespace wms_android.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _context;

        public UsersController(IUserService userService, AppDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        [AllowAnonymous] // For testing; consider adding authorization in production
        public async Task<ActionResult<IEnumerable<dynamic>>> GetUsers()
        {
            try
            {
                // Get all users with their roles and branches in a single efficient query
                var users = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Role)
                    .ToListAsync();
                
                // Get all branches at once to avoid N+1 queries
                var branches = await _context.Branches
                    .AsNoTracking()
                    .ToDictionaryAsync(b => b.Id, b => b);
                
                // Format the response with branch information
                var formattedUsers = new List<dynamic>();
                
                foreach (var user in users)
                {
                    // Get branch information from preloaded dictionary
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
                        // Get branch information if BranchId is set
                        if (user.BranchId.HasValue && branches.ContainsKey(user.BranchId.Value))
                        {
                            var branch = branches[user.BranchId.Value];
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
                    catch (Exception branchEx)
                    {
                        // Log the branch error but continue with user data
                        Console.WriteLine($"Error fetching branch for user {user.Id}: {branchEx.Message}");
                    }
                    
                    formattedUsers.Add(new
                    {
                        user.Id,
                        user.Username,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.CreatedAt,
                        user.IsActive,
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
                    Role = new
                    {
                        user.Role.Id,
                        user.Role.Name,
                        user.Role.Description
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
        [AllowAnonymous] // For testing; consider adding authorization in production
        public async Task<ActionResult<dynamic>> GetUserByUsername(string username)
        {
            try
            {
                Console.WriteLine($"[UsersController] Getting user by username: '{username}'");
                
                // Get user by username
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    Console.WriteLine($"[UsersController] User '{username}' not found");
                    return NotFound(new { message = $"User with username '{username}' not found" });
                }

                Console.WriteLine($"[UsersController] User found - ID: {user.Id}, Username: {user.Username}, RoleId: {user.RoleId}");
                Console.WriteLine($"[UsersController] Role object: {(user.Role != null ? $"Role ID: {user.Role.Id}, Name: '{user.Role.Name}'" : "null")}");
                
                // If role is null but RoleId is set, there might be a database issue
                if (user.Role == null && user.RoleId != 0)
                {
                    Console.WriteLine($"[UsersController] WARNING: User has RoleId {user.RoleId} but Role is null - checking database...");
                    var roleExists = await _context.Roles.AnyAsync(r => r.Id == user.RoleId);
                    Console.WriteLine($"[UsersController] Role with ID {user.RoleId} exists in database: {roleExists}");
                }

                // Get branch information if available
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
                catch
                {
                    // Ignore branch lookup errors
                }

                // Format the response - match the User model structure expected by the client
                var result = new
                {
                    id = user.Id,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    isActive = user.IsActive,
                    roleId = user.RoleId,
                    role = user.Role != null ? new
                    {
                        id = user.Role.Id,
                        name = user.Role.Name,
                        description = user.Role.Description
                    } : null,
                    branchId = user.BranchId,
                    branch = branchInfo
                };

                Console.WriteLine($"[UsersController] Returning result with role: {(result.role != null ? $"ID: {result.role.id}, Name: '{result.role.name}'" : "null")}");
                return Ok(result);
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
                existingUser.BranchId = userDto.BranchId > 0 ? userDto.BranchId : (int?)null; // Update branch assignment
                existingUser.IsActive = userDto.IsActive;

                // If password is provided, update it
                if (!string.IsNullOrEmpty(userDto.Password))
                {
                    byte[] passwordHash, passwordSalt;
                    CreatePasswordHash(userDto.Password, out passwordHash, out passwordSalt);
                    existingUser.PasswordHash = passwordHash;
                    existingUser.PasswordSalt = passwordSalt;
                }

                await _userService.UpdateUserAsync(existingUser);
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
    }

    public class ChangePasswordDto
    {
        public string? NewPassword { get; set; }
    }
} 