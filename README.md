# Autenticar-LDAP
API REST desarrollada en .NET para autenticar usuarios contra un servidor OpenLDAP utilizando contenedores Docker. 

## Estructura del Proyecto
LDAPAuthAPI/
│
├── openldap-docker/ # Configuración del servidor OpenLDAP en Docker
│ └── docker-compose.yml
│
└── src/ # Proyecto de la solución .NET
├── WebApi/ # API principal (Program.cs, Controladores)
│ └── WebApi.csproj
├── Application/ # Lógica de negocio
│ └── Application.csproj
├── Infrastructure/ # Acceso a LDAP y servicios externos
│ └── Infrastructure.csproj
└── Domain/ # Entidades y contratos
└── Domain.csproj


## Cómo Ejecutar el Proyecto

### 1. Prerrequisitos

- Docker & Docker Compose
- .NET 9.0 

### 2. Levantar el servidor LDAP

Desde la carpeta raíz:

bash
cd openldap-docker
docker-compose up -d
Esto iniciará un contenedor OpenLDAP accesible en ldap://localhost:1389.

### 3. Correr la API .NET

La API quedará disponible en  http://localhost:5000 
dotnet restore
dotnet build
dotnet run

 ## Usuarios de Prueba (LDAP)

 Administrador:

DN: cn=admin,dc=miempresa,dc=com
Password: adminpassword

| Usuario | UID     | Contraseña | DN                                       |
| ------- | ------- | ---------- | ---------------------------------------- |
| user01  | user01  | password1  | cn=user01,ou=users,dc=miempresa,dc=com   |
| user02  | user02  | password2  | cn=user02,ou=users,dc=miempresa,dc=com   |
| jdoe    | jdoe    | secret123  | uid=jdoe,ou=users,dc=miempresa,dc=com    |
| janedoe | janedoe | secret456  | uid=janedoe,ou=users,dc=miempresa,dc=com |

### Endpoints de la API
## POST /api/Authenticate/authenticate
Autentica un usuario con sus credenciales LDAP.

POST /api/Authenticate/authenticate
Content-Type: application/json

## Solicitud
{
  "Username": "jdoe",
  "Password": "secret123"
}

## Respuesta esperada:
{
    "status": "success",
    "user": {
        "username": "jdoe",
        "email": "jdoe@example.com",
        "displayName": "John Doe",
        "department": "jdoe@example.com",
        "title": "Software Engineer"
    }
## Respuesta fallida (credenciales incorrectas)

{
  "status": "error",
  "message": "Invalid credentials"
}




