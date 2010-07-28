using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// Walk the AST counting usage frequency of symbols
	//  - this is part 2 of what was started by VisitorSymbolDeclaration
	class VisitorSymbolUsage : ast.IVisitor
	{
		// Constructor
		public VisitorSymbolUsage(SymbolScope rootScope)
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

			if (n.GetType() == typeof(ast.ExprNodeIdentifier))
			{
				var m = (ast.ExprNodeIdentifier)n;
				if (m.Lhs == null)
				{
					currentScope.Symbols.UseSymbol(m.Name);
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

		public SymbolScope currentScope = null;
	}
}
