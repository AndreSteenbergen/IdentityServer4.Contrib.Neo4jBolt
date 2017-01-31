using Neo4j.Driver.V1;

namespace IdentityServer4.Contrib.Neo4jBolt
{
    public sealed class Neo4jProvider
    {
        static volatile Neo4jProvider instance;
        static object syncRoot = new object();

        public IDriver Driver { get; private set; }

        Neo4jProvider(Configuration config)
        {
            var authToken = AuthTokens.Basic(config.Neo4jUser, config.Neo4jPassword);
            Driver = GraphDatabase.Driver(config.ConnectionString, authToken);

            //initialize always:
            using (var session = Driver.Session())
            using (var tx = session.BeginTransaction())
            {
                tx.Run($"CREATE CONSTRAINT ON (n:{config.ClientLabel}) ASSERT n.ClientId IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (n:{config.ClientRedirectUriLabel}) ASSERT n.RedirectUri IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (n:{config.ClientCorsOriginLabel}) ASSERT n.CorsOrigin IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (n:{config.ClientGrantTypeLabel}) ASSERT n.GrantType IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (n:{config.IdentityProviderRestrictionLabel}) ASSERT n.IdentityProviderRestriction IS UNIQUE");
                
                tx.Run($"CREATE CONSTRAINT ON (n:{config.ApiResourceLabel}) ASSERT n.Name IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (n:{config.ResourceScopeLabel}) ASSERT n.Name IS UNIQUE");
                tx.Run($"CREATE CONSTRAINT ON (n:{config.PersistedGrantLabel}) ASSERT n.Key IS UNIQUE");

                tx.Success();
            }
        }

        public static Neo4jProvider Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Neo4jProvider(Configuration.Instance);
                    }
                }

                return instance;
            }
        }
    }
}
