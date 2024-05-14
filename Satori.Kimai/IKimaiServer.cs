using Satori.Kimai.Models;

namespace Satori.Kimai;

public interface IKimaiServer
{
    Task<User> GetMyUserAsync();
}