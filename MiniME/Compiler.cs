using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

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

				rootScope.Dump(0);

				StringBuilder sb = new StringBuilder();
				statements.Render(sb);
				Console.WriteLine(sb.ToString());
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
