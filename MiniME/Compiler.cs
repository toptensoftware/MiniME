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
using System.IO;

namespace MiniME
{
	public enum MinifyKind
	{
		Auto,
		JS,
		CSS,
	}

	// Main api into the MiniME minifier/obfuscator
	public class Compiler
	{
		// Constructor
		public Compiler()
		{
			Reset();
			DetectConsts = true;
			UseOptionsFile = true;
			MaxLineLength = 120;
			MinifyKind=MinifyKind.Auto;
		}

		public MinifyKind MinifyKind
		{
			get; set;
		}

		// Attributes
		List<FileInfo> m_files = new List<FileInfo>();
		List<string> m_ResponseFiles = new List<string>();
		List<string> m_IncludedFiles = new List<string>();

		/// <summary>
		/// Maximum line length before wrap
		///  - set to zero for no line breaks
		///  - no guarantees, long strings won't be broken
		///    to enforce this, some operators may overhang
		///    by a character or two.
		/// </summary>
		public int MaxLineLength
		{
			get;
			set;
		}

		/// <summary>
		/// Enable/disable obfuscation of local symbols inside function closures
		/// </summary>
		public bool NoObfuscate
		{
			get;
			set;
		}

		/// <summary>
		///  Enable/disable replacement of consts variables
		/// </summary>
		public bool DetectConsts
		{
			get;
			set;
		}

		/// <summary>
		/// Enable/disable formatted output very rough formatting, just 
		/// enough to be vaguely readable for diagnostic purposes
		/// </summary>
		public bool Formatted
		{
			get;
			set;
		}

		/// <summary>
		///  Set to include diagnostic information about symbol obfuscation
		/// </summary>
		public bool SymbolInfo
		{
			get;
			set;
		}

		/// <summary>
		///  Set to dump the abstract syntax tree to stdout
		/// </summary>
		public bool DumpAST
		{
			get;
			set;
		}

		/// <summary>
		/// Set to dump scope information about all function scopes to stdout
		/// </summary>
		public bool DumpScopes
		{
			get;
			set;
		}

		/// <summary>
		/// Set to an encoding for the output file - defaults to the same encoding 
		/// as the first input file
		/// </summary>
		public Encoding OutputEncoding
		{
			get;
			set;
		}

		/// <summary>
		/// Set the output file name defaults to the name of the input file 
		/// with `.js` removed and `.js.min` appended
		/// </summary>
		public string OutputFileName
		{
			get;
			set;
		}

		/// <summary>
		///  Write to stdout instead of output file
		/// </summary>
		public bool StdOut
		{
			get;
			set;
		}

		/// <summary>
		///  When true, doesn't include the "Minified by MiniME" credit comment
		/// </summary>
		public bool NoCredit
		{
			get;
			set;
		}

		// When true checks the timestamp of all input files
		// and only regenerates output if something changed
		public bool CheckFileTimes
		{
			get;
			set;
		}


		public bool UseOptionsFile
		{
			get;
			set;
		}

		/// <summary>
		/// Sets the <see cref="System.IO.TextWriter"/> for all output. Defaults to stdout.
		/// </summary>
		public TextWriter StdOutWriter {
			get { return _outWriter; } 
			set { 
				_outWriter = value;
				Console.SetOut(_outWriter);
			} 
		}
		private TextWriter _outWriter = Console.Out;

		/// <summary>
		/// Get the supported option parameters and their default values.
		/// </summary>
		/// <param name="bWithIncludedFiles"></param>
		/// <returns>Supported option parameters and their default values</returns>
		public string CaptureOptions(bool bWithIncludedFiles)
		{
			var buf = new StringBuilder();

			// Options
			buf.AppendFormat("linelen:{0}\n", MaxLineLength);
			buf.AppendFormat("no-obfuscate:{0}\n", NoObfuscate);
			buf.AppendFormat("detect-consts:{0}\n", DetectConsts);
			buf.AppendFormat("formatted:{0}\n", Formatted);
			buf.AppendFormat("diag-symbols:{0}\n", SymbolInfo);
			buf.AppendFormat("output-encoding:{0}\n", OutputEncoding==null ? "null" : OutputEncoding.ToString());

			// File list
			buf.Append("files:\n");
			foreach (var f in m_files)
			{
				buf.Append(f.filename);
				buf.Append(System.IO.Path.PathSeparator);
				buf.Append(f.encoding==null ? "null" : f.encoding.ToString());
				buf.Append("\n");
			}

			// Included file list
			if (bWithIncludedFiles)
			{
				buf.Append("included:\n");
				foreach (var f in m_IncludedFiles)
				{
					buf.Append(f);
					buf.Append("\n");
				}
			}

			return buf.ToString();
		}


