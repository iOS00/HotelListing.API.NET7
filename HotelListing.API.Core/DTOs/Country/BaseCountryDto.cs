using System.ComponentModel.DataAnnotations;

namespace HotelListing.API.Core.DTOs.Country
{
    public abstract class BaseCountryDto
    {
        [Required]  // because required for PUT, must be required in abstract as base
        public string Name { get; set; } 
        public string ShortName {  get; set; }
    }
}
