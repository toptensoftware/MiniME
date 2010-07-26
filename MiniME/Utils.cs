using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	internal static class Utils
	{							   
		internal static void Increment(this Dictionary<string, int> map, string str)
		{
			int freq = 0;
			map.TryGetValue(str, out freq);
			map[str] = freq + 1;
		}

		internal static void Increment(this Dictionary<string, int> map, string str, int Count)
		{
			int freq = 0;
			map.TryGetValue(str, out freq);
			map[str] = freq + Count;
		}

		internal static void WriteIndentedLine(int indent, string format, params object[] args)
		{
			Console.Write(new string(' ', 4 * indent));
			Console.WriteLine(format, args);
		}
	}
}
