using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sufficit.Gateway.Gravatar
{
    /// <summary>
    ///     Typed client for the public Gravatar APIs (avatar and profile).
    /// </summary>
    public interface IGravatarClient
    {
        /// <summary>
        ///     Downloads the avatar image for an e-mail address.
        ///     Returns null when the e-mail has no Gravatar avatar.
        /// </summary>
        /// <param name="email">e-mail address; normalized internally</param>
        /// <param name="size">optional square size override; falls back to options</param>
        /// <param name="defaultStatus">optional absence status override (sent as default=); falls back to options</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task<GravatarAvatar?> GetAvatarByEmailAsync(string email, uint? size = null, HttpStatusCode? defaultStatus = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Downloads the avatar image for a MD5 or SHA-256 hash.
        ///     Returns null when the hash has no Gravatar avatar.
        /// </summary>
        /// <param name="hash">MD5 (32 hex chars) or SHA-256 (64 hex chars) hash</param>
        /// <param name="size">optional square size override; falls back to options</param>
        /// <param name="defaultStatus">optional absence status override (sent as default=); falls back to options</param>
        /// <param name="cancellationToken">cancellation token</param>
        Task<GravatarAvatar?> GetAvatarByHashAsync(string hash, uint? size = null, HttpStatusCode? defaultStatus = null, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets the public profile for an e-mail address.
        ///     Returns null when the e-mail has no public Gravatar profile.
        /// </summary>
        Task<GravatarProfile?> GetProfileByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Gets the public profile for a MD5 or SHA-256 hash.
        ///     Returns null when the hash has no public Gravatar profile.
        /// </summary>
        Task<GravatarProfile?> GetProfileByHashAsync(string hash, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Combined presence lookup for an e-mail: avatar + profile in a single call.
        /// </summary>
        Task<GravatarSearchResult> SearchByEmailAsync(string email, CancellationToken cancellationToken = default);
    }

    /// <summary>
    ///     Default <see cref="IGravatarClient"/> implementation over a shared HttpClient.
    ///     Absence (no avatar / no profile) is reported as null; transport failures
    ///     propagate as exceptions so callers can map them to 5xx responses.
    /// </summary>
    public sealed class GravatarClient : IGravatarClient
    {
        /// <summary>
        ///     Logical name of the HttpClient registered by AddSufficitGatewayGravatar.
        /// </summary>
        public const string HttpClientName = "Sufficit.Gateway.Gravatar";

        private static readonly JsonSerializerOptions JsonOptions
            = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly IOptionsMonitor<GravatarOptions>? _optionsMonitor;
        private readonly GravatarOptions? _staticOptions;
        private readonly ILogger _logger;

        /// <summary>
        ///     DI-friendly constructor (typed client): reads live options from the monitor.
        /// </summary>
        public GravatarClient(
            HttpClient httpClient,
            IOptionsMonitor<GravatarOptions>? optionsMonitor = null,
            ILogger<GravatarClient>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _optionsMonitor = optionsMonitor;
            _logger = (ILogger?)logger ?? NullLogger.Instance;
        }

        /// <summary>
        ///     Manual constructor for hosts without DI: static options snapshot.
        /// </summary>
        public GravatarClient(
            HttpClient httpClient,
            GravatarOptions options,
            ILogger<GravatarClient>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _staticOptions = options ?? throw new ArgumentNullException(nameof(options));
            _logger = (ILogger?)logger ?? NullLogger.Instance;
        }

        private GravatarOptions CurrentOptions
            => _optionsMonitor?.CurrentValue ?? _staticOptions ?? new GravatarOptions();

        public async Task<GravatarAvatar?> GetAvatarByEmailAsync(string email, uint? size = null, HttpStatusCode? defaultStatus = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await GetAvatarByHashAsync(GravatarHash.Sha256(email), size, defaultStatus, cancellationToken).ConfigureAwait(false);
        }

        public async Task<GravatarAvatar?> GetAvatarByHashAsync(string hash, uint? size = null, HttpStatusCode? defaultStatus = null, CancellationToken cancellationToken = default)
        {
            if (!GravatarHash.IsHash(hash))
                throw new ArgumentException("invalid gravatar hash (expected 32 or 64 hex chars)", nameof(hash));

            var options = CurrentOptions;
            var absence = defaultStatus ?? options.AvatarDefaultStatusCode;
            var url = BuildAvatarUrl(hash, size, absence, options);
            _logger.LogTrace("gravatar avatar request: {url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyUserAgent(request, options);

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == absence)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("gravatar avatar request failed for hash {hash}: {status} {reason}",
                    hash, (int)response.StatusCode, response.ReasonPhrase);
                response.EnsureSuccessStatusCode();
            }

            var content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            if (content == null || content.Length == 0)
                return null;

            return new GravatarAvatar
            {
                Hash = hash,
                Content = content,
                ContentType = response.Content.Headers.ContentType?.MediaType,
                LastModified = response.Content.Headers.LastModified?.UtcDateTime
            };
        }

        public async Task<GravatarProfile?> GetProfileByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await GetProfileByHashAsync(GravatarHash.Sha256(email), cancellationToken).ConfigureAwait(false);
        }

        public async Task<GravatarProfile?> GetProfileByHashAsync(string hash, CancellationToken cancellationToken = default)
        {
            if (!GravatarHash.IsHash(hash))
                throw new ArgumentException("invalid gravatar hash (expected 32 or 64 hex chars)", nameof(hash));

            var options = CurrentOptions;
            var url = string.Format(options.ProfileBaseUrl, hash);
            _logger.LogTrace("gravatar profile request: {url}", url);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyUserAgent(request, options);

            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("gravatar profile request failed for hash {hash}: {status} {reason}",
                    hash, (int)response.StatusCode, response.ReasonPhrase);
                response.EnsureSuccessStatusCode();
            }

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var document = await JsonSerializer
                .DeserializeAsync<GravatarProfileResponse>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (document == null || document.Entry == null || document.Entry.Count == 0)
                return null;

            var profile = document.Entry[0];
            if (string.IsNullOrEmpty(profile.Hash) && !string.IsNullOrEmpty(document.Hash))
                profile.Hash = document.Hash;
            return profile;
        }

        public async Task<GravatarSearchResult> SearchByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = GravatarHash.Normalize(email);
            var result = new GravatarSearchResult
            {
                Email = normalized,
                Hash = GravatarHash.Sha256(normalized),
                HashMd5 = GravatarHash.Md5(normalized)
            };

            if (string.IsNullOrWhiteSpace(normalized))
                return result;

            result.Avatar = await GetAvatarByEmailAsync(normalized, null, null, cancellationToken).ConfigureAwait(false);
            result.HasAvatar = result.Avatar != null;

            result.Profile = await GetProfileByEmailAsync(normalized, cancellationToken).ConfigureAwait(false);
            result.HasProfile = result.Profile != null;

            return result;
        }

        private static string BuildAvatarUrl(string hash, uint? size, HttpStatusCode absence, GravatarOptions options)
        {
            var effectiveSize = size.HasValue && size.Value > 0 ? size.Value : options.AvatarSize;
            return string.Format(options.AvatarBaseUrl, hash)
                + "?s=" + effectiveSize
                + "&d=" + (int)absence;
        }

        private static void ApplyUserAgent(HttpRequestMessage request, GravatarOptions options)
        {
            if (!string.IsNullOrEmpty(options.UserAgent))
                request.Headers.TryAddWithoutValidation("User-Agent", options.UserAgent);
        }
    }
}
