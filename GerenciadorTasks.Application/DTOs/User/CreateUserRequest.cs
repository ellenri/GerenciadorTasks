using GerenciadorTasks.Core.Enums;

namespace GerenciadorTasks.Application.DTOs.User
{
    public class CreateUserRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime BirthDate { get; set; }
        public UserRole Role { get; set; }
        public Guid? ParentId  { get; set; }

    }
}
