using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.API.Core.DTOs.Country;
using HotelListing.API.Core.Exceptions;
using HotelListing.API.Core.Interfaces;
using HotelListing.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.API.Core.Repository
{
    public class CountriesRepository : GenericRepository<Country>, ICountriesRepository
    {
        private readonly HotelListingDbContext _context;
        private readonly IMapper _mapper;
        public CountriesRepository(HotelListingDbContext context, IMapper mapper) : base(context, mapper)  //must be injected in derived also
        {
            this._context = context;
            this._mapper = mapper;
        }

        public async Task<CountryDto> GetDetails(int id)
        {
            var country = await _context.Countries.Include(q => q.Hotels)
                .ProjectTo<CountryDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (country == null) 
            {
                throw new NotFoundException(nameof(GetDetails), id);
            }
            return country;
        }
    }
}
