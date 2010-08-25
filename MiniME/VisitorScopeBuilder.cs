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
		public VisitorScopeBuilder(SymbolScope rootScope, SymbolScope rootPseudoScope)
		{
			m_Scopes.Push(rootScope);
			m_PseudoScopes.Push(rootPseudoScope);
		}

		public void EnterPseudoScope(ast.Node n)
		{
			SymbolScope currentScope = m_PseudoScopes.Peek();

			n.PseudoScope = new SymbolScope(currentScope.Compiler, n, Accessibility.Private);

			currentScope.InnerScopes.Add(n.PseudoScope);
			n.PseudoScope.OuterScope = currentScope;

			m_PseudoScopes.Push(n.PseudoScope);
		}

		public bool OnEnterNode(ast.Node n)
		{
			// New actual scope (function body or catch clause)
			if (n.GetType() == typeof(ast.ExprNodeFunction) || n.GetType() == typeof(ast.CatchClause))
			{
				SymbolScope currentScope = m_Scopes.Peek();

				n.Scope = new SymbolScope(currentScope.Compiler, n, Accessibility.Private);

				// Add this function to the parent function's list of nested functions
				currentScope.InnerScopes.Add(n.Scope);
				n.Scope.OuterScope = currentScope;

				// Enter scope
				m_Scopes.Push(n.Scope);

				// Also create a pseudo scope
				EnterPseudoScope(n);

				return true;
			}

			// New pseudo scope (statement body or braced code block)
			if (n.GetType() == typeof(ast.CodeBlock) || n.GetType() == typeof(ast.StatementBlock) || n.GetType()==typeof(ast.StatementFor) || n.GetType()==typeof(ast.StatementForIn))
			{
				// Create pseudo scope
				EnterPseudoScope(n);
				return true;
			}

			// Is it an evil?
			if (n.GetType() == typeof(ast.StatementWith))
			{
				m_Scopes.Peek().Compiler.RecordWarning(n.Bookmark, "use of `with` statement prevents local symbol obfuscation of all containing scopes");
				m_Scopes.Peek().DefaultAccessibility = Accessibility.Public;
				return true;
			}

			// More evil
			if (n.GetType() == typeof(ast.ExprNodeIdentifier))
			{
				var m = (ast.ExprNodeIdentifier)n;
				if (m.Lhs == null && m.Name == "eval")
				{
					m_Scopes.Peek().Compiler.RecordWarning(n.Bookmark, "use of `eval` prevents local symbol obfuscation of all containing scopes");
					m_Scopes.Peek().DefaultAccessibility = Accessibility.Public;
				}
				return true;
			}

			// Private member declaration?
			if (n.GetType() == typeof(ast.StatementAccessibility))
			{
				var p = (ast.StatementAccessibility)n;
				foreach (var s in p.Specs)
				{
					m_Scopes.Peek().AddAccessibilitySpec(p.Bookmark, s);
				}
			}

			// Try to guess name of function by assignment in variable declaration
			var decl = n as ast.StatementVariableDeclaration;
			if (decl != null)
			{
				foreach (var i in decl.Variables)
				{
					if (i.InitialValue!=null)
					{
						var fn = i.InitialValue.RootNode as ast.ExprNodeFunction;
						if (fn != null)
						{
							fn.AssignedToName = i.Name;
						}
					}
				}
			}

			// Try to guess name of function by assignment
			var assignment = n as ast.ExprNodeAssignment;
			if (assignment != null)
			{
				var fn = assignment.Rhs as ast.ExprNodeFunction;
				if (fn != null)
				{
					var id = assignment.Lhs as ast.ExprNodeIdentifier;
					if (id!=null)
						fn.AssignedToName = id.Name;
				}
			}

			// Try to guess name of function in object literal
			var objLiteral = n as ast.ExprNodeObjectLiteral;
			if (objLiteral != null)
			{
				foreach (var i in objLiteral.Values)
				{
					var fn = i.Value as ast.ExprNodeFunction;
					if (fn != null)
					{
						var id = i.Key as ast.ExprNodeIdentifier;
						if (id != null)
						{
							fn.AssignedToName = id.Name;
						}
					}
				}
			}

			return true;
		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.Scope!=null)
			{
				System.Diagnostics.Debug.Assert(m_Scopes.Peek() == n.Scope);

				// Check if scope contained evil
				Accessibility innerAccessibility = m_Scopes.Peek().DefaultAccessibility;

				// Pop the stack
				m_Scopes.Pop();

				// Propagate evil
				if (innerAccessibility==Accessibility.Public)
					m_Scopes.Peek().DefaultAccessibility = Accessibility.Public;
			}

			if (n.PseudoScope != null)
			{
				System.Diagnostics.Debug.Assert(m_PseudoScopes.Peek() == n.PseudoScope);
				m_PseudoScopes.Pop();
			}
		}

		public Stack<SymbolScope> m_Scopes = new Stack<SymbolScope>();
		public Stack<SymbolScope> m_PseudoScopes = new Stack<SymbolScope>();
	}
}
