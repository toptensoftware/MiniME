using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class VisitorConstDetector : ast.IVisitor
	{
		public VisitorConstDetector(SymbolScope rootScope)
		{
			currentScope = rootScope;
		}

		public void OnEnterNode(MiniME.ast.Node n)
		{
			// Descending into an inner scope
			if (n.Scope != null)
			{
				System.Diagnostics.Debug.Assert(n.Scope.OuterScope == currentScope);
				currentScope = n.Scope;
			}

			// Is is a "var <name>=<literal_int_or_double>"
			if (n.GetType() == typeof(ast.StatementVariableDeclaration))
			{
				var decl = (ast.StatementVariableDeclaration)n;
				if (decl.InitialValue != null)
				{
					object literal = decl.InitialValue.EvalConstLiteral();
					if (literal != null)
					{

					}
				}
			}


		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.Scope != null)
			{
				currentScope = n.Scope.OuterScope;
			}
		}

		public SymbolScope currentScope;
	}
}
