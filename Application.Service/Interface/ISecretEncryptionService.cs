using System.Threading.Tasks;

namespace Application.Service.Interface
{
    /// <summary>
    /// Provides reversible encryption for secrets using an application-level key.
    /// </summary>
    public interface ISecretEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
