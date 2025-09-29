using LearningHub.Api.Dtos;
using System.Threading.Tasks;

namespace LearningHub.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest dto);
        Task<AuthResponse?> LoginAsync(LoginRequest dto);
    }
}
