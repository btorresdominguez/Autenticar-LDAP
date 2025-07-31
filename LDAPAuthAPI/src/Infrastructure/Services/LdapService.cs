using Application.DTOs;
using Application.Interfaces;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class LdapService : ILdapService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LdapService> _logger;
        private readonly ILoginInfoRepository _loginRepo;

        // Variables de conexión LDAP
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useSsl;
        private readonly string _adminDn;
        private readonly string _adminPassword;
        private readonly string _userSearchBase;

        public LdapService(IConfiguration config, ILogger<LdapService> logger, ILoginInfoRepository loginRepo)
        {
            _config = config;
            _logger = logger;
            _loginRepo = loginRepo;

            // Inicializar variables de conexión
            _host = _config["Ldap:Host"];
            _port = int.Parse(_config["Ldap:Port"]);
            _useSsl = bool.TryParse(_config["Ldap:UseSSL"], out var ssl) && ssl;
            _adminDn = _config["Ldap:AdminDn"];
            _adminPassword = _config["Ldap:AdminPassword"];
            _userSearchBase = _config["Ldap:UserSearchBase"];

        }

        public async Task<UserInfoDto?> ValidateCredentialsAsync(string username, string password)
        {
            using var adminConnection = new LdapConnection();

            try
            {
                _logger.LogInformation("Iniciando conexión LDAP para el usuario: {Username}", username);

                if (_useSsl)
                {
                    adminConnection.SecureSocketLayer = true;
                }

                adminConnection.Constraints = new LdapConstraints
                {
                    TimeLimit = 5000, // 5 segundos
                    ReferralFollowing = true
                };

                // Intentar conectarse al servidor LDAP
                adminConnection.Connect(_host, _port);
                _logger.LogInformation("Conexión LDAP exitosa");

                // Intentar autenticar como el administrador
                adminConnection.Bind(_adminDn, _adminPassword);
                _logger.LogInformation("Autenticación del administrador LDAP exitosa");

                // Buscar el usuario y obtener sus atributos
                var userInfo = await FindUserByUsername(username);
                if (userInfo == null)
                {
                    _logger.LogWarning("Usuario {Username} no encontrado en LDAP", username);
                    await SaveLoginError(username, "Usuario no encontrado en LDAP", "El usuario no existe.");
                    return null; // Retorna null si el usuario no se encuentra
                }

                // Intentar autenticar al usuario usando el DN encontrado
                try
                {
                    var userDn = $"uid={username},{_userSearchBase}"; // Usar el DN del usuario
                    adminConnection.Bind(userDn, password); // Autenticarse con el DN del usuario
                    _logger.LogInformation("Credenciales válidas para el usuario {Username}", username);
                }
                catch (LdapException authEx)
                {
                    _logger.LogWarning(authEx, "Credenciales inválidas para {Username}", username);
                    await SaveLoginError(username, "Credenciales inválidas", authEx.Message);
                    return null; // Retorna null si las credenciales son inválidas
                }

                // Ya tenemos los atributos del usuario, solo retornamos
                return userInfo; // userInfo ya contiene todos los atributos necesarios
            }
            catch (LdapException ex)
            {
                _logger.LogError(ex, "Error LDAP durante la autenticación de {Username}", username);
                await SaveLoginError(username, "Error LDAP durante la autenticación", ex.Message);
                return null; // Retorna null en caso de error de LDAP
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante la autenticación de {Username}", username);
                await SaveLoginError(username, "Error inesperado durante la autenticación", ex.Message);
                return null; // Retorna null en caso de error inesperado
            }
        }


        // Método para guardar el error en la base de datos
        private async Task SaveLoginError(string username, string errorTitle, string errorMessage)
        {
            var jsonError = JsonSerializer.Serialize(new { status = "error", message = errorMessage });
            await _loginRepo.SaveLoginInfoAsync(username, string.Empty, jsonError, errorTitle);
        }
    


        public async Task<string> TestLdapConnection()
        {
            using var ldapConnection = new LdapConnection();
            try
            {
                ldapConnection.Connect(_host, _port);
                ldapConnection.Bind(_adminDn, _adminPassword); // Usar AdminDn y AdminPassword desde la configuración
                return "Conexión y autenticación LDAP exitosa";
            }
            catch (LdapException ex)
            {
                throw new Exception($"Error de LDAP: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}");
            }
        }

        public async Task<UserInfoDto?> FindUserByUsername(string username)
        {
            using var ldapConnection = new LdapConnection();
            try
            {
                // Conectar al servidor LDAP
                ldapConnection.Connect(_host, _port);
                ldapConnection.Bind(_adminDn, _adminPassword); // Usar AdminDn y AdminPassword desde la configuración

                var searchBase = _userSearchBase; // Usar la configuración de UserSearchBase
                var searchFilter = $"(uid={username})"; // Filtrar por el uid proporcionado
                var searchResults = ldapConnection.Search(searchBase, LdapConnection.SCOPE_SUB, searchFilter, null, false);

                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    return new UserInfoDto
                    {
                        Username = entry.getAttribute("uid")?.StringValue,
                        Email = entry.getAttribute("mail")?.StringValue,
                        DisplayName = entry.getAttribute("cn")?.StringValue,
                        Department = entry.getAttribute("department")?.StringValue,
                        Title = entry.getAttribute("title")?.StringValue
                    };
                }
            }
            catch (LdapException ex)
            {
                throw new Exception($"Error de LDAP: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado: {ex.Message}");
            }

            return null; // Si no se encuentra el usuario
        }

        public void AddOrUpdateUser(string username)
        {
            using var connection = new LdapConnection();
            try
            {
                // Conectar y autenticar al servidor LDAP
                connection.Connect(_host, _port);
                connection.Bind(_adminDn, _adminPassword);

                var dn = $"uid={username},ou=users,dc=miempresa,dc=com";

                var entry = new LdapEntry(
                    dn,
                    new LdapAttributeSet
                    {
                    new LdapAttribute("objectClass", new[] { "top", "person", "organizationalPerson", "inetOrgPerson" }),
                    new LdapAttribute("cn", "John Doe"),
                    new LdapAttribute("sn", "Doe"),
                    new LdapAttribute("uid", username),
                    new LdapAttribute("userPassword", "secret123"),
                    new LdapAttribute("mail", "jdoe@example.com"),
                    new LdapAttribute("department", "IT"), // Asegúrate de que el atributo esté definido
                    new LdapAttribute("title", "Software Engineer") // Asegúrate de que el atributo esté definido
                    }
                );

                // Intentar agregar el usuario
                try
                {
                    connection.Add(entry);
                    Console.WriteLine("Usuario agregado.");
                }
                catch (LdapException ex) when (ex.ResultCode == LdapException.ENTRY_ALREADY_EXISTS)
                {
                    Console.WriteLine("El usuario ya existe. Actualizando atributos...");

                    var modifications = new List<LdapModification>
                {
                    new LdapModification(LdapModification.REPLACE, new LdapAttribute("mail", "jdoe@example.com")),
                    new LdapModification(LdapModification.REPLACE, new LdapAttribute("department", "IT")),
                    new LdapModification(LdapModification.REPLACE, new LdapAttribute("title", "Software Engineer"))
                };

                    connection.Modify(dn, modifications.ToArray());
                    Console.WriteLine("Usuario modificado.");
                }
            }
            catch (LdapException ex)
            {
                Console.WriteLine($"Error de LDAP: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
            }
            finally
            {
                connection.Disconnect();
            }
        }
    }


}