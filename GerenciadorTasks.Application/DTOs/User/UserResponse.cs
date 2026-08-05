using GerenciadorTasks.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GerenciadorTasks.Application.DTOs.User
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public int Points { get; set; }
        public Guid? ParentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
