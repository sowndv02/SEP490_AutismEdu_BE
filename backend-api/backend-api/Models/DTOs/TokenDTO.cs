﻿namespace backend_api.Models.DTOs
{
    public class TokenDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string? AccessTokenGoogle { get; set; }
    }
}
