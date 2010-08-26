using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class VisitorLint : ast.IVisitor
	{
		// Constructor
		public VisitorLint(SymbolScope rootScope, SymbolScope rootPseudoScope)
		{
			currentScope = rootScope;
			currentPseudoScope = rootPseudoScope;
			DetectMultipleDeclarations();
		}

		public void CheckControlCondition(ast.Statement statement, ast.Expression expr)
		{
			if (expr == null)
				return;

			if ((expr.RootNode as ast.ExprNodeAssignment)!=null)
			{
				currentScope.Compiler.RecordWarning(expr.Bookmark, "assignment as condition of flow control statement (use parens to disable this warning)");
			}
		}

		public void DetectMultipleDeclarations()
		{
			foreach (var i in currentPseudoScope.Symbols)
			{
				var s = i.Value;
				if (s.Scope==Symbol.ScopeType.local && s.Declarations.Count > 1)
				{
					int index = 1;
					foreach (var d in s.Declarations)
					{
						currentScope.Compiler.RecordWarning(d, s.Declarations[0], "in {0} symbol '{1}' has multiple declarations, instance {2}", currentScope.Name, s.Name, index);
						if (d.warnings)
							index++;
					}
				}
			}
		}

		public bool OnEnterNode(MiniME.ast.Node n)
		{
			if (n.Scope != null)
			{
				currentScope = n.Scope;
			}

			if (n.PseudoScope!=null)
			{
				currentPseudoScope=n.PseudoScope;
				DetectMultipleDeclarations();
			}

			// Check 'if' statement
			var ifStatement = n as ast.StatementIfElse;
			if (ifStatement != null)
			{
				CheckControlCondition(ifStatement, ifStatement.Condition);
			}

			// Check 'while' statement
			var whileStatement = n as ast.StatementWhile;
			if (whileStatement != null)
			{
				CheckControlCondition(whileStatement, whileStatement.Condition);
			}

			// Check 'do' statement
			var doStatement = n as ast.StatementDoWhile;
			if (doStatement != null)
			{
				CheckControlCondition(doStatement, doStatement.Condition);
			}

			// Check 'for' statement
			var forStatement = n as ast.StatementFor;
			if (forStatement != null)
			{
				CheckControlCondition(forStatement, forStatement.Condition);
			}

			// Check for variable used outself declaring pseudo scope
			var ident = n as ast.ExprNodeIdentifier;
			if (ident!=null && ident.Lhs == null)
			{
				var symbol=currentScope.FindLocalSymbol(ident.Name);
				if (symbol!=null)
				{
					// Now walk the pseudo scopes and make sure that it's defined in the current scope too
					// (and not in a child scope)

					var scope = currentPseudoScope;
					bool bFound=false;
					while (scope != null && !bFound)
					{
						// Check scope
						if (scope.FindLocalSymbol(ident.Name) != null)
						{
							bFound = true;
							break;
						}

						// Are we finished on the actual local scope
						if (scope.Node!=null && scope.Node.Scope != null)
							break;

						// Get next outer scope
						scope = scope.OuterScope;
					}

					if (!bFound)
					{
						currentScope.Compiler.RecordWarning(n.Bookmark, "symbol `{0}` used outside declaring pseudo scope", ident.Name);
						foreach (var decl in symbol.Declarations)
						{
							currentScope.Compiler.RecordWarning(decl, n.Bookmark, "see also declaration of `{0}`", ident.Name);
						}
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
			if (n.PseudoScope != null)
			{
				currentPseudoScope = n.PseudoScope.OuterScope;
			}
		}

		public SymbolScope currentScope = null;
		public SymbolScope currentPseudoScope = null;
	}
}
