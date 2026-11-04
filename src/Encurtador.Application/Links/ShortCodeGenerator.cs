using System.Security.Cryptography;

namespace Encurtador.Application.Links;

public sealed class ShortCodeGenerator : IShortCodeGenerator
{
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTVWXYZ";
    public const int CodeLength = 7;

    public string Generate()
    {
        return string.Create(CodeLength, Alphabet, static (code, alphabet) =>
        {
            for (var index = 0; index < code.Length; index++)
            {
                code[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }
        });
    }
}
