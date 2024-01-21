using HotelListing.API.Data;

namespace HotelListing.API.Core.Interfaces
{
    public interface ICountriesRepository : IGenericRepository<Country> 
    {
        Task<Country> GetDetails(int id);
    }
}
