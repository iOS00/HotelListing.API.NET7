using HotelListing.API.DTOs.Hotel;

namespace HotelListing.API.DTOs.Country
{
    public class CountryDto : BaseCountryDto
    {
        public int Id { get; set; }
        public List<HotelDto> Hotels { get; set; }
    }
}
