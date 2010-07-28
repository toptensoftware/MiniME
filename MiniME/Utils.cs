using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// Miscellaneous utility and extension functions
	static class Utils
	{							   
		// Write a line to output with indenting
		public static void WriteIndentedLine(int indent, string format, params object[] args)
		{
			Console.Write(new string(' ', 4 * indent));
			Console.WriteLine(format, args);
		}
	}
}
