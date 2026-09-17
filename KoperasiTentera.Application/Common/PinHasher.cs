using BCrypt.Net;

namespace KoperasiTentera.Application.Common;

public static class PinHasher
{
    public static string Hash(string pin) => BCrypt.Net.BCrypt.HashPassword(pin);

    public static bool Verify(string pin, string hash) => BCrypt.Net.BCrypt.Verify(pin, hash);
}
