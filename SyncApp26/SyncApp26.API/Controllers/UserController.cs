using Microsoft.AspNetCore.Mvc;
using SyncApp26.Application.IServices;
using SyncApp26.Domain.Entities;
using SyncApp26.Shared.DTOs.Request.User;
using SyncApp26.Shared.DTOs.Response.User;

namespace SyncApp26.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IDepartmentService _departmentService;

        public UserController(IUserService userService, IDepartmentService departmentService)
        {
            _userService = userService;
            _departmentService = departmentService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserGETResponseDTO>> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var department = await _departmentService.GetDepartmentByIdAsync(user.DepartmentId);
            User? assignedTo = null;
            if (user.AssignedToId.HasValue)
            {
                assignedTo = await _userService.GetUserByIdAsync(user.AssignedToId.Value);
            }

            return Ok(new UserGETResponseDTO
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                DepartmentId = user.DepartmentId,
                DepartmentName = department?.Name ?? "Unknown",
                AssignedToId = user.AssignedToId,
                AssignedToName = assignedTo != null ? $"{assignedTo.FirstName} {assignedTo.LastName}" : null,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserGETResponseDTO>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            var responseList = new List<UserGETResponseDTO>();

            foreach (var user in users)
            {
                var department = await _departmentService.GetDepartmentByIdAsync(user.DepartmentId);
                User? assignedTo = null;
                if (user.AssignedToId.HasValue)
                {
                    assignedTo = await _userService.GetUserByIdAsync(user.AssignedToId.Value);
                }

                responseList.Add(new UserGETResponseDTO
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    DepartmentId = user.DepartmentId,
                    DepartmentName = department?.Name ?? "Unknown",
                    AssignedToId = user.AssignedToId,
                    AssignedToName = assignedTo != null ? $"{assignedTo.FirstName} {assignedTo.LastName}" : null,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                });
            }

            return Ok(responseList);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<IEnumerable<UserGETResponseDTO>>> GetUsersByDepartment(Guid departmentId)
        {
            var users = await _userService.GetUsersByDepartmentIdAsync(departmentId);
            var department = await _departmentService.GetDepartmentByIdAsync(departmentId);

            if (department == null)
            {
                return NotFound(new { message = "Department not found" });
            }

            var responseList = new List<UserGETResponseDTO>();

            foreach (var user in users)
            {
                User? assignedTo = null;
                if (user.AssignedToId.HasValue)
                {
                    assignedTo = await _userService.GetUserByIdAsync(user.AssignedToId.Value);
                }

                responseList.Add(new UserGETResponseDTO
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    DepartmentId = user.DepartmentId,
                    DepartmentName = department.Name,
                    AssignedToId = user.AssignedToId,
                    AssignedToName = assignedTo != null ? $"{assignedTo.FirstName} {assignedTo.LastName}" : null,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                });
            }

            return Ok(responseList);
        }

        [HttpGet("assigned-to/{assignedToId}")]
        public async Task<ActionResult<IEnumerable<UserGETResponseDTO>>> GetUsersAssignedTo(Guid assignedToId)
        {
            var users = await _userService.GetUsersAssignedToAsync(assignedToId);
            var lineManager = await _userService.GetUserByIdAsync(assignedToId);

            if (lineManager == null)
            {
                return NotFound(new { message = "Line manager not found" });
            }

            var responseList = new List<UserGETResponseDTO>();

            foreach (var user in users)
            {
                var department = await _departmentService.GetDepartmentByIdAsync(user.DepartmentId);

                responseList.Add(new UserGETResponseDTO
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    DepartmentId = user.DepartmentId,
                    DepartmentName = department?.Name ?? "Unknown",
                    AssignedToId = user.AssignedToId,
                    AssignedToName = $"{lineManager.FirstName} {lineManager.LastName}",
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                });
            }

            return Ok(responseList);
        }

        [HttpPost]
        public async Task<ActionResult<UserResponseDTO>> AddUser([FromBody] UserRequestDTO userRequestDTO)
        {
            if (string.IsNullOrEmpty(userRequestDTO.FirstName) ||
                string.IsNullOrEmpty(userRequestDTO.LastName) ||
                string.IsNullOrEmpty(userRequestDTO.Email))
            {
                return BadRequest(new UserResponseDTO
                {
                    Success = false,
                    Message = "FirstName, LastName, and Email are required"
                });
            }

            // Verify department exists
            var department = await _departmentService.GetDepartmentByIdAsync(userRequestDTO.DepartmentId);
            if (department == null)
            {
                return BadRequest(new UserResponseDTO
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            // Verify assigned to user exists if provided
            if (userRequestDTO.AssignedToId.HasValue)
            {
                var assignedTo = await _userService.GetUserByIdAsync(userRequestDTO.AssignedToId.Value);
                if (assignedTo == null)
                {
                    return BadRequest(new UserResponseDTO
                    {
                        Success = false,
                        Message = "Assigned to user not found"
                    });
                }
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = userRequestDTO.FirstName,
                LastName = userRequestDTO.LastName,
                Email = userRequestDTO.Email,
                DepartmentId = userRequestDTO.DepartmentId,
                AssignedToId = userRequestDTO.AssignedToId,
                CreatedAt = DateTime.UtcNow
            };

            await _userService.AddUserAsync(user);

            return Ok(new UserResponseDTO
            {
                Success = true,
                Message = "User created successfully"
            });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserResponseDTO>> UpdateUser(Guid id, [FromBody] UserRequestDTO userRequestDTO)
        {
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(new UserResponseDTO
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (string.IsNullOrEmpty(userRequestDTO.FirstName) ||
                string.IsNullOrEmpty(userRequestDTO.LastName) ||
                string.IsNullOrEmpty(userRequestDTO.Email))
            {
                return BadRequest(new UserResponseDTO
                {
                    Success = false,
                    Message = "FirstName, LastName, and Email are required"
                });
            }

            // Verify department exists
            var department = await _departmentService.GetDepartmentByIdAsync(userRequestDTO.DepartmentId);
            if (department == null)
            {
                return BadRequest(new UserResponseDTO
                {
                    Success = false,
                    Message = "Department not found"
                });
            }

            // Verify assigned to user exists if provided
            if (userRequestDTO.AssignedToId.HasValue)
            {
                var assignedTo = await _userService.GetUserByIdAsync(userRequestDTO.AssignedToId.Value);
                if (assignedTo == null)
                {
                    return BadRequest(new UserResponseDTO
                    {
                        Success = false,
                        Message = "Assigned to user not found"
                    });
                }
            }

            existingUser.FirstName = userRequestDTO.FirstName;
            existingUser.LastName = userRequestDTO.LastName;
            existingUser.Email = userRequestDTO.Email;
            existingUser.DepartmentId = userRequestDTO.DepartmentId;
            existingUser.AssignedToId = userRequestDTO.AssignedToId;
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _userService.UpdateUserAsync(existingUser);

            return Ok(new UserResponseDTO
            {
                Success = true,
                Message = "User updated successfully"
            });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<UserResponseDTO>> DeleteUser(Guid id)
        {
            var existingUser = await _userService.GetUserByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound(new UserResponseDTO
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            await _userService.DeleteUserAsync(id);

            return Ok(new UserResponseDTO
            {
                Success = true,
                Message = "User deleted successfully"
            });
        }
    }
}
