using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME
{
	class VisitorLint : ast.IVisitor
	{
		// Constructor
		public VisitorLint(SymbolScope rootScope)
		{
			currentScope = rootScope;
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
			foreach (var i in currentScope.Symbols)
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


			return true;

		}

		public void OnLeaveNode(MiniME.ast.Node n)
		{
			if (n.Scope != null)
			{
				currentScope = n.Scope.OuterScope;
			}
		}

		public SymbolScope currentScope = null;
	}
}
