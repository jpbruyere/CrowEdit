using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Crow;
using Crow.Text;
using CrowEditBase;
using System.Reflection;

namespace CrowEdit
{
    public static class Extensions
    {
		public static Picture GetIcon (this MemberInfo mi)
			=> mi is EventInfo ? new BmpPicture("#Icons.event.png") : new BmpPicture("#Icons.property.png");

    }
}
