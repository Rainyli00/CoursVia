namespace CoursVia.Services;

public class PasswordService
{
    // Kullanıcı şifresini BCrypt ile tek yönlü hash'e çevirir.
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    // Girilen şifreyi veritabanındaki BCrypt hash değeriyle karşılaştırır.
    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
