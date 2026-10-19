using System;
using System.Security.Cryptography;
using Xunit;

namespace Asv.Common.Test.Other;

public class PasswordHashTest
{
    [Theory]
    [InlineData("password", "1:AAECAwQFBgcICQoLDA0ODxAREhMUFRYX:Yw1VJ4OcjBaeRIjBDCmezbaZp5k=")]
    [InlineData("", "1000:AAECAwQFBgcICQoLDA0ODxAREhMUFRYX:E1T58XMnJMafYHfOYWngtKtdmkXzqkeT")]
    [InlineData("Пароль🔑", "4096:AAECAwQFBgcICQoLDA0ODxAREhMUFRYX:kLl2Y+HpmaeeZclY9+bVtkl96IpirAILPZ8IgirTfng=")]
    [InlineData("nul\0password", "1500:AAECAwQFBgcICQoLDA0ODxAREhMUFRYX:GCenmxG6IALaHydO1xP5vw==")]
    public void ValidatePassword_AcceptsLegacyVectors_RejectsWrongPassword(string password, string hash)
    {
        Assert.True(PasswordHash.ValidatePassword(password, hash));
        Assert.False(PasswordHash.ValidatePassword(password + "wrong", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("password")]
    [InlineData("Пароль🔑")]
    [InlineData("nul\0password")]
    [InlineData("invalid\uD800")]
    public void CreateHash_PreservesLegacyFormatAndDerivation(string password)
    {
        var hash = PasswordHash.CreateHash(password);
        var parts = hash.Split(':');
        Assert.Equal(3, parts.Length);
        Assert.Equal("1000", parts[0]);
        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        Assert.Equal(24, salt.Length);
        Assert.Equal(24, expected.Length);

#pragma warning disable SYSLIB0060 // Verify compatibility against the original implementation.
        using var legacy = new Rfc2898DeriveBytes(password, salt);
#pragma warning restore SYSLIB0060
        legacy.IterationCount = int.Parse(parts[0]);
        Assert.Equal(expected, legacy.GetBytes(expected.Length));
        Assert.True(PasswordHash.ValidatePassword(password, hash));
        Assert.False(PasswordHash.ValidatePassword(password + "wrong", hash));
        Assert.NotEqual(hash, PasswordHash.CreateHash(password));
    }
}
