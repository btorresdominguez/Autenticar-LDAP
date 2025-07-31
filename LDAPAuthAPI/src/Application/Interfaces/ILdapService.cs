using Application.DTOs;
using Domain.DTOs;

public interface ILdapService

{
    Task<UserInfoDto> ValidateCredentialsAsync(string username, string password);
    Task<string> TestLdapConnection();

    Task<UserInfoDto?> FindUserByUsername(string username);

    void AddOrUpdateUser(string username);



}