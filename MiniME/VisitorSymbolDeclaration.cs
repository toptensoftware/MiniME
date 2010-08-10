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

		public bool OnEnterNode(MiniME.ast.Node n)
		{
			// Define name of function in outer scope, before descending
			if (n.GetType() == typeof(ast.ExprNodeFunction))
			{
				var fn = (ast.ExprNodeFunction)n;

				// Define a symbol for the new function
				if (!String.IsNullOrEmpty(fn.Name))
				{
					currentScope.Symbols.DefineSymbol(fn.Name);
					currentScope.ProcessAccessibilitySpecs(fn.Name);
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
				return true;
			}

			// Define variables in the current scope
			if (n.GetType() == typeof(ast.StatementVariableDeclaration))
			{
				var vardecl = (ast.StatementVariableDeclaration)n;
				foreach (var v in vardecl.Variables)
				{
					currentScope.Symbols.DefineSymbol(v.Name);
					currentScope.ProcessAccessibilitySpecs(v.Name);

					if (v.InitialValue!=null && v.InitialValue.RootNode.GetType()==typeof(ast.ExprNodeObjectLiteral))
					{
						// Get the object literal
						var literal=(ast.ExprNodeObjectLiteral)v.InitialValue.RootNode;

						// Create a fake/temp identifier node while we process accessibility specs
						var target = new ast.ExprNodeIdentifier(null, v.Name);

						// Process all keys that are identifiers
						foreach (var x in literal.Values)
						{
							var identifierKey=x.Key as ast.ExprNodeIdentifier;
							if (identifierKey!=null && identifierKey.Lhs==null)
							{
								currentScope.ProcessAccessibilitySpecs(target, identifierKey.Name);
							}
						}
					}
				}

				return true;
			}

			// Define parameters in the current scope
			if (n.GetType() == typeof(ast.Parameter))
			{
				var p = (ast.Parameter)n;
				currentScope.Symbols.DefineSymbol(p.Name);
				currentScope.ProcessAccessibilitySpecs(p.Name);
				return true;
			}

			// Automatic declaration of private member?
			// We're looking for an assignment to a matching private spec
			if (n.GetType() == typeof(ast.StatementExpression))
			{
				var exprstmt = (ast.StatementExpression)n;
				if (exprstmt.Expression.RootNode.GetType()==typeof(ast.ExprNodeAssignment))
				{
					var assignOp = (ast.ExprNodeAssignment)exprstmt.Expression.RootNode;
					if (assignOp.Op == Token.assign)
					{
						// Lhs must be an identifier member
						// eg: target.member=<expr>
						if (assignOp.Lhs.GetType()==typeof(ast.ExprNodeIdentifier))
						{
							var identifier=(ast.ExprNodeIdentifier)assignOp.Lhs;
							if (identifier.Lhs!=null)
							{
								// For member specs, the identifier must have a lhs
								if (identifier.Lhs.GetType() != typeof(ast.ExprNodeIdentifier))
									return false;

								currentScope.ProcessAccessibilitySpecs((ast.ExprNodeIdentifier)identifier.Lhs, identifier.Name);
							}
						}

						// Assignment of an object literal
						// eg: target={member:value,member:value};
						if (assignOp.Lhs.GetType() == typeof(ast.ExprNodeIdentifier) &&
							assignOp.Rhs.GetType() == typeof(ast.ExprNodeObjectLiteral))
						{
							var target = (ast.ExprNodeIdentifier)assignOp.Lhs;
							var literal=(ast.ExprNodeObjectLiteral)assignOp.Rhs;

							if (target.Lhs == null)
							{
								foreach (var x in literal.Values)
								{
									var identifierKey=x.Key as ast.ExprNodeIdentifier;
									if (identifierKey!=null && identifierKey.Lhs==null)
									{
										currentScope.ProcessAccessibilitySpecs(target, identifierKey.Name);
									}
								}
							
							}
						}
						/*
						if (assignOp.Rhs.GetType() == typeof(ast.ExprNodeObjectLiteral))
						{
							var literal=ast.ExprNode
						}
						 */
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

		SymbolScope currentScope;
	}
}
