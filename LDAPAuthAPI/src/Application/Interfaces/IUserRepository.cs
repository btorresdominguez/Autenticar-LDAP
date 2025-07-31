using System.Threading.Tasks;
using Domain.DTOs;

namespace Application.Interfaces
{
    public interface IUserRepository
    {
        Task<UserInfoDto> ObtenerUsuarioPorUsernameAsync(string username);
        Task<string?> ObtenerRolPorUsuarioAsync(int usuarioId);
        Task<string?> ObtenerDepartamentoPorUsuarioAsync(int usuarioId);
        Task<User?> GetUsuario(string username, string password);
    }
}
