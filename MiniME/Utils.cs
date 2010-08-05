using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	enum TriState
	{
		Yes,
		No,
		Maybe,
	}

	// Miscellaneous utility and extension functions
	static class Utils
	{							   
		// Write a line to output with indenting
		public static void WriteIndentedLine(int indent, string format, params object[] args)
		{
			Console.Write(new string(' ', 4 * indent));
			Console.WriteLine(format, args);
		}

		public static List<string> ParseCommandLine(string args)
		{
			var newargs = new List<string>();

			var temp = new StringBuilder();

			int i = 0;
			while (i < args.Length)
			{
				if (char.IsWhiteSpace(args[i]))
				{
					i++;
					continue;
				}

				bool bInQuotes = false;
				temp.Length = 0;
				while (i < args.Length && (!char.IsWhiteSpace(args[i]) && !bInQuotes))
				{
					if (args[i] == '\"')
					{
						if (args[i + 1] == '\"')
						{
							temp.Append("\"");
							i++;
						}
						else
						{
							bInQuotes = !bInQuotes;
						}
					}
					else
					{
						temp.Append(args[i]);
					}

					i++;
				}

				if (temp.Length > 0)
				{
					newargs.Add(temp.ToString());
				}
			}

			return newargs;
		}

	}
}
