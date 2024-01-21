using HotelListing.API.Core.DTOs.Hotel;

namespace HotelListing.API.Core.DTOs.Country
{
    public class CountryDto : BaseCountryDto
    {
        public int Id { get; set; }
        public List<HotelDto> Hotels { get; set; }
    }
}
