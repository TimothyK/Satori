using Satori.AzureDevOps.Models;
using System.Net;

namespace Satori.AzureDevOps.Tests.Teams.SampleFiles;

internal static class SampleTeams
{
    public static readonly Team Active = Builder.Builder<Team>.New().Build(int.MaxValue);
    
    public static readonly Team Inactive = Builder.Builder<Team>.New().Build(int.MaxValue);
    public static readonly Team Undated = Builder.Builder<Team>.New().Build(int.MaxValue);

    public static HttpStatusCode GetHttpStatusCode(this Team team)
    {
        return team == Inactive ? HttpStatusCode.NotFound : HttpStatusCode.OK;
    }

    public static byte[] GetPayload(this Team team)
    {
        if (team == Active)
        {
            return TeamResponses.ActiveIteration;
        }
        if (team == Undated)
        {
            return TeamResponses.NullFinishDate_iteration;
        }
        if (team == Inactive)
        {
            return TeamResponses.NoIterationFound_error;
        }

        throw new ArgumentOutOfRangeException(nameof(team));
    }

}