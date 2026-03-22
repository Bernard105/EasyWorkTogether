using System.Security.Cryptography;
using System.Text;

namespace WorkspaceStressSystem.Api.Helpers;

public static class TokenGenerator
{
    public static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public static string GenerateShortCode(int size = 8)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(size);
        var result = new char[size];
        for (int i = 0; i < size; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }

    public static string GenerateCode(int size = 8)
    {
        return GenerateShortCode(size);
    }

    public static string Sha256(string raw)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }
}