using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a sequence of statements.
	// Used for global script scope, function body, catch clauses
	class CodeBlock : Node
	{
		// Constructor
		public CodeBlock(Bookmark bookmark, TriState hasBraces) : base(bookmark)
		{
			HasBraces = hasBraces;

		}

		// Attributes
		public TriState HasBraces;
		public List<Statement> Content = new List<Statement>();

		// Combine consecutive variable declarations into a single `var` statment
		public void CombineVarDecls()
		{
			for (int i = 1; i < Content.Count; i++)
			{
				if (Content[i - 1].GetType() == typeof(ast.StatementVariableDeclaration) &&
					Content[i].GetType() == typeof(ast.StatementVariableDeclaration))
				{
					var decl1 = (ast.StatementVariableDeclaration)Content[i - 1];
					var decl2 = (ast.StatementVariableDeclaration)Content[i];
					decl1.Variables.AddRange(decl2.Variables);
					Content.RemoveAt(i);
					i--;
				}
			}

		}

		public override void Dump(int indent)
		{
			foreach (var n in Content)
			{
				n.Dump(indent);
			}
		}

		public bool WillRenderBraces
		{
			get
			{
				return (HasBraces == TriState.Yes) || (HasBraces == TriState.Maybe && Content.Count != 1);
			}
		}

		public override bool Render(RenderContext dest)
		{
			// some statement blocks require braces, even if they only
			// contain a single statement

			// Opening brace
			if (WillRenderBraces)
			{
				dest.StartLine();
				dest.Append('{');
				dest.Indent();
				dest.StartLine();
			}

			// Render each statement, optionally putting a brace between them
			bool bNeedSemicolon = false;
			bool bUnreachable = false;
			for (var i=0; i<Content.Count; i++)
			{
				var s = Content[i];

				// Unreachable code?
				if (bUnreachable)
				{
					if (!s.IsDeclarationOnly())
					{
						dest.Compiler.RecordWarning(Content[i].Bookmark, "unreachable code");
						bUnreachable = false;
					}
				}

				// Pending semicolon?
				if (bNeedSemicolon)
					dest.Append(';');

				if (i>0)
					dest.StartLine();

				// Get the next statement and render it
				bNeedSemicolon=s.Render(dest);

				bUnreachable |= s.BreaksExecutionFlow();

				// In formatted mode, append the terminating semicolon immediately
				if (bNeedSemicolon && dest.Compiler.Formatted)
				{
					dest.Append(';');
					bNeedSemicolon = false;
				}
			}

			// Closing brace
			if (WillRenderBraces)
			{
				dest.Unindent();
				dest.StartLine();
				dest.Append('}');
				return false;
			}

			return bNeedSemicolon;
		}

		// Render this node in an indented block (if in formatted mode)
		public bool RenderIndented(RenderContext dest)
		{
			bool bIndent = false;

			if (dest.Compiler.Formatted)
			{
				bool Braces=(HasBraces == TriState.Yes) || (HasBraces == TriState.Maybe && Content.Count != 1);
				bIndent=!Braces;
			}

			if (bIndent)
			{
				dest.Indent();
				dest.StartLine();
				bool b = Render(dest);
				dest.Unindent();
				return b;
			}
			else
			{
				return Render(dest);
			}
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var c in Content)
			{
				c.Visit(visitor);
			}
		}

		public void AddStatement(Statement stmt)
		{
			// Ignore if null (typically from extra semicolon in source)
			if (stmt == null)
				return;

			Content.Add(stmt);
		}

	}
}
