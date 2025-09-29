using LearningHub.Api.Models;
using System;
using System.Threading.Tasks;

namespace LearningHub.Api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task AddAsync(User user);
    }
}
