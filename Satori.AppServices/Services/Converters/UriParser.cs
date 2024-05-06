using CodeMonkeyProjectiles.Linq;
using System.Text;

namespace Satori.AppServices.Services.Converters
{
    internal static class UriParser
    {
        /// <summary>
        /// Returns the AzureDevOps URL including the first Organization path segment.
        /// </summary>
        /// <param name="fullUrl"></param>
        /// <returns></returns>
        public static Uri GetAzureDevOpsOrgUrl(string fullUrl)
        {
            var uri = new Uri(fullUrl);
            var paths = uri.AbsolutePath.Split('/');

            var builder = new StringBuilder();
            builder.Append(uri.GetLeftPart(UriPartial.Authority));
            if (uri.Port.IsNotIn(0, 80))
            {
                builder.Append($":{uri.Port}");
            }

            builder.Append('/').Append(paths[1]);
            return new Uri(builder.ToString());
        }
    }
}
