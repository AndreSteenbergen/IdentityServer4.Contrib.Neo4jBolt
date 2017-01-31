using IdentityServer4.Models;
using Neo4j.Driver.V1;

namespace IdentityServer4.Contrib.Neo4jBolt.Mappers
{
    public static class ScopeMapper
    {
        public static Scope ToScope(this INode dbScope)
        {
            var result = new Scope
            {
                Name = dbScope["Name"].As<string>()                
            };

            if (dbScope.Properties.ContainsKey("Description"))
                result.Description = dbScope["Description"].As<string>();

            if (dbScope.Properties.ContainsKey("DisplayName"))
                result.DisplayName = dbScope["DisplayName"].As<string>();

            if (dbScope.Properties.ContainsKey("Emphasize"))
                result.Emphasize = dbScope["Emphasize"].As<bool>();

            if (dbScope.Properties.ContainsKey("Required"))
                result.Required = dbScope["Required"].As<bool>();

            if (dbScope.Properties.ContainsKey("ShowInDiscoveryDocument"))
                result.ShowInDiscoveryDocument = dbScope["ShowInDiscoveryDocument"].As<bool>();
            
            return result;
        }
    }
}
