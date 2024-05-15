using Satori.Kimai.Models;

namespace Satori.Kimai;

public interface IKimaiServer
{
    Task<TimeEntry[]> GetTimeSheetAsync(TimeSheetFilter filter);

    Task<User> GetMyUserAsync();
}