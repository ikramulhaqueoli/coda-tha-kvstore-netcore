using KvStore.Core.Domain.Exceptions;

namespace KvStore.Core.Domain.Validation;

public static class KeyValidator
{
    public static void EnsureValid(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidKeyException(key);
        }

        foreach (var c in key)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
            {
                continue;
            }

            throw new InvalidKeyException(key);
        }
    }
}

