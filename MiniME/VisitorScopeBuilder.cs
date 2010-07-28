using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// AST Visitor to create the SymbolScope object heirarchy
	//  - scopes are created for functions and catch clauses
	//  - also detects evil and marks scopes as such
	class VisitorScopeBuilder : ast.IVisitor
	{
		// Constructor
		public VisitorScopeBuilder(SymbolScope rootScope)
		{
			m_Scopes.Push(rootScope);
		}

		public void OnEnterNode(MiniME.ast.Node n)
		{
			// Is it a function?
			if (n.GetType() == typeof(ast.ExprNodeFunction) || n.GetType() == typeof(ast.CatchClause))
			{
				n.Scope = new SymbolScope(n);

				// Add this function to the parent function's list of nested functions
				m_Scopes.Peek().InnerScopes.Add(n.Scope);
				n.Scope.OuterScope = m_Scopes.Peek();

				// Enter scope
				m_Scopes.Push(n.Scope);

				return;
			}

			// Is it an evil?
			if (n.GetType() == typeof(ast.StatementWith))
			{
				m_Scopes.Peek().ContainsEvil = true;
				return;
			}

			// More evil
			if (n.GetType() == typeof(ast.ExprNodeIdentifier))
			{
				var m = (ast.ExprNodeIdentifier)n;
				if (m.Lhs == null && m.Name == "eval")
				{
					m_Scopes.Peek().ContainsEvil = true;
				}
				return;
			}
		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.Scope!=null)
			{
				System.Diagnostics.Debug.Assert(m_Scopes.Peek() == n.Scope);

				// Check if scope contained evil
				bool bEvil = m_Scopes.Peek().ContainsEvil;

				// Pop the stack
				m_Scopes.Pop();

				// Propagate evil
				if (bEvil)
					m_Scopes.Peek().ContainsEvil = true;
			}
		}

		public Stack<SymbolScope> m_Scopes=new Stack<SymbolScope>();
	}
}
