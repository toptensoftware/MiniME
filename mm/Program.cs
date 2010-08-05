using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
namespace mm
{

	// "Z:\mobilize\www\mobSite\js\jQuery-1.4.2.js" -o:"Z:\mobilize\www\mobSite\js\jQuery-1.4.2.mm.js" -diag-formatted
	// input.js -stdout -diag-ast

	class Program
	{
		void Run(string[] args)
		{
			// Create MiniME compiler and command line processor
			MiniME.Compiler c = new MiniME.Compiler();
			MiniME.CommandLine cl = new MiniME.CommandLine(c);

			try
			{
				// Process all arguments, quit if handled internally
				if (!cl.ProcessArgs(args.ToList()))
					return;

				// Show the logo
				cl.ShowLogo();

				// Check we have something to do, if not, show the help
				if (c.FileCount==0)
				{
					cl.ShowHelp();
					Console.WriteLine("No input files specified");
					return;
				}

				// Compile!
				c.Compile();
			}
			catch (MiniME.CompileError e)
			{
				cl.ShowLogo();
				Console.WriteLine(e.Message);
				System.Environment.ExitCode = 7;
			}
			catch (MiniME.CommandLine.Error e)
			{
				cl.ShowLogo();
				Console.WriteLine(e.Message);
				System.Environment.ExitCode = 9;
			}
			catch (System.IO.IOException e)
			{
				cl.ShowLogo();
				Console.WriteLine("File error - {0}", e.Message);
				System.Environment.ExitCode = 11;
			}
#if !DEBUG
			catch (Exception e)
			{		  
				Console.WriteLine("Internal error - {0}", e.Message);
				System.Environment.ExitCode = 13;
			}
#endif

			Console.WriteLine("");
		}

		static void Main(string[] args)
		{
			new Program().Run(args);
		}
	}
}
