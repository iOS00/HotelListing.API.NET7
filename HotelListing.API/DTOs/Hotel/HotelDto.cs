using System.ComponentModel.DataAnnotations.Schema;

namespace HotelListing.API.DTOs.Hotel
{

    public class HotelDto : BaseHotelDto
    {
        public int Id { get; set; }
    }

}
