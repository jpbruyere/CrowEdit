// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NetcoreDbgPlugin
{
		[DebuggerDisplay("{Name}")]
		public class MIObject
		{
			public string Name;
			public MIObject(ReadOnlySpan<char> name)
			{
				Name = name.ToString();
			}
		}
		[DebuggerDisplay("{Name}={Value}")]
		public class MIAttribute : MIObject
		{
			public string Value;
			public MIAttribute(ReadOnlySpan<char> name, ReadOnlySpan<char> value) : base(name)
			{
				Value = value.ToString();
			}
			public T GetValue<T>()
			{
				Type type = typeof(T);
				if (type == typeof(string))
					return (T)(object)Value;
				if (type.IsEnum)
				{
					return (T)Enum.Parse(typeof(T), Value);
				}
				else
				{
					MethodInfo miParse = type.GetMethod("Parse", new Type[] { typeof(string) });
					if (miParse != null)
						return (T)miParse.Invoke(null, new object[] { Value });
				}
				return (T)Convert.ChangeType(Value, type);
			}
		}
		[DebuggerDisplay("{Name} ({Attributes.Count})")]
		public class MITupple : MIObject
		{
			public List<MIObject> Attributes = new List<MIObject>();
			public MITupple(ReadOnlySpan<char> name) : base(name) { }

			public MIObject this[string attributeName]
				=> Attributes.FirstOrDefault(a => a.Name == attributeName);
			public MIObject this[int index]
				=> Attributes[index];

			public string GetAttributeValue(string attributeName)
				=> Attributes.OfType<MIAttribute>().FirstOrDefault(a => a.Name == attributeName)?.Value;
			public bool TryGetAttributeValue(string attributeName, out MIAttribute value)
			{
				value = Attributes.OfType<MIAttribute>().FirstOrDefault(a => a.Name == attributeName);
				return value != null;
			}
			public bool TryGetAttributeValue(string attributeName, out string value)
			{
				value = Attributes.OfType<MIAttribute>().FirstOrDefault(a => a.Name == attributeName)?.Value;
				return !string.IsNullOrEmpty (value);
			}

			public static MITupple Parse (ReadOnlySpan<char> data)
			{
				int tokStart = 0;
				int curPos = 0;

				Stack<MIObject> mistack = new Stack<MIObject>();
				mistack.Push(new MITupple("Root"));
				ReadOnlySpan<char> curName = null;
				MITupple tup = null;

				while (curPos < data.Length)
				{
					switch (data[curPos])
					{
						case '[':
							mistack.Push(new MIList(curName));
							curName = null;
							break;
						case '{':
							mistack.Push(new MITupple(curName));
							curName = null;
							break;
						case '}':
							tup = mistack.Pop() as MITupple;
							if (mistack.Peek() is MITupple mit)
								mit.Attributes.Add(tup);
							else
								(mistack.Peek() as MIList).Items.Add(tup);
							break;
						case ']':
							MIList list = mistack.Pop() as MIList;
							(mistack.Peek() as MITupple).Attributes.Add(list);
							break;
						case ',':
							curName = null;
							break;
						case '"':
							tup = mistack.Peek() as MITupple;
							tokStart = ++curPos;
							while (curPos < data.Length && !(data[curPos] == '"' && data[curPos-1] != '\\'))
								curPos++;
							
							tup.Attributes.Add(new MIAttribute(curName, data.Slice(tokStart, curPos - tokStart)));
							break;
						case '=':
							curName = data.Slice(tokStart, curPos - tokStart);
							break;
						default:
							curPos++;
							continue;
					}
					tokStart = ++curPos;
				}
				return mistack.Pop() as MITupple;
			}
		}
		[DebuggerDisplay("{Name} ({Items.Count})")]
		public class MIList : MIObject
		{
			public List<MITupple> Items = new List<MITupple>();
			public MIList(ReadOnlySpan<char> name) : base(name)
			{
			}
		}

		
}