using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace ProductsImport.Models;

public class ConnectionProfile
{
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public bool UseWindowsAuth { get; set; }
    public string Login { get; set; } = string.Empty;

    /// <summary>Base64 DPAPI-protected password. Empty when using Windows auth.</summary>
    public string EncryptedPassword { get; set; } = string.Empty;

    [JsonIgnore]
    public string Password
    {
        get => Unprotect(EncryptedPassword);
        set => EncryptedPassword = Protect(value);
    }

    public string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Server,
            InitialCatalog = Database,
            TrustServerCertificate = true,
            ConnectTimeout = 15
        };

        if (UseWindowsAuth)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = Login;
            builder.Password = Password;
        }

        return builder.ConnectionString;
    }

    public override string ToString() => $"{Name} ({Server}/{Database})";

    private static string Protect(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
        {
            return string.Empty;
        }

        try
        {
            var protectedBytes = Convert.FromBase64String(encrypted);
            var bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            // Password was encrypted by a different user/machine profile and cannot be recovered.
            return string.Empty;
        }
    }
}
