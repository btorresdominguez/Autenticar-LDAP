using System;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }


    // Esta propiedad no está en la tabla,  joins o mapeos personalizados
    public List<string> Roles { get; set; } = new();
}