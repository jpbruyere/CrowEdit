using Crow;
using CrowEditBase;
using System.Reflection;

namespace CrowEdit
{
    public static class Extensions
    {
      public static Picture GetIcon (this MemberInfo mi)
        => mi is EventInfo ? new SvgPicture("#icons.event.svg") : new SvgPicture("#icons.property.svg");
      public static Picture GetIcon (this SyntaxException se) => new SvgPicture("#icons.IconAlerte.svg");
    }
}
