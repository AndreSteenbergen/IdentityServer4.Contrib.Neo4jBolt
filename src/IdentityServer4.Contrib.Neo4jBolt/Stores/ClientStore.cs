using IdentityServer4.Stores;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Contrib.Neo4jBolt.Interfaces;
using Neo4j.Driver.V1;
using System.Collections.Generic;
using IdentityServer4.Contrib.Neo4jBolt.Mappers;
using IdentityServer4.Contrib.Neo4jBolt.Results;
using System;

namespace IdentityServer4.Contrib.Neo4jBolt.Stores
{
    public class ClientStore : IClientStore, IClientAdminService
    {
        public Task<ClientAdminResult> CreateClient(Client client)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            using (var session = neo4jDriver.Session(AccessMode.Write))
            using (var tx = session.BeginTransaction())
            {
                try
                {
                    //first check if all scopes already exists in the database:
                    var result = tx.Run($@"
                                    UNWIND {{scopes}} AS sc
                                    MATCH (scope:{cfg.ResourceScopeLabel} {{Name : sc}})
                                    RETURN scope.Name AS Name
                                    UNION
                                    UNWIND {{scopes}} AS sc
                                    MATCH (idResource:{cfg.IdentityResourceLabel} {{Name : sc}})
                                    RETURN idResource.Name AS Name
                                    ", new Dictionary<string, object> {
                                        { "scopes", client.AllowedScopes.ToArray() }
                                    }).ToList();
                    //check if all scopes exist in the database
                    var existingScopes = result.Select(r => r["Name"].As<string>()).Distinct().ToList();
                    var clientScopes = client.AllowedScopes.Distinct().ToList();
                    
                    if (clientScopes.Except(existingScopes).Any())
                        throw new Exception("Invalid scopes found");

                    string CreateClientCommand = $@"
                                    CREATE (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    SET
                                        client.ClientName = {{clientName}},
                                        client.ClientUri = {{clientUri}},
                                        client.LogoutUri = {{logoutUri}},
                                        client.LogoUri = {{logoUri}},
                                        client.Enabled = {{enabled}},
                                        client.AbsoluteRefreshTokenLifetime = {{absoluteRefreshTokenLifetime}},
                                        client.IdentityTokenLifetime = {{identityTokenLifetime}},
                                        client.SlidingRefreshTokenLifetime = {{slidingRefreshTokenLifetime}},
                                        client.AccessTokenLifetime = {{accessTokenLifetime}},
                                        client.AccessTokenType = {{accessTokenType}},
                                        client.AllowAccessTokensViaBrowser = {{allowAccessTokensViaBrowser}},
                                        client.AllowOfflineAccess = {{allowOfflineAccess}},
                                        client.AllowPlainTextPkce = {{allowPlainTextPkce}},
                                        client.AllowRememberConsent = {{allowRememberConsent}},
                                        client.AlwaysIncludeUserClaimsInIdToken = {{alwaysIncludeUserClaimsInIdToken}},
                                        client.AlwaysSendClientClaims = {{alwaysSendClientClaims}},
                                        client.AuthorizationCodeLifetime = {{authorizationCodeLifetime}},
                                        client.EnableLocalLogin = {{enableLocalLogin}},
                                        client.IncludeJwtId = {{includeJwtId}},
                                        client.LogoutSessionRequired = {{logoutSessionRequired}},
                                        client.PrefixClientClaims = {{prefixClientClaims}},
                                        client.ProtocolType = {{protocolType}},
                                        client.RefreshTokenExpiration = {{refreshTokenExpiration}},
                                        client.RefreshTokenUsage = {{refreshTokenUsage}},
                                        client.RequireClientSecret = {{requireClientSecret}},
                                        client.RequireConsent = {{requireConsent}},
                                        client.RequirePkce = {{requirePkce}},
                                        client.UpdateAccessTokenClaimsOnRefresh = {{updateAccessTokenClaimsOnRefresh}}";
                    tx.Run(CreateClientCommand, client.ToDictionary());

                    if (client.RedirectUris != null && client.RedirectUris.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{redirectUris}} AS uri
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (redirect:{cfg.ClientRedirectUriLabel} {{RedirectUri : uri}})
                                    CREATE UNIQUE (client)-[:{cfg.HasRedirectUriRelName} {{`Type` : 'RedirectUri'}}]->(redirect)
                                    ", new Dictionary<string, object> {
                                        { "redirectUris", client.RedirectUris.ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.PostLogoutRedirectUris != null && client.PostLogoutRedirectUris.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{redirectUris}} AS uri
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (redirect:{cfg.ClientRedirectUriLabel} {{RedirectUri : uri}})
                                    CREATE UNIQUE (client)-[:{cfg.HasRedirectUriRelName} {{`Type` : 'PostLogoutRedirectUri'}}]->(redirect)
                                    ", new Dictionary<string, object> {
                                        { "redirectUris", client.PostLogoutRedirectUris.ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.AllowedCorsOrigins != null && client.AllowedCorsOrigins.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{corsOrigins}} AS uri
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (corsorigin:{cfg.ClientCorsOriginLabel} {{CorsOrigin : uri}})
                                    CREATE UNIQUE (client)-[:{cfg.HasCorsOriginsRelName}]->(corsorigin)
                                    ", new Dictionary<string, object> {
                                        { "corsOrigins", client.AllowedCorsOrigins.ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.AllowedGrantTypes != null && client.AllowedGrantTypes.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{grantTypes}} AS gt
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (granttype:{cfg.ClientGrantTypeLabel} {{GrantType : gt}})
                                    CREATE UNIQUE (client)-[:{cfg.HasGrantTypesRelName}]->(granttype)
                                    ", new Dictionary<string, object> {
                                        { "grantTypes", client.AllowedGrantTypes.ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{identityProviderRestrictions}} AS rst
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (restriction:{cfg.IdentityProviderRestrictionLabel} {{IdentityProviderRestriction : rst}})
                                    CREATE UNIQUE (client)-[:{cfg.HasIdentityProviderRestrictionsRelName}]->(restriction)
                                    ", new Dictionary<string, object> {
                                        { "identityProviderRestrictions", client.IdentityProviderRestrictions.ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.AllowedScopes != null && client.AllowedScopes.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{scopes}} AS sc
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MATCH (scope:{cfg.ResourceScopeLabel} {{Name : sc}})
                                    CREATE UNIQUE (client)-[:{cfg.HasScopeRelName}]->(scope)
                                    ", new Dictionary<string, object> {
                                        { "scopes", client.AllowedScopes.ToArray() },
                                        { "clientId", client.ClientId }
                                    });

