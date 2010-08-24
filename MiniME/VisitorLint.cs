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

		public bool OnEnterNode(MiniME.ast.Node n)
		{
			if (n.Scope != null)
			{
				currentScope = n.Scope;
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
