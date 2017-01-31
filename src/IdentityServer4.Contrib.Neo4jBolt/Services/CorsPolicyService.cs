using IdentityServer4.Services;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.Neo4jBolt.Services
{
    public class CorsPolicyService : ICorsPolicyService
    {
        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            var allOrigins = new List<string>();
            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                var result = session.Run($"MATCH (client)-[:{cfg.HasCorsOriginsRelName}]->(corsorigin) RETURN corsorigin.CorsOrigin").ToList();
                allOrigins = result.Select(r => r["corsorigin.CorsOrigin"].As<string>()).ToList();
            }

            var isAllowed = allOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(isAllowed);
        }
    }
}
