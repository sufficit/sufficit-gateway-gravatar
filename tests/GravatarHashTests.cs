using Sufficit.Gateway.Gravatar;
using Xunit;

public class GravatarHashTests
{
    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("test", "098f6bcd4621d373cade4e832627b4f6", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    public void Hashes_Match_Known_Vectors(string input, string expectedMd5, string expectedSha256)
    {
        Assert.Equal(expectedMd5, GravatarHash.Md5(input));
        Assert.Equal(expectedSha256, GravatarHash.Sha256(input));
    }

    [Fact]
    public void Normalize_Trims_And_Lowercases()
    {
        Assert.Equal("user@example.com", GravatarHash.Normalize("  User@Example.COM "));
    }

    [Theory]
    [InlineData("d41d8cd98f00b204e9800998ecf8427e", true)]   // md5
    [InlineData("E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", true)] // sha256 (uppercase)
    [InlineData("d41d8cd98f00b204e9800998ecf8427g", false)]  // non-hex char
    [InlineData("d41d8cd98f00b204e9800998ecf8427", false)]   // 31 chars
    [InlineData("", false)]
    [InlineData("not-a-hash", false)]
    public void IsHash_Validates_Hex_Length(string candidate, bool expected)
    {
        Assert.Equal(expected, GravatarHash.IsHash(candidate));
    }
}
