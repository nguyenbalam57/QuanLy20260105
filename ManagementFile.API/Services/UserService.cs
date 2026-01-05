using ManagementFile.Contracts.DTOs.UserManagement;
using ManagementFile.Models.UserManagement;

namespace ManagementFile.API.Services
{
    public class UserService
    {

        


        /// <summary>
        /// Convert từ User entity sang UserDto
        /// </summary>
        public static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                Department = user.Department,
                PhoneNumber = user.PhoneNumber,
                Position = user.Position,
                ManagerId = user.ManagerId,
                Avatar = user.Avatar,
                Language = user.Language,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                DisplayName = user.DisplayName,
                IsAccountLocked = user.IsAccountLocked
            };
        }

        /// <summary>
        /// Map collection of Users to UserDtos
        /// </summary>
        public static List<UserDto> MapToUserDtos(List<User> users)
        {
            return users.Select(MapToUserDto).ToList();
        }


    }
}
