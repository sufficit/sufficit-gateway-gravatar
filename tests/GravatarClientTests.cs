using Sufficit.Gateway.Gravatar;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public HttpRequestMessage? LastRequest { get; private set; }

    public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        => _respond = respond;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(_respond(request));
    }
}

public class GravatarClientTests
{
    private const string Sha256OfTest = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

    private static GravatarClient CreateClient(StubHandler handler)
        => new GravatarClient(new HttpClient(handler), new GravatarOptions());

    [Fact]
    public async Task GetAvatarByEmailAsync_ReturnsNull_OnConfiguredDefaultStatus()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        var avatar = await client.GetAvatarByEmailAsync("test");

        Assert.Null(avatar);
        Assert.Contains("d=404", handler.LastRequest!.RequestUri!.Query);
        Assert.Contains("s=230", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetAvatarByEmailAsync_ReturnsContent_OnSuccess()
    {
        var bytes = new byte[] { 1, 2, 3 };
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        });
        var client = CreateClient(handler);

        var avatar = await client.GetAvatarByEmailAsync("test");

        Assert.NotNull(avatar);
        Assert.Equal(bytes, avatar!.Content);
        Assert.Equal(Sha256OfTest, avatar.Hash);
    }

    [Fact]
    public async Task GetAvatarByHashAsync_Throws_OnInvalidHash()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetAvatarByHashAsync("zzz"));
    }

    [Fact]
    public async Task GetProfileByEmailAsync_ParsesEntry()
    {
        var json = "{\"entry\":[{\"hash\":\"abc\",\"preferredUsername\":\"someone\",\"profileUrl\":\"https://gravatar.com/someone\"}]}";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var client = CreateClient(handler);

        var profile = await client.GetProfileByEmailAsync("test");

        Assert.NotNull(profile);
        Assert.Equal("someone", profile!.PreferredUsername);
        Assert.Equal("abc", profile.Hash);
    }

    [Fact]
    public async Task GetProfileByHashAsync_ReturnsNull_On404()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var client = CreateClient(handler);

        var profile = await client.GetProfileByHashAsync(Sha256OfTest);

        Assert.Null(profile);
    }

    [Fact]
    public async Task SearchByEmailAsync_CombinesAvatarAndProfile()
    {
        var json = "{\"entry\":[{\"hash\":\"abc\",\"preferredUsername\":\"someone\"}]}";
        var calls = 0;
        var handler = new StubHandler(_ =>
        {
            calls++;
            if (calls == 1)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 9, 9 })
                };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var client = CreateClient(handler);

        var result = await client.SearchByEmailAsync("TEST");

        Assert.True(result.HasAvatar);
        Assert.True(result.HasProfile);
        Assert.NotNull(result.Avatar);
        Assert.NotNull(result.Profile);
        Assert.Equal("test", result.Email);
    }
}

public class GravatarClientDefaultStatusTests
{
    [Fact]
    public async Task GetAvatarByEmailAsync_HonorsDefaultStatusOverride()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = new GravatarClient(new HttpClient(handler), new GravatarOptions());

        var avatar = await client.GetAvatarByEmailAsync("test", null, HttpStatusCode.NoContent);

        Assert.Null(avatar);
        Assert.Contains("d=204", handler.LastRequest!.RequestUri!.Query);
    }
}
