using Application.DTOs;
using Application.Interfaces;
using Domain.DTOs;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using System;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AuthenticateController : ControllerBase
{
    private readonly ILdapService _ldapService;
    private readonly IJwtTokenHelper _jwtTokenHelper;
    private readonly ILoginInfoRepository _loginRepo;
    private readonly IConfiguration _config;
    private readonly ILogger<LdapService> _logger;


    public AuthenticateController(
        ILdapService ldapService,
        IJwtTokenHelper jwtTokenHelper,
        ILoginInfoRepository loginRepo,
        IConfiguration config,
        ILogger<LdapService> logger)
    {
        _ldapService = ldapService;
        _jwtTokenHelper = jwtTokenHelper;
        _loginRepo = loginRepo;
        _config = config;
        _logger = logger;
    }

    [HttpGet("find/{username}")]
    public async Task<IActionResult> FindUser(string username)
    {
        var user = await _ldapService.FindUserByUsername(username);
        if (user == null)
        {
            return NotFound(new { Message = "Usuario no encontrado" });
        }
        return Ok(user);
    }

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] LoginRequestDto loginRequest)
    {
        try
        {
            _logger.LogInformation("Solicitud de autenticación recibida para el usuario: {Username}", loginRequest.Username);

            var userInfo = await _ldapService.ValidateCredentialsAsync(loginRequest.Username, loginRequest.Password);

            if (userInfo == null)
            {
                _logger.LogWarning("Falló la autenticación para el usuario: {Username}", loginRequest.Username);

                var errorResponse = new
                {
                    status = "error",
                    message = "Invalid credentials or user not found."
                };

                string jsonError = JsonSerializer.Serialize(errorResponse);
                await _loginRepo.SaveLoginInfoAsync(loginRequest.Username, string.Empty, jsonError, "Credenciales inválidas");

                return Unauthorized(errorResponse);
            }

            _logger.LogInformation("Usuario autenticado correctamente: {Username}", userInfo.Username);
            var token = _jwtTokenHelper.GenerateToken(userInfo);

            var successResponse = new
            {
                status = "success",
                user = new
                {
                    username = userInfo.Username,
                    email = userInfo.Email,
                    displayName = userInfo.DisplayName,
                    department = userInfo.Department,
                    title = userInfo.Title
                }
            };

            string jsonSuccess = JsonSerializer.Serialize(successResponse);
            await _loginRepo.SaveLoginInfoAsync(userInfo.Username, token, jsonSuccess, "Login exitoso");

            return Ok(successResponse);
        }
        catch (Novell.Directory.Ldap.LdapException ldapEx)
        {
            _logger.LogError(ldapEx, "Error LDAP al autenticar usuario {Username}: {Message}", loginRequest.Username, ldapEx.Message);
            var errorResponse = new
            {
                status = "error",
                message = ldapEx.Message // Devuelve el mensaje real de LDAP
            };
            string jsonError = JsonSerializer.Serialize(errorResponse);
            await _loginRepo.SaveLoginInfoAsync(loginRequest.Username, null, jsonError, ldapEx.Message);

            return Unauthorized(errorResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al autenticar usuario {Username}: {Message}", loginRequest.Username, ex.Message);
            var errorResponse = new
            {
                status = "error",
                message = "Unexpected error occurred during authentication."
            };
            string jsonError = JsonSerializer.Serialize(errorResponse);
            await _loginRepo.SaveLoginInfoAsync(loginRequest.Username, null, jsonError, "Error en autenticación");

            return StatusCode(500, errorResponse);
        }
    }

    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        try
        {
            // Llama a tu método de prueba de conexión LDAP
            var result = await _ldapService.TestLdapConnection();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al conectar a LDAP: {ex.Message}");
        }
    }


    [HttpPost("add-or-update")]
    public async Task<IActionResult> AddOrUpdateUser([FromBody] UserInfoDto userInfo)
    {
        if (userInfo == null || string.IsNullOrEmpty(userInfo.Username))
        {
            return BadRequest(new { status = "error", message = "Invalid user information." });
        }

        try
        {
             _ldapService.AddOrUpdateUser(userInfo.Username); // Asegúrate de que este método exista en tu servicio
            return Ok(new { status = "success", message = "User added or updated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }

}

