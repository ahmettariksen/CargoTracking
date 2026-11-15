namespace CargoTracking.API.Configuration;

public sealed class JwtOptions
{
    //Issuer: Token'ı üreten uygulamayı belirtir.
    //Audience: Token'ın hangi uygulama veya hedef için üretildiğini belirtir.
    //SecretKey: Token'ın imzalanmasında kullanılan gizli anahtardır.
    //ExpirationInMinutes: Token'ın kaç dakika geçerli olacağını belirler. Şimdilik 30 dakika kullanacağız.

    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    public int ExpirationInMinutes { get; set; } = 30;
}
