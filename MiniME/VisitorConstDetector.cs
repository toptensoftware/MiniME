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
				var vardecl = (ast.StatementVariableDeclaration)n;
				foreach (var v in vardecl.Variables)
				{
					// Must have initial value
					if (v.InitialValue == null)
						continue;

					// Must evaluate to a constant
					object val = v.InitialValue.EvalConstLiteral();
					if (val==null)
						continue;

					// Must be a number
					if (val.GetType() != typeof(long) && val.GetType() != typeof(DoubleLiteral))
						continue;


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
