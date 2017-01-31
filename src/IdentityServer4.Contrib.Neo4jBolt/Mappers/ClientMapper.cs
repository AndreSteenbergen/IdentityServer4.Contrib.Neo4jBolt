using IdentityServer4.Models;
using Neo4j.Driver.V1;
using System.Collections;
using System.Collections.Generic;

namespace IdentityServer4.Contrib.Neo4jBolt.Mappers
{
    public static class ClientMapper
    {
        public static Client ToClient(this INode dbClient)
        {
            var client = new Client
            {
                ClientId = dbClient["ClientId"].As<string>()
            };

            if (dbClient.Properties.ContainsKey("ClientName"))
                client.ClientName = dbClient["ClientName"].As<string>();

            if (dbClient.Properties.ContainsKey("ClientUri"))
                client.ClientUri = dbClient["ClientUri"].As<string>();

            if (dbClient.Properties.ContainsKey("LogoutUri"))
                client.LogoutUri = dbClient["LogoutUri"].As<string>();

            if (dbClient.Properties.ContainsKey("LogoUri"))
                client.ClientUri = dbClient["LogoUri"].As<string>();

            if (dbClient.Properties.ContainsKey("Enabled"))
                client.AllowAccessTokensViaBrowser = dbClient["Enabled"].As<bool>();

            if (dbClient.Properties.ContainsKey("AbsoluteRefreshTokenLifetime"))
                client.AbsoluteRefreshTokenLifetime = dbClient["AbsoluteRefreshTokenLifetime"].As<int>();

            if (dbClient.Properties.ContainsKey("IdentityTokenLifetime"))
                client.IdentityTokenLifetime = dbClient["IdentityTokenLifetime"].As<int>();

            if (dbClient.Properties.ContainsKey("SlidingRefreshTokenLifetime"))
                client.SlidingRefreshTokenLifetime = dbClient["SlidingRefreshTokenLifetime"].As<int>();

            if (dbClient.Properties.ContainsKey("AccessTokenLifetime"))
                client.AccessTokenLifetime = dbClient["AccessTokenLifetime"].As<int>();

            if (dbClient.Properties.ContainsKey("AccessTokenType"))
                client.AccessTokenType = (AccessTokenType)dbClient["AccessTokenType"].As<int>();

            if (dbClient.Properties.ContainsKey("AllowAccessTokensViaBrowser"))
                client.AllowAccessTokensViaBrowser = dbClient["AllowAccessTokensViaBrowser"].As<bool>();

            if (dbClient.Properties.ContainsKey("AllowOfflineAccess"))
                client.AllowOfflineAccess = dbClient["AllowOfflineAccess"].As<bool>();

            if (dbClient.Properties.ContainsKey("AllowPlainTextPkce"))
                client.AllowPlainTextPkce = dbClient["AllowPlainTextPkce"].As<bool>();

            if (dbClient.Properties.ContainsKey("AllowRememberConsent"))
                client.AllowRememberConsent = dbClient["AllowRememberConsent"].As<bool>();

            if (dbClient.Properties.ContainsKey("AlwaysIncludeUserClaimsInIdToken"))
                client.AlwaysIncludeUserClaimsInIdToken = dbClient["AlwaysIncludeUserClaimsInIdToken"].As<bool>();

            if (dbClient.Properties.ContainsKey("AlwaysSendClientClaims"))
                client.AlwaysSendClientClaims = dbClient["AlwaysSendClientClaims"].As<bool>();

            if (dbClient.Properties.ContainsKey("AuthorizationCodeLifetime"))
                client.AuthorizationCodeLifetime = dbClient["AuthorizationCodeLifetime"].As<int>();

            if (dbClient.Properties.ContainsKey("EnableLocalLogin"))
                client.AlwaysSendClientClaims = dbClient["EnableLocalLogin"].As<bool>();

            if (dbClient.Properties.ContainsKey("IncludeJwtId"))
                client.IncludeJwtId = dbClient["IncludeJwtId"].As<bool>();

            if (dbClient.Properties.ContainsKey("LogoutSessionRequired"))
                client.LogoutSessionRequired = dbClient["LogoutSessionRequired"].As<bool>();

            if (dbClient.Properties.ContainsKey("PrefixClientClaims"))
                client.PrefixClientClaims = dbClient["PrefixClientClaims"].As<bool>();

            if (dbClient.Properties.ContainsKey("ProtocolType"))
                client.ProtocolType = dbClient["ProtocolType"].As<string>();

            if (dbClient.Properties.ContainsKey("RefreshTokenExpiration"))
                client.RefreshTokenExpiration = (TokenExpiration)dbClient["RefreshTokenExpiration"].As<int>();

            if (dbClient.Properties.ContainsKey("RefreshTokenUsage"))
                client.RefreshTokenUsage = (TokenUsage)dbClient["RefreshTokenUsage"].As<int>();

            if (dbClient.Properties.ContainsKey("RequireClientSecret"))
                client.RequireClientSecret = dbClient["RequireClientSecret"].As<bool>();

            if (dbClient.Properties.ContainsKey("RequireConsent"))
                client.RequireConsent = dbClient["RequireConsent"].As<bool>();

            if (dbClient.Properties.ContainsKey("RequirePkce"))
                client.RequirePkce = dbClient["RequirePkce"].As<bool>();

            if (dbClient.Properties.ContainsKey("UpdateAccessTokenClaimsOnRefresh"))
                client.UpdateAccessTokenClaimsOnRefresh = dbClient["UpdateAccessTokenClaimsOnRefresh"].As<bool>();

            return client;
        }

        public static IDictionary<string, object> ToDictionary(this Client client)
        {
            return new Dictionary<string, object> {
                { "clientId", client.ClientId },
                { "clientName", client.ClientName },
                { "clientUri", client.ClientUri },
                { "logoutUri", client.LogoutUri },
                { "logoUri", client.LogoUri},
                { "enabled", client.Enabled},
                { "absoluteRefreshTokenLifetime", client.AbsoluteRefreshTokenLifetime },
                { "identityTokenLifetime", client.IdentityTokenLifetime },
                { "slidingRefreshTokenLifetime", client.SlidingRefreshTokenLifetime },
                { "accessTokenLifetime", client.AccessTokenLifetime },
                { "accessTokenType", (int)client.AccessTokenType },
                { "allowAccessTokensViaBrowser", client.AllowAccessTokensViaBrowser },
                { "allowOfflineAccess", client.AllowOfflineAccess },
                { "allowPlainTextPkce", client.AllowPlainTextPkce },
                { "allowRememberConsent", client.AllowRememberConsent },
                { "alwaysIncludeUserClaimsInIdToken", client.AlwaysIncludeUserClaimsInIdToken },
                { "alwaysSendClientClaims", client.AlwaysSendClientClaims },
                { "authorizationCodeLifetime", client.AuthorizationCodeLifetime },
                { "enableLocalLogin", client.EnableLocalLogin },
                { "includeJwtId", client.IncludeJwtId },
                { "logoutSessionRequired", client.LogoutSessionRequired },
                { "prefixClientClaims", client.PrefixClientClaims },
                { "protocolType", client.ProtocolType},
                { "refreshTokenExpiration", (int) client.RefreshTokenExpiration },
                { "refreshTokenUsage", (int) client.RefreshTokenUsage },
                { "requireClientSecret", client.RequireClientSecret },
                { "requireConsent", client.RequireConsent },
                { "requirePkce", client.RequirePkce },
                { "updateAccessTokenClaimsOnRefresh", client.UpdateAccessTokenClaimsOnRefresh }
            };
        }
    }
}
