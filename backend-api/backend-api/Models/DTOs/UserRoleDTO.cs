﻿namespace backend_api.Models.DTOs
{
    public class UserRoleDTO
    {
        public string UserId { get; set; }
        public List<string> UserRoleIds { get; set; }
    }
}