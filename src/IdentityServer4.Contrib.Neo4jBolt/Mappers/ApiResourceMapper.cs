using IdentityServer4.Models;
using Neo4j.Driver.V1;
using System.Collections.Generic;

namespace IdentityServer4.Contrib.Neo4jBolt.Mappers
{
    public static class ApiResourceMapper
    {
        public static ApiResource ToApiResource(this INode dbResource)
        {
            var result = new ApiResource
            {
                Name = dbResource["Name"].As<string>()       ,                         
            };

            if (dbResource.Properties.ContainsKey("Description"))
                result.Description = dbResource["Description"].As<string>();

            if (dbResource.Properties.ContainsKey("DisplayName"))
                result.DisplayName = dbResource["DisplayName"].As<string>();

            if (dbResource.Properties.ContainsKey("Enabled"))
                result.Enabled = dbResource["Enabled"].As<bool>();
            
            return result;
        }

        public static IDictionary<string, object> ToDictionary(this ApiResource resource)
        {
            return new Dictionary<string, object> {
                { "name", resource.Name },
                { "description", resource.Description },
                { "displayName", resource.DisplayName },
                { "enabled", resource.Enabled }
            };
        }
    }
}
