namespace BanterApp.Api.Data.Entities;

/// <summary>
/// How a pundit desk may be shown in the product.
/// Persona = fictional character; Licensed = credited real take with source URL;
/// PublicationOnly = outlet name only, no individual identity.
/// </summary>
public enum PunditAttributionMode
{
    Persona = 0,
    Licensed = 1,
    PublicationOnly = 2,
}
