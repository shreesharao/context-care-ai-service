using System;

namespace ContextCare.Domain.Interfaces;

public interface IRepositoryService<T>
{
    Task AddAsync(T item);
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task UpdateAsync(T item);
    Task DeleteAsync(T item);
    IQueryable<T> Query();
}
