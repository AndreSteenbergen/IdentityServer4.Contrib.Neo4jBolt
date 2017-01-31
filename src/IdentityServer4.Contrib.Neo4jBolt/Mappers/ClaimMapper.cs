using Neo4j.Driver.V1;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer4.Contrib.Neo4jBolt.Mappers
{
    public static class ClaimMapper
    {
        public static Claim ToClaim(this INode dbClaim)
        {
            var type = dbClaim.Properties.ContainsKey("Type") ? dbClaim["Type"].As<string>() : string.Empty;
            var value = dbClaim.Properties.ContainsKey("Value") ? dbClaim["Value"].As<string>() : string.Empty;

            return new Claim(type, value);
        }

        public static IDictionary<string, object> ToDictionary(this Claim claim)
        {
            var result = new Dictionary<string, object>();

            result["type"] = claim.Type;
            result["value"] = claim.Value;
            //result["typevalue"] = $"{claim.Type}-<>-{claim.Value}"; //not necesary?

            return result;
        }
    }
}
