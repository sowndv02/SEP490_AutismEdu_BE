﻿using System.ComponentModel.DataAnnotations;

namespace backend_api.Models.DTOs
{
    public class RoleDTO
    {
        public string? Id { get; set; }
        [Required]
        public string Name { get; set; }
    }
}