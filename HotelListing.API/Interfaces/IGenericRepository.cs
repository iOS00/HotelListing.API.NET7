using HotelListing.API.DTOs;

namespace HotelListing.API.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // defines methods to be used for all enpoinds we need
        Task<T> GetAsync(int? id);
        Task<List<T>> GetAllAsync();
        Task<PagedResult<TResult>> GetAllAsync<TResult>(QueryParameters queryParameters);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);  //NB: not Task<T>
        Task DeleteAsync(int id);
        Task<bool> Exists(int id);  // before refactoring - last method in controller
    }
}
