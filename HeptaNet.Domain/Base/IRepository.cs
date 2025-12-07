using HeptaNet.Domain.Models;

namespace HeptaNet.Domain.Base;

// Interface générique pour les opérations CRUD de base
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task<T?> UpdateAsync(T entity);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}

// Interface spécifique pour User avec des méthodes métier
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserNameAsync(string userName);
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<IEnumerable<User>> GetBlacklistedUsersAsync();
    Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
}

// Interface spécifique pour AuthorizedEmail avec des méthodes métier
public interface IAuthorizedEmailRepository : IRepository<AuthorizedEmail>
{
    Task<AuthorizedEmail?> GetByEmailAsync(string email);
    Task<IEnumerable<AuthorizedEmail>> GetImportedAsync(bool isImported);
    Task<IEnumerable<AuthorizedEmail>> SearchAsync(string? email, bool? isImported);
    Task<bool> UpdateImportStatusAsync(string email, bool isImported);
    Task<bool> DeleteByEmailAsync(string email);
}