using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mm
{
	class Program
	{
		string m_strOutputFileName;

		void Run(string[] args)
		{
			MiniME.Compiler c = new MiniME.Compiler();

			// Process command line arguments
			foreach (var a in args)
			{
				// Args are in format [/-]<switchname>[:<value>];
				if (a.StartsWith("/") || a.StartsWith("-"))
				{
					string SwitchName = a.Substring(1);
					string Value = null;

					int colonpos = SwitchName.IndexOf(':');
					if (colonpos >= 0)
					{
						// Split it
						Value = SwitchName.Substring(colonpos + 1);
						SwitchName = SwitchName.Substring(0, colonpos);
					}

					switch (SwitchName)
					{
						case "o":
							m_strOutputFileName = Value;
							break;
					}
				}
				else
				{
					// Base output file name on first input file
					if (String.IsNullOrEmpty(m_strOutputFileName))
					{
						int dotpos = a.LastIndexOf('.');
						if (dotpos >= 0)
							m_strOutputFileName = a.Substring(0, dotpos);
						m_strOutputFileName += ".min.js";
					}

					c.AddFile(a);
				}
			}

			c.Compile();
		}

		static void Main(string[] args)
		{
			try
			{
				new Program().Run(args);
			}
			catch (MiniME.CompileError  e)
			{
				Console.WriteLine(e.Message);
			}
		}
	}
}
