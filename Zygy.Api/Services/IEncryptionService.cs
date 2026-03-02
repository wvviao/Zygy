using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Zygy.Api.Services;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] data);

    [return: NotNullIfNotNull(nameof(text))]
    string? DecryptFromBase64String(string? text) =>
        text is null ? text : Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(text)));

    [return: NotNullIfNotNull(nameof(text))]
    string? EncryptToBase64String(string? text) =>
        text is null ? text : Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(text)));
}
