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
		void ShowLogo()
		{
			System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Console.WriteLine("MiniME v{0}", v);
			Console.WriteLine("Copyright (C) 2010 Topten Software. Some Rights Reserved.");
		}
		void ShowHelp()
		{
            Console.WriteLine("");
            Console.WriteLine("MiniME is a Javascript obfuscator and minifier.");
            Console.WriteLine("");
            Console.WriteLine("usage: mm [options] <file1> <file2>...");
            Console.WriteLine("");
            Console.WriteLine("General Options:");
            Console.WriteLine("   -o:<file>              Output filename (defaults to <file1name>.min.js)");
            Console.WriteLine("   -stdout                Send output to stdout instead of a file (use with -nologo)");
            Console.WriteLine("   -linelen:<chars>       Set the maximum line length (defaults to 0 which means no line breaks)");
			Console.WriteLine("   -no-obfuscate          Don't obfuscate symbols");
			Console.WriteLine("   -ive-donated           Don't add 'Minified by MiniME' credit comment (enforced by Karma)");
			Console.WriteLine("   -inputencoding:<name>  Set input file encoding (defaults to `auto`, applies to subsequent filename args)");
			Console.WriteLine("   -outputencoding:<name> Set output file encoding (defaults to same as input file)");
			Console.WriteLine("   -listencodings         Display a list of available encodings");
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

		void Run(string[] args)
		{
			bool bStdOut = false;
			bool bAnyFiles = false;
			bool bShowLogo = true;
			MiniME.Compiler c = new MiniME.Compiler();
			Encoding useEncoding = null;

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
						case "h":
						case "?":
							ShowHelp();
							return;

						case "v":
							ShowLogo();
							return;

						case "nologo":
							bShowLogo = false;
							break;

						case "o":
							c.OutputFileName = Value;
							break;

						case "stdout":
							bStdOut = true;
							break;

						case "linelen":
							if (Value == null)
							{
								Console.WriteLine("Warning: ignoring command line argument `linelen` - value not specified");
							}
							else
							{
								int linelen;
								if (int.TryParse(Value, out linelen) && linelen>=0)
								{
									c.MaxLineLength = linelen;
								}
								else
								{
									Console.WriteLine("Warning: ignoring command line argument `linelen` - invalid value");
								}

							}
							break;

						case "inputencoding":
							if (String.IsNullOrEmpty(Value) || Value=="auto")
							{
								useEncoding = null;
							}
							else
							{
								useEncoding = null;
								try
								{
									useEncoding=MiniME.TextFileUtils.EncodingFromName(Value);
								}
								catch (Exception)
								{
									// Ignore
								}
								if (useEncoding == null)
								{
									Console.WriteLine("Unknown input encoding: `{0}`. Use -listencodings for a list", Value);
									System.Environment.ExitCode = 7;
									return;
								}
							}
							break;

						case "outputencoding":
							{
								c.OutputEncoding = MiniME.TextFileUtils.EncodingFromName(Value);
								if (c.OutputEncoding == null)
								{
									Console.WriteLine("Unknown output encoding: `{0}`. Use -listencodings for a list", Value);
									System.Environment.ExitCode = 9;
									return;
								}
								break;
							}

						case "listencodings":
							{
								foreach (var e in from x in System.Text.Encoding.GetEncodings() orderby x.Name select x)
								{
									Console.WriteLine("`{0}` - {1}", e.Name, e.DisplayName);
								}
								System.Environment.ExitCode = 9;
								return;
							}

		
						case "no-obfuscate":
							c.NoObfuscate = true;
							break;

						case "ive-dontated":
							c.NoCredit = true;
							break;

						case "diag-formatted":
							c.Formatted = true;
							break;

						case "diag-symbols":
							c.SymbolInfo = true;
							c.Formatted = true;
							break;

						case "diag-ast":
							c.DumpAST = true;
							break;

						case "diag-scopes":
							c.DumpScopes = true;
							break;

						default:
							Console.WriteLine("Unknown switch `-{0}`, aborting", a);
							System.Environment.ExitCode = 7;
							return;

					}
				}
				else
				{
					c.AddFile(a, useEncoding);
					bAnyFiles = true;
				}
			}

			if (bShowLogo)
			{
				ShowLogo();
			}

			if (!bAnyFiles)
			{
				ShowHelp();
				return;
			}

			if (bStdOut)
			{
				Console.WriteLine(c.CompileToString());
			}
			else
			{
				c.Compile();
			}
		}

		static void Main(string[] args)
		{
			try
			{
				new Program().Run(args);
			}
			catch (MiniME.CompileError e)
			{
				Console.WriteLine(e.Message);
				System.Environment.ExitCode = 7;
			}
			catch (System.IO.IOException e)
			{
				Console.WriteLine("File error - {0}", e.Message);
				System.Environment.ExitCode = 9;
			}
				/*
			catch (Exception e)
			{
				Console.WriteLine("Internal error - {0}", e.Message);
				System.Environment.ExitCode = 11;
			}
				 */
		
}
	}
}
