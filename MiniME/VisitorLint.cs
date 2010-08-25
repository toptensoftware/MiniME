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

			if (!statement.Bookmark.warnings)
				return;

			if ((expr.RootNode as ast.ExprNodeAssignment)!=null)
			{
				Console.WriteLine("{0}: warning: assignment as condition of control statement (use parens to disable this warning)", expr.Bookmark);
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
						Console.WriteLine("{0}: warning: in {1} symbol '{2}' has multiple declarations, instance {3}.", d, currentScope.Name, s.Name, index++);
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

			// Check for new Object and new Array
			var newStatement = n as ast.ExprNodeNew;
			if (newStatement!=null && newStatement.Arguments.Count==0 && newStatement.Bookmark.warnings)
			{
				var id = newStatement.ObjectType as ast.ExprNodeIdentifier;
				if (id != null && id.Lhs==null)
				{
					if (id.Name == "Object")
					{
						Console.WriteLine("{0}: warning: use of `new Object()`. Suggest using `{{}}` instead", newStatement.Bookmark);
					}
					if (id.Name == "Array")
					{
						Console.WriteLine("{0}: warning: use of `new Array()`. Suggest using `[]` instead", newStatement.Bookmark);
					}
				}
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
						if (scope.Node.Scope != null)
							break;

						// Get next outer scope
						scope = scope.OuterScope;
					}

					if (!bFound)
					{
						Console.WriteLine("{0}: warning: variable `{1}` used outside declaring pseudo scope", n.Bookmark, ident.Name);
						foreach (var decl in symbol.Declarations)
						{
							Console.WriteLine("{0}: see also declaration of `{1}`", decl, ident.Name);
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
