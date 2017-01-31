using System;
using System.Collections.Specialized;

namespace IdentityServer4.Contrib.Neo4jBolt
{
    public class Configuration
    {
        static volatile Configuration instance;
        static object syncRoot = new object();

        NameValueCollection Settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        /// <param name="settings">The settings used to define the labels</param>
        Configuration(NameValueCollection settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "Unable to find or read Neo4j configuration");
            }
            Settings = settings;
        }
        
        public string ConnectionString => Settings["Connection:Uri"];
        public string Neo4jUser => Settings["Connection:Username"];
        public string Neo4jPassword => Settings["Connection:Password"];

        public string UserLabel => Settings["UserLabel"] ?? "User";
        public string ClaimLabel => Settings["ClaimLabel"] ?? "Claim";
        public string ExternalLoginLabel => Settings["ExternalLoginLabel"] ?? "ExternalLogin";
        public string SecretLabel => Settings["SecretLabel"] ?? "Secret";
        public string ClientLabel => Settings["ClientLabel"] ?? "Client";
        public string ClientScopeLabel => Settings["ClientScopeLabel"] ?? "ClientScope";
        
        public string ClientCorsOriginLabel => Settings["ClientCorsOriginLabel"] ?? "ClientCorsOrigin";
        public string ClientGrantTypeLabel => Settings["ClientGrantTypeLabel"] ?? "ClientGrantType";
        public string ClientRedirectUriLabel => Settings["ClientRedirectUriLabel"] ?? "ClientRedirectUri";
        public string ResourceScopeLabel => Settings["ResourceScopeLabel"] ?? "Scope";
        public string ApiResourceLabel => Settings["ApiResourceLabel"] ?? "ApiResource";
        public string IdentityResourceLabel => Settings["IdentityResourceLabel"] ?? "IdentityResource";
        public string PersistedGrantLabel => Settings["PersistedGrantLabel"] ?? "PersistedGrant";
        public string ClientIdentityProviderRestrictionLabel => Settings["IdentityProviderRestrictionLabel"] ?? "IdentityProviderRestriction";

        public string HasClaimRelName => Settings["HasClaimRelName"] ?? "HAS_Claim";
        public string HasSecretRelName => Settings["HasSecretRelName"] ?? "HAS_Secret";
        public string HasScopeRelName => Settings["HasScopeRelName"] ?? "HAS_Scope";
        public string HasRedirectUriRelName => Settings["HasRedirectUriRelName"] ?? "HAS_Redirect";
        public string HasCorsOriginsRelName => Settings["HasCorsOriginsRelName"] ?? "HAS_CorsOrigin";
        public string HasGrantTypesRelName => Settings["HasGrantTypesRelName"] ?? "HAS_GrantType";
        public string HasIdentityProviderRestrictionsRelName => Settings["HasIdentityProviderRestrictionsRelName"] ?? "HAS_IdentityProviderRestriction";
        
        public string AuthProviderName => Settings["AuthProviderName"] ?? "IdentityServer4";

        public static Configuration SetConfiguration(NameValueCollection settings)
        {
            lock (syncRoot)
            {
                instance = new Configuration(settings);
            }
            return instance;    
        }

        /// <summary>
        /// Global singleton for accessing common graph configuration settings
        /// </summary>
        public static Configuration Instance
        {
            get
            {
                if (instance == null)
                    throw new Exception("Neo4J is not yet configured");
                
                return instance;
            }
        }
    }
}
