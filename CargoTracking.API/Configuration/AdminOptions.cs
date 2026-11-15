namespace CargoTracking.API.Configuration;

public sealed class AdminOptions
{
    // secret.json a bilgileri yazdık appsettingse tanımlama yapmayacağız ki githuba yüklendiği zaman bilgiler gözükmesin.
    public const string SectionName = "InitialAdmin";

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
