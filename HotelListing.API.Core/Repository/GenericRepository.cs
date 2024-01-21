using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.API.Core.DTOs;
using HotelListing.API.Core.Interfaces;
using HotelListing.API.Data;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.API.Core.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly HotelListingDbContext _context;
        private readonly IMapper _mapper;

        public GenericRepository(HotelListingDbContext context, IMapper mapper)
        {
            this._context = context;
            this._mapper = mapper;
        }
        public async Task<T> AddAsync(T entity)
        {
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await GetAsync(id);
            _context.Set<T>().Remove(entity);  //Remove doesn't RemoveAsync
            await _context.SaveChangesAsync();
        }

        public async Task<bool> Exists(int id)
        {
            var entity = await GetAsync(id);
            return entity != null;
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();  // Set<T> corresponds to DbSet in HotelListingDbContext
        }

        public async Task<PagedResult<TResult>> GetAllAsync<TResult>(QueryParameters queryParameters)
        {
            var totalSize = await _context.Set<T>().CountAsync();  // get number of all items 
            var items = await _context.Set<T>()
                .Skip(queryParameters.StartIndex)  // skip rows before StartIndex (if specified)
                .Take(queryParameters.PageSize)    // define page size (we've set 15 as default)
                .ProjectTo<TResult>(_mapper.ConfigurationProvider)  // map to DTO as in MapperConfig.cs
                .ToListAsync();
            return new PagedResult<TResult> 
            { 
                Items = items,
                PageNumber = queryParameters.PageNumber,
                RecordNumber = queryParameters.PageSize,
                TotalCount = totalSize
            };
        }

        public async Task<T> GetAsync(int? id)
        {
            if (id == null)
            {
                return null;
            }
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task UpdateAsync(T entity)  //NB: here we use non-generic Task but method accepts(T)
        {
            _context.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}
