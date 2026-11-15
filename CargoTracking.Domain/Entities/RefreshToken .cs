using CargoTracking.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class RefreshToken:BaseEntity
{
    public Guid UserId { get; private set; }

    // 	Refresh Token'ın güvenli hash değerini saklar.
    public string TokenHash { get; private set; } = default!;

    // Token'ın son geçerlilik zamanını belirler.
    public DateTime ExpiresAt { get; private set; }

    // 	İptal edilme zamanını tutar
    public DateTime? RevokedAt { get; private set; }

    // Token yenilendiğinde yerine üretilen token'ın hash'i.
    public string? ReplacedByTokenHash { get; private set; }
    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired => DateTime.Now >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        string tokenHash,
        DateTime expiresAt)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("TokenHash cannot be empty.", nameof(tokenHash));

        if (expiresAt <= DateTime.Now)
            throw new ArgumentException("Expiration date must be in the future.", nameof(expiresAt));

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }
    public void Revoke(string? replacedByTokenHash = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = DateTime.Now;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
