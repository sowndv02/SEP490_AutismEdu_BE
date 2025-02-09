﻿using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs
{
    public class RoleDTO
    {
        public string? Id { get; set; }
        [Required]
        public string Name { get; set; }
        public List<ApplicationUserDTO>? Users { get; set; }
        public int TotalUsersInRole { get; set; }
    }
}
