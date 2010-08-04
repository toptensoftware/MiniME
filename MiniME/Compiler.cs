using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/*
 * TODO:
 * - Ability to change directives prefix `-prefix:mm`
 * - A directive to declare external symbols that shouldn't be re-used	(or just use var keyword?) // mm:var 
 * - Ability to obfuscate global scope variables
 * - Ability to remove credit comment `-ivedonated`
 * - Warning on unreachable code (after return, break or continue)
 * - Warning on unused locals  (functions and variable - not parameters)
 * - Automatic member privatisation by simple prefix  // mm:private
 * - Privatisation of object literals
 
 * - Ability to display a list of non-obfuscated symbols `-whatsleft`
 * - Ability to show summary of what was done `-whathappened`
 * - Diagnostic mode to reparse generated content `-diag-selfcheck`
 * - Better optimizations of constant expressions
 * - Ability to not obfuscate specific instances of members (use target.['member'])
 */

namespace MiniME
{
	// Main api into the MiniME minifier/obfuscator
	public class Compiler
	{
		// Constructor
		public Compiler()
		{
			Reset();
			DetectConsts = true;
			MaxLineLength = 120;
		}

		// Attributes
		List<FileInfo> m_files = new List<FileInfo>();

		// Maximum line length before wrap
		//  - set to zero for no line breaks
		//  - no guarantees, long strings won't be broken
		//    to enforce this, some operators may overhang
		//    by a character or two.
		public int MaxLineLength
		{
			get;
			set;
		}

		// Enable/disable obfuscation of local symbols inside
		// function closures
		public bool NoObfuscate
		{
			get;
			set;
		}

		// Enable/disable replacement of consts variables
		public bool DetectConsts
		{
			get;
			set;
		}

		// Enable/disable formatted output
		//  - very rough formatting, just enough to be vaguely readable
		//    for diagnostic purposes
		public bool Formatted
		{
			get;
			set;
		}

		// Set to include diagnostic information about symbol obfuscation
		public bool SymbolInfo
		{
			get;
			set;
		}

		// Set to dump the abstract syntax tree to stdout
		public bool DumpAST
		{
			get;
			set;
		}

		// Set to dump scope information about all function scopes to stdout
		public bool DumpScopes
		{
			get;
			set;
		}

		// Set to an encoding for the output file
		//  - defaults to the same encoding as the first input file
		public Encoding OutputEncoding
		{
			get;
			set;
		}

		// Set the output file name
		//  - defaults to the name of the input file with `.js` removed
		//    and `.js.min` appended
		public string OutputFileName
		{
			get;
			set;
		}

		// When true, don't include the "Minified by MiniME" credit comment
		public bool NoCredit
		{
			get;
			set;
		}

		// Reset this compiler
		public void Reset()
		{
			m_files.Clear();
		}

		// Add a file to be processed
		public void AddFile(string strFileName)
		{
			AddFile(strFileName, null);
		}

		// Add a file to be processed (with explicit character encoding specified)
		public void AddFile(string strFileName, System.Text.Encoding Encoding)
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


			// Automatic output filename
			if (String.IsNullOrEmpty(OutputFileName))
			{
				int dotpos = strFileName.LastIndexOf('.');
				if (dotpos >= 0)
					OutputFileName = strFileName.Substring(0, dotpos);
				OutputFileName += ".min.js";
			}

			// Add file info
			var i = new FileInfo();
			i.filename = strFileName;
			i.content = File.ReadAllText(strFileName, Encoding);
			m_files.Add(i);
		}

		// Add Javascript code to be processed, direct from string
		public void AddScript(string strName, string strScript)
		{
			var i = new FileInfo();
			i.filename = strName;
			i.content = strScript;
			m_files.Add(i);
		}

		// Compile all loaded script to a string
		public string CompileToString()
		{
			// Create the global statement block
			//  turn off braces
			var code = new ast.CodeBlock(null, TriState.No);

			// Process all files
			foreach (var file in m_files)
			{
				Console.WriteLine("Processing {0}...", file.filename);

				// Create a tokenizer and parser
				Tokenizer t = new Tokenizer(file.content, file.filename);
				Parser p = new Parser(t);

				// Parse the file into a namespace
				p.ParseStatements(code);

				// Ensure everything processed
				if (t.more)
				{
					throw new CompileError("Unexpected end of file", t);
				}
			}

			// Dump the abstract syntax tree
			if (DumpAST)
				code.Dump(0);

			// Create the root symbol scope and build scopes for all 
			// constained function scopes
			SymbolScope rootScope = new SymbolScope(null, Accessibility.Public);
			code.Visit(new VisitorScopeBuilder(rootScope));

			// Combine consecutive var declarations into a single one
			code.Visit(new VisitorCombineVarDecl(rootScope));

			// Find all variable declarations
			code.Visit(new VisitorSymbolDeclaration(rootScope));

			// Try to eliminate const declarations
			if (DetectConsts && !NoObfuscate)
			{
				code.Visit(new VisitorConstDetectorPass1(rootScope));
				code.Visit(new VisitorConstDetectorPass2(rootScope));
				code.Visit(new VisitorConstDetectorPass3(rootScope));
			}

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

			// Create a symbol allocator
			SymbolAllocator SymbolAllocator = new SymbolAllocator(this);

			// Tell the global scope to claim all locally defined symbols
			// so they're not re-used (and therefore hidden) by the 
			// symbol allocation
			rootScope.ClaimSymbols(SymbolAllocator);

			// Don't let the symbol allocator use any reserved words or common Javascript bits
			// We only go up to three letters - symbol allocation of more than 3 letters is 
			// highly unlikely.
			// (based on list here: http://www.quackit.com/javascript/javascript_reserved_words.cfm)
			string[] words = new string[] {"if", "in", "do", "for", "new", "var", "int", "try", "NaN", "ref", "sun", "top" };
			foreach (var s in words)
			{
				SymbolAllocator.ClaimSymbol(s);
			}

			// Create a member allocator
			SymbolAllocator MemberAllocator = new SymbolAllocator(this);

			// Render
			RenderContext r = new RenderContext(this, SymbolAllocator, MemberAllocator, rootScope);

			// Create a credit comment
			if (!NoCredit)
			{
				int iInsertPos = 0;
				while (iInsertPos < code.Content.Count && code.Content[iInsertPos].GetType() == typeof(ast.StatementComment))
					iInsertPos++;
				code.Content.Insert(iInsertPos, new ast.StatementComment(null, "// Minified by MiniME from toptensoftware.com"));
			}

			code.Render(r);

			string strResult = r.GetGeneratedOutput();

			// return the final script
			return strResult;
		}

		// Compile all loaded files and write to the output file
		public void Compile()
		{
			// Compile
			string str = CompileToString();

			// Write
			if (OutputEncoding!=null)
			{
				System.IO.File.WriteAllText(OutputFileName, str, OutputEncoding);
			}
			else
			{
				System.IO.File.WriteAllText(OutputFileName, str);
			}
		}

		// Stores information about a file to be processed
		class FileInfo
		{
			public string filename;
			public string content;
		}

	}
}