		/// <summary>
		/// Clears all files.
		/// </summary>
		public void Reset()
		{
			m_files.Clear();
		}

		public int FileCount
		{
			get
			{
				return m_files.Count;
			}
		}

		public void RegisterResponseFile(string strFileName)
		{
			m_ResponseFiles.Add(strFileName);
		}

		public void RegisterIncludedFile(string strFileName)
		{
			m_IncludedFiles.Add(strFileName);
		}

		// Add a file to be processed
		public void AddFiles(string strFileName, bool Warnings)
		{
			AddFile(strFileName, null, Warnings);
		}

		// Add a file to be processed (with explicit character encoding specified)
		public void AddFiles(string strFileName, System.Text.Encoding Encoding, bool Warnings)
		{
			// Work out directory
			string strDirectory=System.IO.Path.GetDirectoryName(strFileName);
			string strFile=System.IO.Path.GetFileName(strFileName);
			if (String.IsNullOrEmpty(strDirectory))
			{
				strDirectory = System.IO.Directory.GetCurrentDirectory();
			}
			else
			{
				strDirectory = System.IO.Path.GetFullPath(strDirectory);
			}

			// Wildcard?
			if (strFile.Contains('*') || strFile.Contains('?'))
			{
				var files=System.IO.Directory.GetFiles(strDirectory, strFile, SearchOption.TopDirectoryOnly);
				foreach (var f in files)
				{
					string strThisFile=System.IO.Path.Combine(strDirectory, f);

					if ((from fx in m_files where string.Compare(fx.filename, strThisFile, true) == 0 select fx).Count() > 0)
						continue;

					AddFile(strThisFile, Encoding, Warnings);
				}
			}
			else
			{
				AddFile(System.IO.Path.Combine(strDirectory, strFile), Encoding, Warnings);
			}
		}

		/// <summary>
		/// Add a file to be processed
		/// </summary>
		/// <param name="strFileName">The file path</param>
		/// <param name="Warnings">Enable lint warnings</param>
		public void AddFile(string strFileName, bool Warnings)
		{
			AddFile(strFileName, null, Warnings);
		}

		/// <summary>
		/// Add a file to be processed
		/// </summary>
		/// <param name="strFileName">The file path</param>
		/// <param name="Encoding">The encoding. Defaults to an encoding detection algorithm, and then <see cref="Encoding.UTF8"/> </param>
		/// <param name="Warnings">Enable lint warnings</param>
		public void AddFile(string strFileName, System.Text.Encoding Encoding, bool Warnings)
		{
			// Work out auto file encoding
			if (Encoding == null)
			{
				EncodingInfo e = TextFileUtils.DetectFileEncoding(strFileName);
				if (e != null)
					Encoding=e.GetEncoding();
			}

			// Use same encoding for output
			if (OutputEncoding != null)
				OutputEncoding = Encoding;
			else
			{
				Encoding = Encoding.UTF8;
			}

			// Workout minify kind
			if (MinifyKind == MinifyKind.Auto)
			{
				if (strFileName.EndsWith(".js", StringComparison.InvariantCultureIgnoreCase))
					MinifyKind = MinifyKind.JS;
				else if (strFileName.EndsWith(".css", StringComparison.InvariantCultureIgnoreCase))
					MinifyKind = MinifyKind.CSS;
			}

			// Add file info
			var i = new FileInfo();
			i.filename = strFileName;
			i.content = File.ReadAllText(strFileName, Encoding);
			i.encoding = Encoding;
			i.warnings = Warnings;
			m_files.Add(i);
		}

		/// <summary>
		/// Add javascript to be processed.
		/// </summary>
		/// <param name="strName">What you want to call the script.</param>
		/// <param name="strScript">The script contents. </param>
		/// <param name="Warnings">Enable lint warnings</param>
		public void AddScript(string strName, string strScript, bool Warnings)
		{
			var i = new FileInfo();
			i.filename = strName;
			i.content = strScript;
			i.encoding = Encoding.UTF8;
			i.warnings = Warnings;
			m_files.Add(i);
		}

		/// <summary>
		/// Compile all loaded scripts to a string. Can only compile EITHER JS or CSS, not both.
		/// </summary>
		/// <returns>The compiled scripts or styles.</returns>
		public string CompileToString()
		{
			return MinifyKind == MinifyKind.CSS ? CompileCssToString() : CompileJavascriptToString();
		}

