using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System;
using System.Data;

public class LoginInfoRepository : ILoginInfoRepository
{
    private readonly string _connectionString;


    public LoginInfoRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task SaveLoginInfoAsync(string username, string token, string jsonRespuesta, string message)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand("sp_SaveLoginInfo", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("@Username", username ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Token", token ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@JsonRespuesta", jsonRespuesta ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Message", message ?? (object)DBNull.Value);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }
}
