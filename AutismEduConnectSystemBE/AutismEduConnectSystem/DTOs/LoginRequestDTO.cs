﻿using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs
{
    public class LoginRequestDTO
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string AuthenticationRole { get; set; }

    }
}
