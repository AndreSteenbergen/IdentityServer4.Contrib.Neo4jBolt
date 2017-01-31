using IdentityServer4.Contrib.Neo4jBolt.Results;
using IdentityServer4.Models;
using System.Threading.Tasks;

namespace IdentityServer4.Contrib.Neo4jBolt.Interfaces
{
    public interface IClientAdminService
    {
        Task<ClientAdminResult> CreateClient(Client client);
    }
}
