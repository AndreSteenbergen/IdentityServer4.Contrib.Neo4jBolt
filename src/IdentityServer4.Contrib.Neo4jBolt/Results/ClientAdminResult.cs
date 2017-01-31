using IdentityServer4.Models;

namespace IdentityServer4.Contrib.Neo4jBolt.Results
{
    public class ClientAdminResult
    {
        public ClientAdminResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
            Success = false;
        }

        public ClientAdminResult(Client client)
        {
            Client = client;
            Success = true;
        }

        public Client Client { get; private set; }

        public bool Success { get; private set; }

        public string ErrorMessage { get; private set; }
    }
}
