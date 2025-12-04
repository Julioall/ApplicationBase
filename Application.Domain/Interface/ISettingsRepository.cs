using Application.Domain.Model;
using System.Threading.Tasks;

namespace Application.Domain.Interface
{
    public interface ISettingsRepository
    {
        Task<Configurations?> GetAsync();
        Task SaveAsync(Configurations configurations);
    }
}
