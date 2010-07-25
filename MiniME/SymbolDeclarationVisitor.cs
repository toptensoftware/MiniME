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
				if (m_Scopes.Count > 0)
				{
					m_Scopes.Peek().NestedScopes.Add(fn.Scope);
				}

				// Define a symbol for the new function
				if (!String.IsNullOrEmpty(fn.Name))
				{
					DefineSymbol(fn.Name);
				}

				// Enter scope
				m_Scopes.Push(fn.Scope);

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
			if (n.GetType()==typeof(ast.ExprNodeFunction))
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
