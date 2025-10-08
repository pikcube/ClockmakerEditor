using Octokit;

namespace Clockmaker0.Data;

public class TokenData
{
    public string? TokenType { get; init; }
    public string? AccessToken { get; init; }
    public int ExpiresIn { get; init; }
    public string? RefreshToken { get; init; }
    public int RefreshTokenExpiresIn { get; init; }
    public string[]? Scope { get; init; }
    public string? Error { get; init; }
    public string? ErrorDescription { get; init; }
    public string? ErrorUri { get; init; }

    public TokenData()
    {
    }

    public TokenData(OauthToken token)
    {
        TokenType = token.TokenType;
        AccessToken = token.AccessToken;
        ExpiresIn = token.ExpiresIn;
        RefreshToken = token.RefreshToken;
        RefreshTokenExpiresIn = token.RefreshTokenExpiresIn;
        Scope = [.. token.Scope];
        Error = token.Error;
        ErrorDescription = token.ErrorDescription;
        ErrorUri = token.ErrorUri;
    }
}