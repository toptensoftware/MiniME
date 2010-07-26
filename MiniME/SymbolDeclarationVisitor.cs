using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class SymbolDeclarationVisitor : ast.IVisitor
	{
		public SymbolDeclarationVisitor(SymbolScope rootScope)
		{
			m_Scopes.Push(rootScope);
		}

		public void OnEnterNode(MiniME.ast.Node n)
		{
			// Is it a function?
			if (n.GetType() == typeof(ast.ExprNodeFunction))
			{
				var fn = (ast.ExprNodeFunction)n;

				fn.Scope = new SymbolScope(fn);

				// Add this function to the parent function's list of nested functions
				m_Scopes.Peek().NestedScopes.Add(fn.Scope);

				// Define a symbol for the new function
				if (!String.IsNullOrEmpty(fn.Name))
				{
					DefineSymbol(fn.Name);
				}

				// Enter scope
				m_Scopes.Push(fn.Scope);

			}

			// Is it a CatchClause?
			if (n.GetType() == typeof(ast.CatchClause))
			{
				var cc = (ast.CatchClause)n;

				cc.Scope = new SymbolScope(cc);

				// Add this function to the parent function's list of nested functions
				if (m_Scopes.Count > 0)
				{
					m_Scopes.Peek().NestedScopes.Add(cc.Scope);
				}

				// Enter scope
				m_Scopes.Push(cc.Scope);

				DefineSymbol(cc.ExceptionVariable);
			}

			if (n.GetType() == typeof(ast.StatementVariableDeclaration))
			{
				var v = (ast.StatementVariableDeclaration)n;
				DefineSymbol(v.Name);
			}

			if (n.GetType() == typeof(ast.Parameter))
			{
				var p = (ast.Parameter)n;
				DefineSymbol(p.Name);
			}
		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.GetType() == typeof(ast.ExprNodeFunction))
			{
				m_Scopes.Pop();
			}
			if (n.GetType() == typeof(ast.CatchClause))
			{
				m_Scopes.Pop();
			}
		}

		void DefineSymbol(string str)
		{
			m_Scopes.Peek().Symbols.DefineSymbol(str);
		}

		public Stack<SymbolScope> m_Scopes=new Stack<SymbolScope>();
	}
}
