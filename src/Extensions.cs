using System;
using System.Collections.Generic;
using System.Text;
using Crow.Text;

namespace CrowEdit
{
    public static class Extensions
    {
        public static TextChange Inverse (this TextChange tch, string src)
            => new TextChange (tch.Start, string.IsNullOrEmpty (tch.ChangedText) ? 0 : tch.ChangedText.Length,
                tch.Length == 0 ? "" : src.AsSpan (tch.Start, tch.Length).ToString());
    }
}
