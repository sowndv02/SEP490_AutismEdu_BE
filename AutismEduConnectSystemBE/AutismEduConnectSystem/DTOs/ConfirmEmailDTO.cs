﻿using System.ComponentModel.DataAnnotations;

namespace AutismEduConnectSystem.DTOs
{
    public class ConfirmEmailDTO
    {
        [Required]
        public string Code { get; set; }
        [Required]
        public string Security { get; set; }
        [Required]
        public string UserId { get; set; }
    }
}
