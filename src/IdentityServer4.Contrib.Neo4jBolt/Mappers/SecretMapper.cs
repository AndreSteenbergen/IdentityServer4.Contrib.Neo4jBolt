using IdentityServer4.Models;
using Neo4j.Driver.V1;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace IdentityServer4.Contrib.Neo4jBolt.Mappers
{
    public static class SecretMapper
    {
        public static Secret ToSecret(this INode dbSecret)
        {
            var value = dbSecret.Properties.ContainsKey("Value") ? dbSecret["Value"].As<string>() : string.Empty;
            var description = dbSecret.Properties.ContainsKey("Description") ? dbSecret["Description"].As<string>() : null;
            var expiration = dbSecret.Properties.ContainsKey("Expiration") && dbSecret["Expiration"].As<string>() != null ?
                                    DateTime.ParseExact(dbSecret["Expiration"].As<string>(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind) :
                                    (DateTime?) null;

            return new Secret(value, description, expiration);
        }

        public static IDictionary<string, object> ToDictionary(this Secret secret)
        {
            var result = new Dictionary<string, object>();

            result["value"] = secret.Value;
            result["description"] = secret.Description;
            result["exiration"] = secret.Expiration?.ToString("o");
            
            return result;
        }
    }
}
