using IdentityServer4.Stores;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Neo4j.Driver.V1;
using IdentityServer4.Contrib.Neo4jBolt.Mappers;

namespace IdentityServer4.Contrib.Neo4jBolt.Stores
{
    public class ResourceStore : IResourceStore
    {
        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            ApiResource resource = null;
            Configuration cfg = Configuration.Instance;
            var neo4jDriver = Neo4jProvider.Instance.Driver;

            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                var resourceResult = session.Run(
                    $@"MATCH (resource:{cfg.ApiResourceLabel} {{Name: {{name}}}})
                       RETURN resource
                    ", new Dictionary<string, object> { { "name", name } }).ToList();

                if (resourceResult.Any())
                {
                    var secretsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel} {{Name: {{name}}}})-[link:{cfg.HasSecretRelName}]->(secret)
                                   RETURN secret
                                ", new Dictionary<string, object> { { "name", name } }).ToList();

                    var claimsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel} {{Name: {{name}}}})-[link:{cfg.HasClaimRelName}]->(claim)
                                   RETURN claim
                                ", new Dictionary<string, object> { { "name", name } }).ToList();

                    var scopeRelationsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel} {{Name: {{name}}}})-[link:{cfg.HasScopeRelName}]->(scope)
                                   RETURN id(scope) as scopeId, scope
                                ", new Dictionary<string, object> { { "name", name } }).ToList();

                    var scopeClaimRelationsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel} {{Name: {{name}}}})-[link:{cfg.HasScopeRelName}]->(scope)-[:{cfg.HasClaimRelName}]->(claim)
                                   RETURN id(scope) as scopeId, claim
                                ", new Dictionary<string, object> { { "name", name } }).ToList();
                    var claimsByScope = scopeClaimRelationsResult.GroupBy(sc => sc["scopeId"].As<int>()).ToDictionary(g => g.Key);


                    //loop through list to build the Client model
                    var dbResource = resourceResult.First()["resource"].As<INode>();
                    resource = dbResource.ToApiResource();

                    if (secretsResult.Any())
                        resource.ApiSecrets = secretsResult.Select(sr => sr["secret"].As<INode>().ToSecret()).ToList();

                    if (claimsResult.Any())
                        resource.UserClaims = claimsResult.Select(cr => cr["claim"].As<INode>().ToClaim()).Select(cl => cl.Type).ToList();

                    if (scopeRelationsResult.Any())
                    {
                        resource.Scopes = scopeRelationsResult.Select(scope =>
                        {
                            Scope result = scope["scope"].As<INode>().ToScope();
                            var scopeId = scope["scopeId"].As<int>();
                            result.UserClaims = claimsByScope.ContainsKey(scopeId) ?
                                                    claimsByScope[scopeId].Select(cr => cr["claim"].As<INode>().ToClaim()).Select(cl => cl.Type).ToList() :
                                                    null;
                            return result;
                        }).ToList();
                    }
                }
            }
            return Task.FromResult(resource);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var resources = new List<ApiResource>();
            Configuration cfg = Configuration.Instance;
            var neo4jDriver = Neo4jProvider.Instance.Driver;

            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                var resourceResult = session.Run(
                    $@"MATCH (resource:{cfg.ApiResourceLabel})-[:{cfg.HasScopeRelName}]->(scope:{cfg.ResourceScopeLabel})
                       WHERE scope.Name IN {{names}}
                       RETURN id(resource) as resourceId, resource, id(scope) as scopeId, scope
                    ", new Dictionary<string, object> { { "names", scopeNames.ToArray() } }).ToList();

                if (resourceResult.Any())
                {
                    var scopesById = new Dictionary<int, Scope>();
                    var resourcesById = resourceResult.GroupBy(r => r["resourceId"].As<int>()).ToDictionary(g => g.Key, g =>
                    {
                        var result = g.First()["resource"].As<INode>().ToApiResource();

                        result.Scopes = new List<Scope>();
                        foreach (var item in g)
                        {
                            Scope scope;
                            int scopeId = item["scopeId"].As<int>();
                            if (!scopesById.TryGetValue(scopeId, out scope))
                                scopesById[scopeId] = scope = item["scope"].As<INode>().ToScope();

                            result.Scopes.Add(scope);
                        }

                        return result;
                    });

                    var secretsResult = session.Run(
                                $@"MATCH (secret)<-[:{cfg.HasSecretRelName}]-(resource:{cfg.ApiResourceLabel})-[:{cfg.HasSecretRelName}]->(scope:{cfg.ResourceScopeLabel})
                                   WHERE scope.Name IN {{names}}
                                   RETURN id(resource) as resourceId, secret
                                ", new Dictionary<string, object> { { "names", scopeNames.ToArray() } }).ToList();

                    var claimsresult = session.Run(
                                $@"MATCH (claim)<-[:{cfg.HasClaimRelName}]-(resource:{cfg.ApiResourceLabel})-[:{cfg.HasClaimRelName}]->(scope:{cfg.ResourceScopeLabel})
                                   WHERE scope.Name IN {{names}}
                                   RETURN id(resource) as resourceId, claim
                                ", new Dictionary<string, object> { { "names", scopeNames.ToArray() } }).ToList();

                    var scopeClaimsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel})-[:{cfg.HasScopeRelName}]->(scope:{cfg.ResourceScopeLabel})-[:{cfg.HasClaimRelName}]->(claim)
                                   WHERE scope.Name IN {{names}}
                                   RETURN id(scope) as scopeId, claim
                                ", new Dictionary<string, object> { { "names", scopeNames.ToArray() } }).ToList();

                    foreach (var item in secretsResult)
                    {
                        var resourceId = item["resourceId"].As<int>();
                        var list = resourcesById[resourceId].ApiSecrets ?? (resourcesById[resourceId].ApiSecrets = new List<Secret>());
                        list.Add(item["secret"].As<INode>().ToSecret());
                    }

                    foreach (var item in claimsresult)
                    {
                        var resourceId = item["resourceId"].As<int>();
                        var list = resourcesById[resourceId].UserClaims ?? (resourcesById[resourceId].UserClaims = new List<string>());
                        list.Add(item["claim"].As<INode>().ToClaim().Type);
                    }

                    foreach (var item in scopeClaimsResult)
                    {
                        var scopeId = item["scopeId"].As<int>();
                        var list = scopesById[scopeId].UserClaims ?? (scopesById[scopeId].UserClaims = new List<string>());
                        list.Add(item["claim"].As<INode>().ToClaim().Type);
                    }

                    resources = resourcesById.Values.ToList();
                }
            }
            return Task.FromResult(resources.AsEnumerable());
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            List<IdentityResource> resources = null;
            Configuration cfg = Configuration.Instance;
            var neo4jDriver = Neo4jProvider.Instance.Driver;

            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                var resourceResult = session.Run(
                    $@"MATCH (resource:{cfg.IdentityResourceLabel})
                       WHERE resource.Name IN {{names}}
                       RETURN id(resource) as resourceId, resource
                    ", new Dictionary<string, object> { { "names", scopeNames.ToArray() } }).ToList();

                if (resourceResult.Any())
                {
                    var resourcesById = resourceResult.GroupBy(r => r["resourceId"].As<int>()).ToDictionary(g => g.Key, g => g.First()["resource"].As<INode>().ToIdentityResource());

                    var claimsresult = session.Run(
                                            $@"MATCH (resource:{cfg.IdentityResourceLabel})-[:{cfg.HasClaimRelName}]->(claim)
                                               WHERE resource.Name IN {{names}}
                                               RETURN id(resource) as resourceId, claim
                                            ", new Dictionary<string, object> { { "names", scopeNames.ToArray() } }).ToList();

                    foreach (var item in claimsresult)
                    {
                        var resourceId = item["resourceId"].As<int>();
                        var list = resourcesById[resourceId].UserClaims ?? (resourcesById[resourceId].UserClaims = new List<string>());
                        list.Add(item["claim"].As<INode>().ToClaim().Type);
                    }

                    resources = resourcesById.Values.ToList();
                }
            }
            return Task.FromResult(resources.AsEnumerable());
        }

        public Task<Resources> GetAllResources()
        {
            List<ApiResource> apiResources = null;
            List<IdentityResource> identityResources = null;

            Configuration cfg = Configuration.Instance;
            var neo4jDriver = Neo4jProvider.Instance.Driver;

            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                if (!string.IsNullOrEmpty("IdentityResource"))
                {
                    var identityResourceResult = session.Run($"MATCH (resource:{cfg.IdentityResourceLabel}) RETURN id(resource) as resourceId, resource").ToList();
                    if (identityResourceResult.Any())
                    {
                        var resourcesById = identityResourceResult.GroupBy(r => r["resourceId"].As<int>()).ToDictionary(g => g.Key, g => g.First()["resource"].As<INode>().ToIdentityResource());
                        var claimsresult = session.Run($"MATCH (resource:{cfg.IdentityResourceLabel})-[:{cfg.HasScopeRelName}]->(claim) RETURN id(resource) as resourceId, claim").ToList();

                        foreach (var item in claimsresult)
                        {
                            var resourceId = item["resourceId"].As<int>();
                            var list = resourcesById[resourceId].UserClaims ?? (resourcesById[resourceId].UserClaims = new List<string>());
                            list.Add(item["claim"].As<INode>().ToClaim().Type);
                        }

                        identityResources = resourcesById.Values.ToList();
                    }
                }

                if (!string.IsNullOrEmpty("ApiResource"))
                {
                    var apiResourceResult = session.Run($"MATCH (resource:{cfg.ApiResourceLabel}) RETURN id(resource) as resourceId, resource").ToList();
                    if (apiResourceResult.Any())
                    {
                        var resourcesById = apiResourceResult.GroupBy(r => r["resourceId"].As<int>()).ToDictionary(g => g.Key, g => g.First()["resource"].As<INode>().ToApiResource());

                        var secretsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel})-[:{cfg.HasSecretRelName}]->(secret)
                                   RETURN id(resource) as resourceId, secret
                                ").ToList();

                        var claimsresult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel})-[:{cfg.HasClaimRelName}]->(claim)
                                   RETURN id(resource) as resourceId, claim
                                ").ToList();

                        var scopesResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel})-[:{cfg.HasScopeRelName}]->(scope:{cfg.ResourceScopeLabel})
                                   RETURN id(resource) as resourceId, id(scope) as scopeId, scope
                                ").ToList();

                        var scopeClaimsResult = session.Run(
                                $@"MATCH (resource:{cfg.ApiResourceLabel})-[:{cfg.HasScopeRelName}]->(scope:{cfg.ResourceScopeLabel})-[:{cfg.HasClaimRelName}]->(claim)
                                   RETURN id(scope) as scopeId, claim
                                ").ToList();

                        foreach (var item in secretsResult)
                        {
                            var resourceId = item["resourceId"].As<int>();
                            var list = resourcesById[resourceId].ApiSecrets ?? (resourcesById[resourceId].ApiSecrets = new List<Secret>());
                            list.Add(item["secret"].As<INode>().ToSecret());
                        }

                        foreach (var item in claimsresult)
                        {
                            var resourceId = item["resourceId"].As<int>();
                            var list = resourcesById[resourceId].UserClaims ?? (resourcesById[resourceId].UserClaims = new List<string>());
                            list.Add(item["claim"].As<INode>().ToClaim().Type);
                        }

                        var scopesById = new Dictionary<int, Scope>();
                        foreach (var item in scopesResult)
                        {
                            Scope scope;
                            int scopeId = item["scopeId"].As<int>();
                            if (!scopesById.TryGetValue(scopeId, out scope))
                                scopesById[scopeId] = scope = item["scope"].As<INode>().ToScope();

                            var resourceId = item["resourceId"].As<int>();
                            var list = resourcesById[resourceId].Scopes ?? (resourcesById[resourceId].Scopes = new List<Scope>());
                            list.Add(scope);
                        }

                        foreach (var item in scopeClaimsResult)
                        {
                            var scopeId = item["scopeId"].As<int>();
                            var list = scopesById[scopeId].UserClaims ?? (scopesById[scopeId].UserClaims = new List<string>());
                            list.Add(item["claim"].As<INode>().ToClaim().Type);
                        }

                        apiResources = resourcesById.Values.ToList();
                    }
                }
            }

            var result = new Resources(identityResources.AsEnumerable(), apiResources.AsEnumerable());
            return Task.FromResult(result);
        }
    }
}
