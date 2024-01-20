using AutoMapper;
using HotelListing.API.Data;
using HotelListing.API.Interfaces;

namespace HotelListing.API.Repository
{
    public class HotelsRepository : GenericRepository<Hotel>, IHotelsRepository
    {
        public HotelsRepository(HotelListingDbContext context, IMapper mapper) : base(context, mapper)
        {

        }
    }
}