		/// <summary>
		/// Compile all loaded scripts to a string, using the css minifier.
		/// </summary>
		/// <returns>The compiled styles.</returns>
		public string CompileCssToString()
		{
			// Step 1, concatenate all files
			var sb = new StringBuilder();
			foreach (var file in m_files)
			{
				Console.WriteLine("Processing {0}...", System.IO.Path.GetFileName(file.filename));
				sb.Append(file.content);
				sb.Append("\n");
			}

			// Do the CSS compression
			var minified = new CssMin().Minify(sb.ToString(), MaxLineLength);

			// Return it
			return minified;
		}

		/// <summary>
		/// Parses all loaded scripts as javascript and compiles them to a string.
		/// </summary>
		/// <returns>A string containing the minified javascript.</returns>
		public string CompileJavascriptToString()
		{
			// Create a symbol allocator
			SymbolAllocator SymbolAllocator = new SymbolAllocator(this);

			// Don't let the symbol allocator use any reserved words or common Javascript bits
			// We only go up to three letters - symbol allocation of more than 3 letters is 
			// highly unlikely.
			// (based on list here: http://www.quackit.com/javascript/javascript_reserved_words.cfm)
			string[] words = new string[] { "if", "in", "do", "for", "new", "var", "int", "try", "NaN", "ref", "sun", "top" };
			foreach (var s in words)
			{
				SymbolAllocator.ClaimSymbol(s);
			}

			// Create a member allocator
			SymbolAllocator MemberAllocator = new SymbolAllocator(this);

			// Render
			RenderContext r = new RenderContext(this, SymbolAllocator, MemberAllocator);
	
			// Process all files
			bool bNeedSemicolon = false;
			foreach (var file in m_files)
			{
				Console.WriteLine("Processing {0}...", System.IO.Path.GetFileName(file.filename));

				// Create a tokenizer and parser
				Warnings = new List<Warning>();
				WarningsEnabledStack = new Stack<bool>();
				Tokenizer t = new Tokenizer(this, file.content, file.filename, file.warnings);
				Parser p = new Parser(t);

				// Create the global statement block
				var code = new ast.CodeBlock(null, TriState.No);

				// Parse the file into a namespace
				p.ParseStatements(code);

				// Ensure everything processed
				if (t.more)
				{
					throw new CompileError("Unexpected end of file", t);
				}


				// Dump the abstract syntax tree
				if (DumpAST)
					code.Dump(0);

				// Create the root symbol scope and build scopes for all 
				// constained function scopes
				SymbolScope rootScope = new SymbolScope(this, null, Accessibility.Public);
				SymbolScope rootPseudoScope = new SymbolScope(this, null, Accessibility.Public);
				code.Visit(new VisitorScopeBuilder(rootScope, rootPseudoScope));

				// Combine consecutive var declarations into a single one
				code.Visit(new VisitorCombineVarDecl(rootScope));

				// Find all variable declarations
				code.Visit(new VisitorSymbolDeclaration(rootScope, rootPseudoScope));

				// Do lint stuff
				code.Visit(new VisitorLint(rootScope, rootPseudoScope));

				// Try to eliminate const declarations
				if (DetectConsts && !NoObfuscate)
				{
					code.Visit(new VisitorConstDetectorPass1(rootScope));
					code.Visit(new VisitorConstDetectorPass2(rootScope));
					code.Visit(new VisitorConstDetectorPass3(rootScope));
				}

				// Simplify expressions
				code.Visit(new VisitorSimplifyExpressions());

				// If obfuscation is allowed, find all in-scope symbols and then
				// count the frequency of their use.
				if (!NoObfuscate)
				{
					code.Visit(new VisitorSymbolUsage(rootScope));
				}

				// Process all symbol scopes, applying default accessibility levels
				// and determining the "rank" of each symbol
				rootScope.Prepare();

				// Dump scopes to stdout
				if (DumpScopes)
					rootScope.Dump(0);

				// Tell the global scope to claim all locally defined symbols
				// so they're not re-used (and therefore hidden) by the 
				// symbol allocation
				rootScope.ClaimSymbols(SymbolAllocator);

				// Create a credit comment on the first file
				if (!NoCredit && file==m_files[0])
				{
					int iInsertPos = 0;
					while (iInsertPos < code.Content.Count && code.Content[iInsertPos].GetType() == typeof(ast.StatementComment))
						iInsertPos++;
					code.Content.Insert(iInsertPos, new ast.StatementComment(null, "// Minified by MiniME from toptensoftware.com"));
				}

				if (bNeedSemicolon)
				{
					r.Append(";");
				}

				// Render it
				r.EnterScope(rootScope);
				bNeedSemicolon=code.Render(r);
				r.LeaveScope();

				// Display warnings
				Warnings.Sort(delegate(Warning w1, Warning w2)
				{
					int Compare = w1.Order.file.FileName.CompareTo(w2.Order.file.FileName);
					if (Compare == 0)
						Compare = w1.Order.position - w2.Order.position;
					if (Compare == 0)
						Compare = w1.OriginalOrder - w2.OriginalOrder;
					return Compare;
				});
				foreach (var w in Warnings)
				{
					Console.WriteLine("{0}: {1}", w.Bookmark, w.Message);
				}


			}

			// return the final script
			string strResult = r.GetGeneratedOutput();
			return strResult;
		}

