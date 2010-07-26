using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/*
 * TODO:
 * 
 * - Prevent obfuscation when 'with' or 'eval' detected
 * - Obfuscation of catch block exception variable
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

		public TextWriter Output
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

		public void Compile()
		{
			foreach (var file in m_files)
			{
				Console.WriteLine("Processing {0}...", file.filename);

				// Create a tokenizer and parser
				Tokenizer t = new Tokenizer(file.content, file.filename);
				Parser p = new Parser(t);

				// Parse the file into a namespace
				var statements = new ast.StatementBlock();
				p.ParseStatements(statements);
				statements.GlobalScope = false;

				if (t.more)
				{
					throw new CompileError("Unexpected EOF", t);
				}

				//statements.Dump(0);
				SymbolScope rootScope = new SymbolScope(null);
				statements.Visit(new SymbolDeclarationVisitor(rootScope));
				statements.Visit(new SymbolUsageVisitor(rootScope));
				rootScope.PrepareSymbolRanks();

//				rootScope.Dump(0);

				RenderContext r = new RenderContext();
				rootScope.ClaimSymbols(r);

				// Claim all 2 and 3 letter reserved words/keywords
				// (based on list here: http://www.quackit.com/javascript/javascript_reserved_words.cfm)
				string[] words = new string[] {"if", "in", "do", "for", "new", "var", "int", "try", "NaN", "ref", "sun", "top" };
				foreach (var s in words)
				{
					r.Symbols.ClaimSymbol(s);
				}

				r.Formatted = true;
				r.DebugInfo = true;
				statements.Render(r);
				Console.WriteLine(r.FinalScript());
			}
		}

		class FileInfo
		{
			public string filename;
			public string content;
		}

		List<FileInfo> m_files=new List<FileInfo>();
	}
}
