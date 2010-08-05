using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	// Pass 1. Detect all `var <identifier>=<constantexpression>` and store the 
	//         const value on the Symbol entry
	class VisitorConstDetectorPass1 : ast.IVisitor
	{
		public VisitorConstDetectorPass1(SymbolScope rootScope)
		{
			currentScope = rootScope;
		}

		public bool OnEnterNode(MiniME.ast.Node n)
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
					object val = v.InitialValue.RootNode.EvalConstLiteral();
					if (val==null)
						continue;

					// Must be a number
					if (val.GetType() != typeof(long) && val.GetType() != typeof(DoubleLiteral))
						continue;

					// Find the symbol in the current scope
					Symbol s = currentScope.Symbols.FindLocalSymbol(v.Name);
					System.Diagnostics.Debug.Assert(s != null);

					// Store the constant value for this symbol
					if (s.ConstValue == null && s.ConstAllowed)
					{
						s.ConstValue = val;
					}
					else
					{
						s.ConstAllowed = false;
						s.ConstValue = null;
					}
				}
			}

			return true;
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


	// Pass 2. Find all assignments to `<identifier>` or `<identifier>++` or `++<identifier>`
	//  	   	- find scope in which variable is defined
	//		   	- zap the stored const value on the SymbolEntry
	//  	   	- mark the symbol as not being eligible for further const declarations
	//				  (in case there's a subsequent var decl the also looks like a const)
	class VisitorConstDetectorPass2 : ast.IVisitor
	{
		public VisitorConstDetectorPass2(SymbolScope rootScope)
		{
			currentScope = rootScope;
		}

		public bool OnEnterNode(MiniME.ast.Node n)
		{
			// Descending into an inner scope
			if (n.Scope != null)
			{
				System.Diagnostics.Debug.Assert(n.Scope.OuterScope == currentScope);
				currentScope = n.Scope;
			}

			// Look for assignments to property
			if (n.GetType() == typeof(ast.ExprNodeAssignment))
			{
				// Is it an assignment
				var assignOp = (ast.ExprNodeAssignment)n;
				RejectConstVariable(assignOp.Lhs);
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

			return true;

		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.Scope != null)
			{
				currentScope = n.Scope.OuterScope;
			}
		}

		public void RejectConstVariable(ast.ExprNode node)
		{
			// Is the lhs a global?
			if (node.GetType() != typeof(ast.ExprNodeIdentifier))
				return;

			// Check it's not a member accessor
			var identifier = (ast.ExprNodeIdentifier)node;
			if (identifier.Lhs != null)
				return;

			// Find the symbol and mark it as not a const
			var s = currentScope.FindSymbol(identifier.Name);
			if (s != null)
			{
				s.ConstValue = null;
				s.ConstAllowed = false;
			}
		}


		public SymbolScope currentScope;
	}


	// Pass 3. Remove variable declarations for any variables that are consts.
	class VisitorConstDetectorPass3 : ast.IVisitor
	{
		public VisitorConstDetectorPass3(SymbolScope rootScope)
		{
			currentScope = rootScope;
		}

		public bool OnEnterNode(MiniME.ast.Node n)
		{
			// Descending into an inner scope
			if (n.Scope != null)
			{
				System.Diagnostics.Debug.Assert(n.Scope.OuterScope == currentScope);
				currentScope = n.Scope;
			}

			// Is it a variable declaration
			if (n.GetType() == typeof(ast.StatementVariableDeclaration))
			{
				var vardecl = (ast.StatementVariableDeclaration)n;

				for (int i = vardecl.Variables.Count - 1; i >= 0; i-- )
				{
					var v = vardecl.Variables[i];

					// Find the symbol (must exist in current scope)
					var s = currentScope.Symbols.FindLocalSymbol(v.Name);
					System.Diagnostics.Debug.Assert(s != null);

					// Is it a const?
					if (s.ConstValue != null)
					{
						// Yes!  Remove it 
						vardecl.Variables.RemoveAt(i);
					}
				}
			}

			return true;
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