		/// <summary>
		///  Compile all loaded files and write to the output file using default options
		/// </summary>
		public void Compile()
		{
			// Automatic output filename
			if (String.IsNullOrEmpty(OutputFileName) && m_files.Count>0)
			{
				string strFileName = m_files[0].filename;

				int dotpos = strFileName.LastIndexOf('.');
				if (dotpos >= 0)
					OutputFileName = strFileName.Substring(0, dotpos);

				OutputFileName += ".min.";

				if (MinifyKind == MinifyKind.CSS)
					OutputFileName += "css";
				else
					OutputFileName += "js";
			}

			string OptionsFile = OutputFileName + ".minime-options";

			if (!StdOut && CheckFileTimes && File.Exists(OutputFileName))
			{
				// Get the timestamp of the output file
				var dtOutput=System.IO.File.GetLastWriteTimeUtc(OutputFileName);

				// Compare with the timestamp of all the input files
				bool bNeedCompile=false;
				foreach (var f in m_files)
				{
					if (System.IO.File.GetLastWriteTimeUtc(f.filename) > dtOutput)
					{
						bNeedCompile = true;
						break;
					}
				}

				// Also check timestamp of any response files used
				if (!bNeedCompile)
				{
					foreach (var f in m_ResponseFiles)
					{
						if (System.IO.File.GetLastWriteTimeUtc(f)> dtOutput)
						{
							bNeedCompile = true;
							break;
						}
					}
				}

				// Also check if any options have changed
				if (!bNeedCompile && UseOptionsFile)
				{
					if (File.Exists(OptionsFile))
					{
						string oldOptions = File.ReadAllText(OptionsFile, Encoding.UTF8);

						int splitpos = oldOptions.IndexOf("included:");

						string included_files = "";
						if (splitpos>=0)
						{
							included_files = oldOptions.Substring(splitpos + 10);
							oldOptions = oldOptions.Substring(0, splitpos);
						}

						bNeedCompile = oldOptions != CaptureOptions(false);

						if (!bNeedCompile)
						{
							// Check if any included files changed
							foreach (var f in included_files.Split('\n', '\r'))
							{
								try
								{
									if (System.IO.File.GetLastWriteTimeUtc(f) > dtOutput)
									{
										bNeedCompile = true;
										break;
									}
								}
								catch (Exception)
								{
								}
							}
						}
					}
					else
					{
						bNeedCompile = true;
					}
				}

				if (!bNeedCompile)
				{
					Console.WriteLine("Nothing Changed");
					return;
				}
			}

			// Compile
			string str = CompileToString();

			// StdOut?
			if (StdOut)
			{
				Console.WriteLine(str);
				Console.WriteLine("");
				return;
			}

			// Write
			if (OutputEncoding!=null)
			{
				System.IO.File.WriteAllText(OutputFileName, str, OutputEncoding);
			}
			else
			{
				System.IO.File.WriteAllText(OutputFileName, str);
			}

			// Save options
			if (UseOptionsFile)
			{
				if (CheckFileTimes)
				{
					// Save options
					File.WriteAllText(OptionsFile, CaptureOptions(true), Encoding.UTF8);
				}
				else
				{
					// Delete an old options file
					if (File.Exists(OptionsFile))
						File.Delete(OptionsFile);
				}
			}
		}

		internal void RecordWarning(Bookmark position, string message, params object[] args)
		{
			RecordWarning(position, position, message, args);
		}

		internal void RecordWarning(Bookmark position, Bookmark order, string message, params object[] args)
		{
			// Quit warnings disabled at this point
			if (!position.warnings)
				return;

			// Create the warning record
			var warning = new Warning();
			warning.Bookmark = position;
			warning.Order = order;
			warning.Message = string.Format("warning: {0}", string.Format(message, args));
			warning.OriginalOrder = Warnings.Count;

			// Add to list
			Warnings.Add(warning);
		}

		class Warning
		{
			public Bookmark Bookmark;
			public Bookmark Order;
			public string Message;
			public int OriginalOrder;
		}

		List<Warning> Warnings;
		public Stack<bool> WarningsEnabledStack;

		// Stores information about a file to be processed
		class FileInfo
		{
			public string filename;
			public string content;
			public Encoding encoding;
			public bool warnings;
		}


	}
}
