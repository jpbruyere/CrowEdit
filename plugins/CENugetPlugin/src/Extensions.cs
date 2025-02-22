using System.Net.Http;
using Crow;
using NuGet.Protocol.Core.Types;

namespace NugetPlugin
{
    public static class Extensions
    {
        public static string GetNugetPackageIcon (this IPackageSearchMetadata pkg) {
			return "url:" + pkg.IconUrl;
		}
    }
}
