using IdentityServer4.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Neo4j.Driver.V1;
using System.Globalization;

namespace IdentityServer4.Contrib.Neo4jBolt.Stores
{
    public class PersistedGrantStore : IPersistedGrantStore
    {
        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            var result = new List<PersistedGrant>();
            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                var grants = session.Run($@"MATCH (grant:{cfg.PersistedGrantLabel} {{SubjectId : {{subjectId}}}}) RETURN grant", new Dictionary<string, object> {
                    { "subjectId", subjectId }
                }).ToList();

                foreach (var grant in grants)
                {
                    var node = grant["grant"].As<INode>();
                    result.Add(new PersistedGrant
                    {
                        Key = node["Key"].As<string>(),
                        ClientId = node["ClientId"].As<string>(),
                        Type = node["Type"].As<string>(),
                        Data = node["Data"].As<string>(),
                        SubjectId = node["SubjectId"].As<string>(),
                        CreationTime = DateTime.ParseExact(node["CreationTime"].As<string>(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        Expiration = string.IsNullOrEmpty(node["Expiration"].As<string>()) ? (DateTime?)null : DateTime.ParseExact(node["Expiration"].As<string>(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                    });
                }
            }

            return Task.FromResult(result.AsEnumerable());
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            PersistedGrant result = null;
            using (var session = neo4jDriver.Session(AccessMode.Read))
            {
                var grant = session.Run($@"MATCH (grant:{cfg.PersistedGrantLabel} {{Key : {{key}}}}) RETURN grant", new Dictionary<string, object> {
                    { "key", key }
                }).ToList().FirstOrDefault();

                if (grant != null)
                {
                    var node = grant["grant"].As<INode>();
                    result = new PersistedGrant {
                        Key = key,
                        ClientId = node["ClientId"].As<string>(),
                        Type = node["Type"].As<string>(),
                        Data = node["Data"].As<string>(),
                        SubjectId = node["SubjectId"].As<string>(),
                        CreationTime = DateTime.ParseExact(node["CreationTime"].As<string>(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                        Expiration = string.IsNullOrEmpty(node["Expiration"].As<string>()) ? (DateTime?) null : DateTime.ParseExact(node["Expiration"].As<string>(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                    };
                }
            }

            return Task.FromResult(result);
        }

        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            using (var session = neo4jDriver.Session(AccessMode.Write))
            using (var tx = session.BeginTransaction())
            {
                tx.Run($@"MATCH (grant:{cfg.PersistedGrantLabel} {{SubjectId : {{subjectId}}, ClientId : {{clientId}}}}) DELETE grant", new Dictionary<string, object> {
                    { "subjectId", subjectId },
                    { "clientId", clientId }
                });

                tx.Success();
            }
            return Task.FromResult(0);
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            using (var session = neo4jDriver.Session(AccessMode.Write))
            using (var tx = session.BeginTransaction())
            {
                tx.Run($@"MATCH (grant:{cfg.PersistedGrantLabel} {{SubjectId : {{subjectId}}, ClientId : {{clientId}}, Type : {{type}}}}) DELETE grant", new Dictionary<string, object> {
                    { "subjectId", subjectId },
                    { "clientId", clientId },
                    { "type", type }
                });

                tx.Success();
            }
            return Task.FromResult(0);
        }

        public Task RemoveAsync(string key)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            using (var session = neo4jDriver.Session(AccessMode.Write))
            using (var tx = session.BeginTransaction())
            {
                tx.Run($@"MATCH (grant:{cfg.PersistedGrantLabel} {{Key : {{key}}}}) DELETE grant", new Dictionary<string, object> {
                    { "key", key }
                });

                tx.Success();
            }
            return Task.FromResult(0);
        }

        public Task StoreAsync(PersistedGrant grant)
        {
            var neo4jDriver = Neo4jProvider.Instance.Driver;
            Configuration cfg = Configuration.Instance;

            using (var session = neo4jDriver.Session(AccessMode.Write))
            using (var tx = session.BeginTransaction())
            {
                tx.Run($@"MERGE (grant:{cfg.PersistedGrantLabel} {{Key : {{key}}}})
                          SET grant.Type         = {{type}},
                              grant.SubjectId    = {{subjectId}},
                              grant.ClientId     = {{clientId}},
                              grant.CreationTime = {{creationTime}},
                              grant.Expiration   = {{expiration}},
                              grant.Data         = {{data}}", new Dictionary<string, object> {
                    { "key", grant.Key },
                    { "type", grant.Type },
                    { "subjectId", grant.SubjectId },
                    { "clientId", grant.ClientId },
                    { "creationTime", grant.CreationTime.ToString("o") },
                    { "expiration", grant.Expiration.HasValue ? grant.Expiration.Value.ToString("o") : (string) null },
                    { "data", grant.Data }
                });

                tx.Success();
            }
            return Task.FromResult(0);
        }
    }
}
