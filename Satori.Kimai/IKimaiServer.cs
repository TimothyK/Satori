using Satori.Kimai.Models;

namespace Satori.Kimai;

public interface IKimaiServer
{
    Uri BaseUrl { get; }

    Task<TimeEntry[]> GetTimeSheetAsync(TimeSheetFilter filter);

    Task<User> GetMyUserAsync();
}