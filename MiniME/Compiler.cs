using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/*
 * TODO:
 * 
 * - Optimization of constants
 * - Collapse variables declarations into a single var statement
 * - Diagnostic mode to reparse generated content
 * - Unit test cases
 * - Comments to prevent use of symbols //mm-reserve:top
 * 
 */

namespace MiniME
{
	public class Compiler
	{
		public Compiler()
		{
			Reset();
		}

		public int MaxLineLength
		{
			get;
			set;
		}

		public bool NoObfuscate
		{
			get;
			set;
		}

		public bool Formatted
		{
			get;
			set;
		}

		public bool SymbolInfo
		{
			get;
			set;
		}

		public bool DumpAST
		{
			get;
			set;
		}

		public bool DumpScopes
		{
			get;
			set;
		}

		public void Reset()
		{
			m_files.Clear();
		}

		public void AddFile(string strFileName)
		{
			var i=new FileInfo();
			i.filename=strFileName;
			i.content=File.ReadAllText(strFileName);
			m_files.Add(i);
		}

		public string Compile()
		{
			var statements = new ast.StatementBlock();
			statements.GlobalScope = false;

			foreach (var file in m_files)
			{
				Console.WriteLine("Processing {0}...", file.filename);

				// Create a tokenizer and parser
				Tokenizer t = new Tokenizer(file.content, file.filename);
				Parser p = new Parser(t);

				// Parse the file into a namespace
				p.ParseStatements(statements);

				if (t.more)
				{
					throw new CompileError("Unexpected EOF", t);
				}
			}

			if (DumpAST)
				statements.Dump(0);

			SymbolScope rootScope = new SymbolScope(null);
			statements.Visit(new VisitorScopeBuilder(rootScope));
			if (!NoObfuscate)
			{
				statements.Visit(new VisitorSymbolDeclaration(rootScope));
				statements.Visit(new VisitorSymbolUsage(rootScope));
			}
			statements.Visit(new VisitorConstDetector(rootScope));
			rootScope.PrepareSymbolRanks();

			if (DumpScopes)
				rootScope.Dump(0);

			RenderContext r = new RenderContext(this);

			r.Symbols.ShowOriginalSymbols = this.SymbolInfo;
			rootScope.ClaimSymbols(r);

			// Claim all 2 and 3 letter reserved words/keywords
			// (based on list here: http://www.quackit.com/javascript/javascript_reserved_words.cfm)
			string[] words = new string[] {"if", "in", "do", "for", "new", "var", "int", "try", "NaN", "ref", "sun", "top" };
			foreach (var s in words)
			{
				r.Symbols.ClaimSymbol(s);
			}

			statements.Render(r);

			return r.FinalScript();
		}

		class FileInfo
		{
			public string filename;
			public string content;
		}

		List<FileInfo> m_files=new List<FileInfo>();
	}
}
