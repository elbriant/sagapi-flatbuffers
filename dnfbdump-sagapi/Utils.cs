using System;

namespace DNFBDmp {
	public class Utils {
		public static string replaceLast(string text, string search, string replace) {
			int pos = text.LastIndexOf(search);
			if (pos < 0) return text;
			return string.Concat(text.AsSpan(0, pos), replace, text.AsSpan(pos + search.Length));
		}

		public static string cleanupClassName(string name) {
			if (name.StartsWith("System.Collections.Generic.Dictionary")) {
				name = name.Replace("System.Collections.Generic.Dictionary`2<", "dict__")
						.Replace(", ", "__").Replace(">", "");
			}
			if (name.StartsWith("System.Collections.Generic.List<System.Collections.Generic.Dictionary")) {
				name = name.Replace("System.Collections.Generic.List<System.Collections.Generic.Dictionary`2<", "list_dict__")
						.Replace(", ", "__").Replace(">>", "");
			}

			name = name.Replace(".", "_").Replace("/", "_").Replace("+", "_")
					.Replace("<", "_").Replace("`", "_").Replace(">", "_")
					.Replace("[", "A").Replace("]", "_").Replace(",", "_");

			name = name.Replace("System_Collections_Generic_Dictionary", "dict__")
					.Replace("Torappu_ListDict", "list_dict__")
					.Replace("System_Collections_Generic_List", "list__");

			return name;
		}
	}
}