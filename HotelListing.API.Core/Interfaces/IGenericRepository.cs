using HotelListing.API.Core.DTOs;
using Microsoft.Build.Execution;

namespace HotelListing.API.Core.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // defines methods to be used for all enpoinds we need
        Task<T> GetAsync(int? id);
        Task<TResult> GetAsync<TResult>(int? id);
        Task<List<T>> GetAllAsync();
        Task<List<TResult>> GetAllAsync<TResult>();
        Task<PagedResult<TResult>> GetAllAsync<TResult>(QueryParameters queryParameters);
        Task<T> AddAsync(T entity);
        Task<TResult> AddAsync<TSource, TResult>(TSource source);
        Task UpdateAsync(T entity);  //NB: not Task<T>
        Task UpdateAsync<TSource>(int id, TSource source);
        Task DeleteAsync(int id);
        Task<bool> Exists(int id);  // before refactoring - last method in controller
    }
}
