using System.Net;

namespace Sufficit.Gateway.Gravatar
{
    /// <summary>
    ///     Options for the Gravatar gateway, bound to the configuration section
    ///     <see cref="SectionName"/> ("Sufficit:Gateway:Gravatar").
    /// </summary>
    public sealed class GravatarOptions
    {
        /// <summary>
        ///     Configuration section name.
        /// </summary>
        public const string SectionName = "Sufficit:Gateway:Gravatar";

        /// <summary>
        ///     Avatar endpoint with a {0} placeholder for the hash.
        /// </summary>
        public string AvatarBaseUrl { get; set; }
            = "https://www.gravatar.com/avatar/{0}";

        /// <summary>
        ///     Public profile endpoint with a {0} placeholder for the hash.
        /// </summary>
        public string ProfileBaseUrl { get; set; }
            = "https://www.gravatar.com/{0}.json";

        /// <summary>
        ///     Requested square avatar size, in pixels (1..2048).
        /// </summary>
        public uint AvatarSize { get; set; }
            = 230;

        /// <summary>
        ///     Status code that indicates "no avatar for this hash".
        ///     The gateway always sends default=404 and treats this status as absence.
        /// </summary>
        public HttpStatusCode AvatarDefaultStatusCode { get; set; }
            = HttpStatusCode.NotFound;

        /// <summary>
        ///     Optional User-Agent sent by the registered HttpClient.
        /// </summary>
        public string UserAgent { get; set; }
            = "SufficitGravatarGateway/1.0 (+https://github.com/sufficit/sufficit-gateway-gravatar)";
    }
}