                        tx.Run($@"
                                    UNWIND {{scopes}} AS sc
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MATCH (idResource:{cfg.IdentityResourceLabel} {{Name : sc}})
                                    CREATE UNIQUE (client)-[:{cfg.HasScopeRelName}]->(idResource)
                                    ", new Dictionary<string, object> {
                                        { "scopes", client.AllowedScopes.ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.Claims != null && client.Claims.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{claims}} AS c
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (claim:{cfg.ClaimLabel} {{Type : {{c.type}}, Value : {{c.value}}}})
                                    CREATE UNIQUE (client)-[:{cfg.HasClaimRelName}]->(claim)
                                    ", new Dictionary<string, object> {
                                        { "claims", client.Claims.Select(cl => cl.ToDictionary()).ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    if (client.ClientSecrets != null && client.ClientSecrets.Any())
                    {
                        tx.Run($@"
                                    UNWIND {{secrets}} AS s
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                                    MERGE (secret:{cfg.SecretLabel} {{Value : {{s.vale}}, Description : {{s.description}}, Expiration : {{s.expiration}}}})
                                    CREATE UNIQUE (client)-[:{cfg.HasSecretRelName}]->(secret)
                                    ", new Dictionary<string, object> {
                                        { "secrets", client.ClientSecrets.Select(s => s.ToDictionary()).ToArray() },
                                        { "clientId", client.ClientId }
                                    });
                    }

                    tx.Success();
                    return Task.FromResult(new ClientAdminResult(client));
                } catch (Exception e)
                {
                    tx.Failure();
                    return Task.FromResult(new ClientAdminResult(e.Message));
                }
            }
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            Client client = null;
            var neo4jDriver = Neo4jProvider.Instance.Driver;

            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                Configuration cfg = Configuration.Instance;
                var clientResult = session.Run(
                    $@"
                        MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})
                        RETURN client
                    ", new Dictionary<string, object> { { "clientId", clientId } }).ToList();

                if (clientResult.Any())
                {
                    var relationsResult = session.Run(
                                $@"
                                    MATCH (client:{cfg.ClientLabel} {{ClientId: {{clientId}}}})-[link]->(relation)
                                    RETURN link, type(link) as linkType, relation
                                ", new Dictionary<string, object> { { "clientId", clientId } }).ToList();

                    //loop through list to build the Client model
                    var dbClient = clientResult.First()["client"].As<INode>();
                    client = dbClient.ToClient();

                    bool foundFirstGrantType = false;
                    foreach (var relation in relationsResult)
                    {
                        //link, type(link) as linkType, relation
                        var relationNode = relation["relation"].As<INode>();
                        var linkType = relation["linkType"].As<string>();
                        var linkObject = relation["link"].As<IRelationship>();

                        if (linkType.Equals(cfg.HasRedirectUriRelName))
                        {
                            var isPostLogoutRedirectUri = false;
                            if (linkObject.Properties.ContainsKey("Type"))
                                isPostLogoutRedirectUri = linkObject["Type"].As<string>().Equals("PostLogoutRedirectUri");

                            ICollection<string> list;
                            if (!isPostLogoutRedirectUri)
                                list = client.RedirectUris ?? (client.RedirectUris = new List<string>());
                             else
                                list = client.PostLogoutRedirectUris ?? (client.PostLogoutRedirectUris = new List<string>());
                            
                            list.Add(relationNode["RedirectUri"].As<string>());
                        }

                        if (linkType.Equals(cfg.HasCorsOriginsRelName))
                        {
                            ICollection<string> list = client.AllowedCorsOrigins ?? (client.AllowedCorsOrigins = new List<string>());
                            list.Add(relationNode["CorsOrigin"].As<string>());
                        }

                        if (linkType.Equals(cfg.HasGrantTypesRelName))
                        {
                            if (!foundFirstGrantType)
                            {
                                client.AllowedGrantTypes = new List<string> { relationNode["GrantType"].As<string>() };
                                foundFirstGrantType = true;
                            } else
                            {
                                IEnumerable<string> gt = client.AllowedGrantTypes;
                                var grantTypes = gt as List<string> ?? gt.ToList();

                                grantTypes.Add(relationNode["GrantType"].As<string>());
                                client.AllowedGrantTypes = grantTypes;
                            }
                        }

                        if (linkType.Equals(cfg.HasIdentityProviderRestrictionsRelName))
                        {
                            ICollection<string> list = client.IdentityProviderRestrictions ?? (client.IdentityProviderRestrictions = new List<string>());
                            list.Add(relationNode["IdentityProviderRestriction"].As<string>());
                        }

                        if (linkType.Equals(cfg.HasScopeRelName))
                        {
                            ICollection<string> list = client.AllowedScopes ?? (client.AllowedScopes = new List<string>());
                            list.Add(relationNode["Name"].As<string>());
                        }

                        if (linkType.Equals(cfg.HasClaimRelName))
                        {
                            ICollection<System.Security.Claims.Claim> list = client.Claims ?? (client.Claims = new List<System.Security.Claims.Claim>());
                            list.Add(relationNode.ToClaim());
                        }

                        if (linkType.Equals(cfg.HasSecretRelName))
                        {
                            ICollection<Secret> list = client.ClientSecrets ?? (client.ClientSecrets = new List<Secret>());
                            list.Add(relationNode.ToSecret());
                        }
                    }
                }
            }

            return Task.FromResult(client);
        }
    }
}
