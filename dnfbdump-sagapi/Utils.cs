using System;

namespace DNFBDmp
{
	public class Utils
	{
		public static string replaceLast(string text, string search, string replace)
		{
			int pos = text.LastIndexOf(search);
			if (pos < 0) return text;
			return string.Concat(text.AsSpan(0, pos), replace, text.AsSpan(pos + search.Length));
		}

		public static string cleanupClassName(string name)
		{
			///*
			/// Regex anyone ? i don't do that so here's a bunch of replaces...
			name = name.Replace(".", "_").Replace("/", "_").Replace("+", "_")
					.Replace("<", "_").Replace("`", "_").Replace(">", "_")
					.Replace("[", "A").Replace("]", "_").Replace(",", "_");

			// Try to shorten the name because std::filesytem implem of windows doesn't handle long paths
			// And flatc uses it without check for a longpath and applying a botch
			// I fucking hate windows so god damn much, fuck you microsoft you piece of shit
			name = name.Replace("System_Collections_Generic_Dictionary", "dict")
					.Replace("System_Collections_Generic_List", "list");
			//.Replace("Torappu_ListDict", "ListDict") not you yet

			//*/

			return name;
		}
	}
}