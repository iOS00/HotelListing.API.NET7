﻿using System.ComponentModel.DataAnnotations;

namespace HotelListing.API.Core.DTOs.Users
{
    public class ApiUserDto : LoginDto
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
    }
}
