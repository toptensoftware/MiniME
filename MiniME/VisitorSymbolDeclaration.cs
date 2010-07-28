using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// Walk the AST, defining symbols on their containing scopes.
	//  - need to walk tree twice since variables can be declared after use
	//     - 1. to find symbol declarations (done by this class)
	//     - 2. to count the usage of symbols (that's done by VisitorSymbolUsage)
	class VisitorSymbolDeclaration : ast.IVisitor
	{
		// Constructor
		public VisitorSymbolDeclaration(SymbolScope rootScope)
		{
			currentScope = rootScope;
		}

		public void OnEnterNode(MiniME.ast.Node n)
		{
			// Define name of function in outer scope, before descending
			if (n.GetType() == typeof(ast.ExprNodeFunction))
			{
				var fn = (ast.ExprNodeFunction)n;

				// Define a symbol for the new function
				if (!String.IsNullOrEmpty(fn.Name))
				{
					currentScope.Symbols.DefineSymbol(fn.Name);
				}
			}

			// Descending into an inner scope
			if (n.Scope != null)
			{
				System.Diagnostics.Debug.Assert(n.Scope.OuterScope == currentScope);
				currentScope = n.Scope;
			}


			// Define catch clause exception variables in the inner scope
			if (n.GetType() == typeof(ast.CatchClause))
			{
				var cc = (ast.CatchClause)n;
				currentScope.Symbols.DefineSymbol(cc.ExceptionVariable);
				return;
			}

			// Define variables in the current scope
			if (n.GetType() == typeof(ast.StatementVariableDeclaration))
			{
				var vardecl = (ast.StatementVariableDeclaration)n;
				foreach (var v in vardecl.Variables)
					currentScope.Symbols.DefineSymbol(v.Name);
				return;
			}

			// Define parameters in the current scope
			if (n.GetType() == typeof(ast.Parameter))
			{
				var p = (ast.Parameter)n;
				currentScope.Symbols.DefineSymbol(p.Name);
				return;
			}
		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.Scope != null)
			{
				currentScope = n.Scope.OuterScope;
			}
		}

		SymbolScope currentScope;
	}
}
