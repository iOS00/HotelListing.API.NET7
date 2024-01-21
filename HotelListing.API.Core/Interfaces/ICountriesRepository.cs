using HotelListing.API.Core.DTOs.Country;
using HotelListing.API.Data;

namespace HotelListing.API.Core.Interfaces
{
    public interface ICountriesRepository : IGenericRepository<Country> 
    {
        Task<CountryDto> GetDetails(int id);
    }
}
