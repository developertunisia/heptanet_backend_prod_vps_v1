using Microsoft.EntityFrameworkCore;
using HeptaNet.Domain.Models;
using HeptaNet.Domain.Base;
using HeptaNet.Infrastructure.Database;

namespace HeptaNet.Infrastructure.Repository;

// Repository générique de base
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly MyAppContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(MyAppContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T?> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.FindAsync(id) != null;
    }
}

// Repository spécifique pour User
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(MyAppContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet.Where(u => !u.IsBlacklisted).ToListAsync();
    }

    public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
    {
        return await _dbSet
            .Where(u => u.FirstName.Contains(searchTerm) || 
                       u.LastName.Contains(searchTerm) || 
                       (u.Email != null && u.Email.Contains(searchTerm)))
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetBlacklistedUsersAsync()
    {
        return await _dbSet.Where(u => u.IsBlacklisted).ToListAsync();
    }

    public async Task<User?> GetByUserNameAsync(string userName)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.UserName == userName);
    }
}

