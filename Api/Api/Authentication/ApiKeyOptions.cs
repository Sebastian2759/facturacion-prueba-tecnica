using Microsoft.AspNetCore.Authentication;

namespace Api.Authentication;

/// <summary>
/// Opciones de configuración para el esquema de autenticación por API Key.
/// Se configura en appsettings.json bajo la sección "ApiKey".
/// </summary>
public sealed class ApiKeyOptions : AuthenticationSchemeOptions
{
    /// <summary>Nombre del header HTTP donde se envía la clave. Por defecto: X-Api-Key</summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>Clave esperada. Configura en appsettings.json → ApiKey:Key</summary>
    public string Key { get; set; } = string.Empty;
}
