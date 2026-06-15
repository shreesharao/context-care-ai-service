using ContextCare.Domain.Interfaces;
using ContextCare.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ContextCare.Domain.Services;

public class RepositoryService<T> : IRepositoryService<T> where T : class
{
    private readonly AppDbContext _context;
    public RepositoryService(AppDbContext context)
    {
        _context = context;
    }
    public Task AddAsync(T item)
    {
        _context.Set<T>().Add(item);
        return _context.SaveChangesAsync();
    }

    public Task DeleteAsync(T item)
    {
        _context.Set<T>().Remove(item);
        return _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await _context.Set<T>().FindAsync(id);
    }

    public async Task UpdateAsync(T item)
    {
        _context.Set<T>().Update(item);
        await _context.SaveChangesAsync();
    }
    public IQueryable<T> Query()
    {
        return _context.Set<T>().AsNoTracking();
    }
}
