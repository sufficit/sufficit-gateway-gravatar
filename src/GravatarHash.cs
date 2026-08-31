using System.Security.Cryptography;
using System.Text;

namespace Sufficit.Gateway.Gravatar
{
    /// <summary>
    ///     Hash utilities required by the Gravatar public API.
    ///     Gravatar accepts MD5 (legacy) and SHA-256 hashes of the normalized e-mail address.
    /// </summary>
    public static class GravatarHash
    {
        /// <summary>
        ///     Normalizes an e-mail address for hashing: trim + lowercase invariant.
        /// </summary>
        public static string Normalize(string email)
            => (email ?? string.Empty).Trim().ToLowerInvariant();

        /// <summary>
        ///     Computes the legacy MD5 hash (32 lowercase hex chars) of the normalized e-mail.
        /// </summary>
        public static string Md5(string email)
        {
            using (var md5 = MD5.Create())
                return ToHex(md5.ComputeHash(Encoding.UTF8.GetBytes(Normalize(email))));
        }

        /// <summary>
        ///     Computes the SHA-256 hash (64 lowercase hex chars) of the normalized e-mail.
        /// </summary>
        public static string Sha256(string email)
        {
            using (var sha = SHA256.Create())
                return ToHex(sha.ComputeHash(Encoding.UTF8.GetBytes(Normalize(email))));
        }

        /// <summary>
        ///     Checks whether the informed string looks like a Gravatar hash
        ///     (32 hex chars for MD5 or 64 hex chars for SHA-256).
        ///     Use it to validate user input before building request URLs.
        /// </summary>
        public static bool IsHash(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            if (hash.Length != 32 && hash.Length != 64)
                return false;

            for (var i = 0; i < hash.Length; i++)
            {
                var c = hash[i];
                var isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex)
                    return false;
            }
            return true;
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            for (var i = 0; i < bytes.Length; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }
    }
}
