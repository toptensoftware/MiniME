using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class ScopeConstInfo
	{
		public ScopeConstInfo()
		{
		}

		class ConstInfo
		{
			public ast.StatementVariableDeclaration vardecl;
			public ast.StatementVariableDeclaration.Variable variable;
		}

		Dictionary<string, ConstInfo> m_Constants = new Dictionary<string, ConstInfo>();

		public void StorePossibleConst(ast.StatementVariableDeclaration vardecl, ast.StatementVariableDeclaration.Variable variable)
		{
			var ci = new ConstInfo();
			ci.vardecl = vardecl;
			ci.variable = variable;
			m_Constants.Add(variable.Name, ci);

		}

		public void RejectConst(string Name)
		{
			if (m_Constants.ContainsKey(Name))
			{
				m_Constants.Remove(Name);
			}
		}

	}

	class VisitorConstDetectorPass1 : ast.IVisitor
	{
		public VisitorConstDetectorPass1(SymbolScope rootScope)
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

			// Is is a "var <name>=<literal_int_or_double>", inside a function
			if (currentScope.OuterScope!=null && n.GetType() == typeof(ast.StatementVariableDeclaration))
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

					// Get const info for this class
					var ci = currentScope.GetVisitorData<ScopeConstInfo>();

					// Store a possible const declaration
					ci.StorePossibleConst(vardecl, v);
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


	class VisitorConstDetectorPass2 : ast.IVisitor
	{
		public VisitorConstDetectorPass2(SymbolScope rootScope)
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

			// Look for assignments to property
			if (n.GetType() == typeof(ast.ExprNodeBinary))
			{
				// Is it an assignment
				var binOp = (ast.ExprNodeBinary)n;
				if (binOp.Op >= Token.assign && binOp.Op <= Token.bitwiseAndAssign)
				{
					RejectConstVariable(binOp.Lhs);
				}
			}

			// Look for increment/decrement operators
			if (n.GetType() == typeof(ast.ExprNodeUnary))
			{
				var oneOp = (ast.ExprNodeUnary)n;
				if (oneOp.Op == Token.increment || oneOp.Op == Token.decrement)
				{
					RejectConstVariable(oneOp.Rhs);
				}
			}

			// Postfix too
			if (n.GetType() == typeof(ast.ExprNodePostfix))
			{
				var oneOp = (ast.ExprNodePostfix)n;
				if (oneOp.Op == Token.increment || oneOp.Op == Token.decrement)
				{
					RejectConstVariable(oneOp.Lhs);
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

		public void RejectConstVariable(ast.ExpressionNode node)
		{
			// Is the lhs a global?
			if (node.GetType() != typeof(ast.ExprNodeIdentifier))
				return;

			// Check it's not a member accessor
			var identifier = (ast.ExprNodeIdentifier)node;
			if (identifier.Lhs != null)
				return;

			// Walk all outer scopes until we find a possible const declaration for it
			// and mark it as rejected
			var s = currentScope.FindScopeOfSymbol(identifier.Name);
			if (s != null)
			{
				s.GetVisitorData<ScopeConstInfo>().RejectConst(identifier.Name);
			}
		}


		public SymbolScope currentScope;
	}
}
