using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sufficit.Gateway.Gravatar
{
    /// <summary>
    ///     Avatar image bytes plus the metadata exposed by the Gravatar response.
    /// </summary>
    public sealed class GravatarAvatar
    {
        /// <summary>
        ///     Hash used to request the avatar.
        /// </summary>
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        ///     Image content.
        /// </summary>
        [JsonPropertyName("content")]
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        ///     Media type of the image (usually image/jpeg or image/png).
        /// </summary>
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        /// <summary>
        ///     Last modification reported by Gravatar, for caching purposes.
        /// </summary>
        [JsonPropertyName("lastModified")]
        public DateTime? LastModified { get; set; }
    }

    /// <summary>
    ///     A public photo declared by a Gravatar profile.
    /// </summary>
    public sealed class GravatarPhoto
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    /// <summary>
    ///     A verified/declared account (GitHub, Twitter, ...) of a Gravatar profile.
    /// </summary>
    public sealed class GravatarAccount
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("shortname")]
        public string Shortname { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Public profile entry returned by the Gravatar profile API.
    /// </summary>
    public sealed class GravatarProfile
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("requestHash")]
        public string RequestHash { get; set; } = string.Empty;

        [JsonPropertyName("profileUrl")]
        public string ProfileUrl { get; set; } = string.Empty;

        [JsonPropertyName("preferredUsername")]
        public string PreferredUsername { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("aboutMe")]
        public string? AboutMe { get; set; }

        [JsonPropertyName("currentLocation")]
        public string? CurrentLocation { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("photos")]
        public IList<GravatarPhoto> Photos { get; set; } = new List<GravatarPhoto>();

        [JsonPropertyName("accounts")]
        public IList<GravatarAccount> Accounts { get; set; } = new List<GravatarAccount>();
    }

    /// <summary>
    ///     Root document of the Gravatar profile API.
    /// </summary>
    public sealed class GravatarProfileResponse
    {
        [JsonPropertyName("hash")]
        public string? Hash { get; set; }

        [JsonPropertyName("entry")]
        public IList<GravatarProfile> Entry { get; set; } = new List<GravatarProfile>();
    }

    /// <summary>
    ///     Combined presence result for an e-mail address.
    /// </summary>
    public sealed class GravatarSearchResult
    {
        /// <summary>
        ///     Normalized e-mail that was searched.
        /// </summary>
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        ///     SHA-256 hash of the e-mail (modern Gravatar identifier).
        /// </summary>
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        /// <summary>
        ///     Legacy MD5 hash of the e-mail.
        /// </summary>
        [JsonPropertyName("hashMd5")]
        public string HashMd5 { get; set; } = string.Empty;

        [JsonPropertyName("hasAvatar")]
        public bool HasAvatar { get; set; }

        [JsonPropertyName("hasProfile")]
        public bool HasProfile { get; set; }

        [JsonPropertyName("avatar")]
        public GravatarAvatar? Avatar { get; set; }

        [JsonPropertyName("profile")]
        public GravatarProfile? Profile { get; set; }
    }
}
