using Application.DTOs;
using Application.Interfaces;
using Domain.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    public class JwtTokenHelper : IJwtTokenHelper
    {
        private readonly IConfiguration _config;

        public JwtTokenHelper(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(UserInfoDto userInfo)
        {
            // Crear claims personalizados con la información del usuario
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userInfo.Username),
            new Claim(ClaimTypes.Email, userInfo.Email ?? ""),
            new Claim("DisplayName", userInfo.DisplayName ?? ""),
            new Claim("Department", userInfo.Department ?? ""),
            new Claim("Title", userInfo.Title ?? "")
            
        };


            // Crear clave de firma y credenciales
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Crear el token
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            // Retornar el token generado en formato string
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

}

