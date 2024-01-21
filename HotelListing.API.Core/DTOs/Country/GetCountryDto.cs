using System.ComponentModel.DataAnnotations.Schema;

namespace HotelListing.API.Core.DTOs.Country
{
    public class GetCountryDto  // for countries as List<GetCountryDto>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
    }
}
