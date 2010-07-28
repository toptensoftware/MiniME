using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Represents a sequence of statements.
	// Used for global script scope, function body, catch clauses
	class StatementBlock : Statement
	{
		// Constructor
		public StatementBlock()
		{

		}

		// Attributes
		public bool HasBraces = true;
		public List<Statement> Content = new List<Statement>();

		// If a statement block contains child statement blocks, roll them
		// into the parent block.  Braces don't create scopes in Javascript
		// so the additional braces are redundant
		public void RemoveRedundant()
		{
			for (int i=0; i<Content.Count; i++)
			{
				if (Content[i].GetType() == typeof(StatementBlock))
				{
					// yes, take it's content an replace it here
					StatementBlock child = (StatementBlock)Content[i];
					Content.InsertRange(i + 1, child.Content);
					Content.RemoveAt(i);
					i--;
				}
			}
		}

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

		public override bool Render(RenderContext dest)
		{
			// some statement blocks require braces, even if they only
			// contain a single statement

			// Opening brace
			if (HasBraces)
			{
				dest.StartLine();
				dest.Append('{');
				dest.Indent();
			}

			// Render each statement, optionally putting a brace between them
			bool bNeedSemicolon = false;
			for (var i=0; i<Content.Count; i++)
			{
				// Pending semicolon?
				if (bNeedSemicolon)
					dest.Append(';');

				// Get the next statement and render it
				var s = Content[i];
				dest.StartLine();
				bNeedSemicolon=s.Render(dest);

				// In formatted mode, append the terminating semicolon immediately
				if (bNeedSemicolon && dest.Compiler.Formatted)
				{
					dest.Append(';');
					bNeedSemicolon = false;
				}
			}

			// Closing brace
			if (HasBraces)
			{
				dest.Unindent();
				dest.StartLine();
				dest.Append('}');
				return false;
			}

			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var c in Content)
			{
				c.Visit(visitor);
			}
		}

	}
}
