using Satori.AzureDevOps.Models;
using System.Net;

namespace Satori.AzureDevOps.Tests.Teams.SampleFiles;

internal static class SampleTeams
{
    public static readonly Team Active = Builder.Builder<Team>.New().Build(int.MaxValue);
    
    public static readonly Team Inactive = Builder.Builder<Team>.New().Build(int.MaxValue);

    public static HttpStatusCode GetHttpStatusCode(this Team team)
    {
        if (team == Active)
        {
            return HttpStatusCode.OK;
        }
        if (team == Inactive)
        {
            return HttpStatusCode.NotFound;
        }

        throw new ArgumentOutOfRangeException(nameof(team));

    }

    public static byte[] GetPayload(this Team team)
    {
        if (team == Active)
        {
            return TeamResponses.ActiveIteration;
        }
        if (team == Inactive)
        {
            return TeamResponses.NoIterationFound_error;
        }

        throw new ArgumentOutOfRangeException(nameof(team));
    }

}