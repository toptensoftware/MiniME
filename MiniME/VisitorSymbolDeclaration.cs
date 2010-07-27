using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class VisitorSymbolDeclaration : ast.IVisitor
	{
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
					DefineSymbol(fn.Name);
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
				DefineSymbol(cc.ExceptionVariable);
				return;
			}

			// Define variables in the current scope
			if (n.GetType() == typeof(ast.StatementVariableDeclaration))
			{
				var v = (ast.StatementVariableDeclaration)n;
				DefineSymbol(v.Name);
				return;
			}

			// Define parameters in the current scope
			if (n.GetType() == typeof(ast.Parameter))
			{
				var p = (ast.Parameter)n;
				DefineSymbol(p.Name);
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

		void DefineSymbol(string str)
		{
			currentScope.Symbols.DefineSymbol(str);
		}

		SymbolScope currentScope;
	}
}
