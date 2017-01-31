using IdentityServer4.Models;
using Neo4j.Driver.V1;
using System.Collections.Generic;

namespace IdentityServer4.Contrib.Neo4jBolt.Mappers
{
    public static class IdentityResourceMapper
    {
        public static IdentityResource ToIdentityResource(this INode dbResource)
        {
            var result = new IdentityResource
            {
                Name = dbResource["Name"].As<string>()                
            };
            
            if (dbResource.Properties.ContainsKey("Description"))
                result.Description = dbResource["Description"].As<string>();

            if (dbResource.Properties.ContainsKey("DisplayName"))
                result.DisplayName = dbResource["DisplayName"].As<string>();

            if (dbResource.Properties.ContainsKey("Enabled"))
                result.Enabled = dbResource["Enabled"].As<bool>();

            if (dbResource.Properties.ContainsKey("Emphasize"))
                result.Emphasize = dbResource["Emphasize"].As<bool>();

            if (dbResource.Properties.ContainsKey("Required"))
                result.Required = dbResource["Required"].As<bool>();

            return result;
        }

        public static IDictionary<string, object> ToDictionary(this IdentityResource resource)
        {
            return new Dictionary<string, object> {
                { "name", resource.Name },
                { "description", resource.Description },
                { "displayName", resource.DisplayName },
                { "enabled", resource.Enabled },
                { "emphasize", resource.Emphasize },
                { "required", resource.Required }
            };
        }
    }
}
