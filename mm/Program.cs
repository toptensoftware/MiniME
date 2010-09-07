// 
//   MiniME - http://www.toptensoftware.com/minime
// 
//   The contents of this file are subject to the license terms as 
//	 specified at the web address above.
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
namespace mm
{
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
