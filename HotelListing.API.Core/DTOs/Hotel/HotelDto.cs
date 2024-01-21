using System.ComponentModel.DataAnnotations.Schema;

namespace HotelListing.API.Core.DTOs.Hotel
{

    public class HotelDto : BaseHotelDto
    {
        public int Id { get; set; }
    }

}
