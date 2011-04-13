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

namespace MiniME
{
	public class CommandLine
	{

		public class Error : Exception
		{
			public Error(string msg) :
				base(msg)
			{
			}
		}

		public CommandLine(Compiler c)
		{
			m_compiler = c;
			m_warnings = true;
		}

		Compiler m_compiler;
		Encoding m_useEncoding = null;
		bool m_warnings;
		public bool NoLogo = false;
		public bool LogoShown = false;

		public bool ProcessArgs(List<string> args)
		{
			// Parse args
			foreach (var a in args)
			{
				if (!ProcessArg(a))
					return false;
			}

			return true;
		}

		public bool ProcessArg(string a)
		{
			// Response file
			if (a.StartsWith("@"))
			{
				// Get the fully qualified response file name
				string strResponseFile = System.IO.Path.GetFullPath(a.Substring(1));

				// Load and parse the response file
				var args=Utils.ParseCommandLine(System.IO.File.ReadAllText(strResponseFile));

				// Set the current directory
				string OldCurrentDir = System.IO.Directory.GetCurrentDirectory();
				System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(strResponseFile));

				// Load the file
				bool bRetv=ProcessArgs(args);

				// Restore current directory
				System.IO.Directory.SetCurrentDirectory(OldCurrentDir);

				// Register the response file with the compiler so it can check it's time
				// stamp when using CheckFileTimes.
				m_compiler.RegisterResponseFile(strResponseFile);

				return bRetv;
			}

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
					case "h":
					case "?":
						ShowLogo();
						ShowHelp();
						return false;

					case "v":
						ShowLogo();
						return false;

					case "nologo":
						NoLogo = true;
						break;

					case "o":
						m_compiler.OutputFileName = System.IO.Path.GetFullPath(Value);
						break;

					case "d":
						System.IO.Directory.SetCurrentDirectory(Value);
						break;

					case "stdout":
						m_compiler.StdOut = true;
						break;

					case "js":
						m_compiler.MinifyKind = MinifyKind.JS;
						break;

					case "css":
						m_compiler.MinifyKind = MinifyKind.CSS;
						break;

					case "linelen":
						if (Value == null)
						{
							throw new Error("No value specified for argument `linelen`");
						}
						else
						{
							int linelen;
							if (int.TryParse(Value, out linelen) && linelen >= 0)
							{
								m_compiler.MaxLineLength = linelen;
							}
							else
							{
								throw new Error("Invalid value specified for argument `linelen`");
							}

						}
						break;

					case "inputencoding":
						if (String.IsNullOrEmpty(Value) || Value == "auto")
						{
							m_useEncoding = null;
						}
						else
						{
							m_useEncoding = null;
							try
							{
								m_useEncoding = MiniME.TextFileUtils.EncodingFromName(Value);
							}
							catch (Exception)
							{
								// Ignore
							}
							if (m_useEncoding == null)
							{
								throw new Error(string.Format("Unknown input encoding: `{0}`. Use -listencodings for a list", Value));
							}
						}
						break;

					case "outputencoding":
						{
							m_compiler.OutputEncoding = MiniME.TextFileUtils.EncodingFromName(Value);
							if (m_compiler.OutputEncoding == null)
							{
								throw new Error(string.Format("Unknown output encoding: `{0}`. Use -listencodings for a list", Value));
							}
							break;
						}

					case "listencodings":
						{
							foreach (var e in from x in System.Text.Encoding.GetEncodings() orderby x.Name select x)
							{
								Console.WriteLine("`{0}` - {1}", e.Name, e.DisplayName);
							}
							return false;
						}


					case "no-obfuscate":
						m_compiler.NoObfuscate = true;
						break;

					case "ive-donated":
						m_compiler.NoCredit = true;
						break;

					case "check-filetimes":
						m_compiler.CheckFileTimes = true;
						break;

					case "no-options-file":
						m_compiler.UseOptionsFile = false;
						break;

					case "no-warnings":
						m_warnings = false;
						break;

					case "warnings":
						m_warnings = true;
						break;

					case "diag-formatted":
						m_compiler.Formatted = true;
						break;

					case "diag-symbols":
						m_compiler.SymbolInfo = true;
						m_compiler.Formatted = true;
						break;

					case "diag-ast":
						m_compiler.DumpAST = true;
						break;

					case "diag-scopes":
						m_compiler.DumpScopes = true;
						break;

					default:
						throw new Error(string.Format("Unknown switch `{0}`", a));

				}
			}
			else
			{
				m_compiler.AddFiles(a, m_useEncoding, m_warnings);
			}

			return true;
		}


		public void ShowLogo()
		{
			if (LogoShown || NoLogo)
				return;
			LogoShown = true;

			System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Console.WriteLine("MiniME v{0}", v);
			Console.WriteLine("Copyright (C) 2010 Topten Software. Some Rights Reserved.");
			Console.WriteLine("");
		}

		public void ShowHelp()
		{
			Console.WriteLine("MiniME is a Javascript obfuscator and minifier.");
			Console.WriteLine("");
			Console.WriteLine("usage: mm [options] <file1> <file2>...");
			Console.WriteLine("");
			Console.WriteLine("General Options:");
			Console.WriteLine("   -o:<file>              Output filename (defaults to <file1name>.min.js)");
			Console.WriteLine("   -stdout                Send output to stdout instead of a file (use with -nologo)");
			Console.WriteLine("   -linelen:<chars>       Set the maximum line length (defaults to 0 which means no line breaks)");
			Console.WriteLine("   -css                   Force CSS minifier");
			Console.WriteLine("   -js                    Force Javascript minifier");
			Console.WriteLine("   -no-obfuscate          Don't obfuscate symbols");
			Console.WriteLine("   -ive-donated           Don't add 'Minified by MiniME' credit comment (enforced by Karma)");
			Console.WriteLine("   -inputencoding:<name>  Set input file encoding (defaults to `auto`, applies to subsequent filename args)");
			Console.WriteLine("   -outputencoding:<name> Set output file encoding (defaults to same as input file)");
			Console.WriteLine("   -listencodings         Display a list of available encodings");
			Console.WriteLine("   -D:<directory>         Set the current directory");
			Console.WriteLine("   -check-filetimes       Only compile if one or more input files has changed");
			Console.WriteLine("   -no-options-file       Don't save .minime options file when using -check-filetimes");
			Console.WriteLine("   -no-warnings           Don't show warnings for following files");
			Console.WriteLine("   -warnings              Do show warnings for following files");
			Console.WriteLine("   -h, -?                 Show this help");
			Console.WriteLine("   -v                     Show version number");
			Console.WriteLine("   -nologo                Don't show logo");
			Console.WriteLine("");
			Console.WriteLine("Diagnostics Options: (for use by MiniME developers)");
			Console.WriteLine("   -diag-formatted        Format the output to make it more readable");
			Console.WriteLine("   -diag-symbols          Include info on symbol obfucsation in output");
			Console.WriteLine("   -diag-ast              Dump the parsed abstract syntax tree to stdout");
			Console.WriteLine("   -diag-scopes           Dump scope information to stdout");
			Console.WriteLine("");
			Console.WriteLine("For more information including the licensing terms of this software, please visit");
			Console.WriteLine("\n       http://toptensoftware.com/minime");
		}

	}
}
