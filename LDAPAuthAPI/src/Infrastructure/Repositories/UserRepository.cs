using Application.Interfaces;
using Dapper;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;


        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public async Task<User?> GetUsuario(string username, string password)
        {
            await using var connection = new SqlConnection(_connectionString);

            const string sql = @"EXEC sp_bt_LoginUsuario @username, @password";

            var result = await connection.QueryAsync<User, string, User>(
                sql,
                (usuario, rol) =>
                {
                    usuario.Roles ??= new List<string>();
                    usuario.Roles.Add(rol);
                    return usuario;
                },
                new { username, password },
                splitOn: "Rol"
            );

            var user = result.FirstOrDefault();
            if (user == null)
                return null;

            const string hashSql = @"SELECT Password FROM bt_usr_usuario WHERE Usuario = @username AND Estado = 1";
            var hashGuardado = await connection.QueryFirstOrDefaultAsync<string>(hashSql, new { username });

            if (hashGuardado == null || !BCrypt.Net.BCrypt.Verify(password, hashGuardado))
                return null;

            return user;
        }

        public async Task<UserInfoDto> ObtenerUsuarioPorUsernameAsync(string username)
        {
            await using var connection = new SqlConnection(_connectionString);

            const string sql = @"
            SELECT Id, Username, Email, DisplayName, Estado
            FROM Usuarios
            WHERE Username = @username AND Estado = 1";

            return await connection.QueryFirstOrDefaultAsync<UserInfoDto>(sql, new { username });
        }

        public async Task<string?> ObtenerRolPorUsuarioAsync(int usuarioId)
        {
            await using var connection = new SqlConnection(_connectionString);

            const string sql = @"SELECT TOP 1 Rol FROM UsuarioRoles WHERE UsuarioId = @usuarioId";

            return await connection.QueryFirstOrDefaultAsync<string>(sql, new { usuarioId });
        }

        public async Task<string?> ObtenerDepartamentoPorUsuarioAsync(int usuarioId)
        {
            await using var connection = new SqlConnection(_connectionString);

            const string sql = @"SELECT TOP 1 Departamento FROM UsuarioDepartamentos WHERE UsuarioId = @usuarioId";

            return await connection.QueryFirstOrDefaultAsync<string>(sql, new { usuarioId });
        }
    }
}