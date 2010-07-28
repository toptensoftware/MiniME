using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MiniME
{
	[Flags]
	enum ParseContext
	{
		DisableInOperator=0x0001,
		NoFunctionCalls=0x0002,
	}

	class Parser
	{
		public Parser(Tokenizer t)
		{
			this.t = t;
		}

		internal delegate ast.ExpressionNode fnExprNode(ParseContext ctx);

		ast.ExpressionNode ParseBinary(fnExprNode Next, ParseContext ctx, Func<Token, bool> TokenCheck)
		{
			var lhs = Next(ctx);

			while (true)
			{
				if (TokenCheck(t.token))
				{
					var Op = t.token;
					t.Next();

					lhs = new ast.ExprNodeBinary(lhs, Next(ctx), Op);
				}
				else
					return lhs;
			}
		}

		ast.ExpressionNode ParseExpressionTerminal(ParseContext ctx)
		{
			switch (t.token)
			{
				case Token.literal:
				{
					var temp = new ast.ExprNodeLiteral(t.literal);
					t.Next();
					return temp;
				}

				case Token.openRound:
				{
					t.Next();
					var temp = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);
					return temp;
				}

				case Token.identifier:
				{
					var temp = new ast.ExprNodeMember(t.identifier);
					t.Next();
					return temp;
				}

				case Token.openSquare:
				{
					t.Next();
					var temp = new ast.ExprNodeArrayLiteral();
					while (true)
					{
						if (t.token == Token.closeSquare)
							break;

						// Empty expression
						if (t.token == Token.comma)
						{
							t.Next();
							temp.Expressions.Add(null);
						}
						else
						{
							// Non-empty expression
							temp.Expressions.Add(ParseSingleExpression(0));

							// End of list?
							if (!t.SkipOptional(Token.comma))
								break;
						}

						// Trailing blank element?
						if (t.token == Token.closeSquare)
						{
							temp.Expressions.Add(null);
						}

					}
					t.SkipRequired(Token.closeSquare);
					return temp;
				}

				case Token.openBrace:
				{
					t.Next();

					var temp = new ast.ExprNodeObjectLiteral();
					while (true)
					{
						if (t.token == Token.closeBrace)
							break;

						object key;
						if (t.token == Token.identifier)
						{
							key = t.identifier;
						}
						else
						{
							t.Require(Token.literal);
							key = t.literal;
						}
						t.Next();

						t.SkipRequired(Token.colon);

						temp.Values.Add(new ast.KeyExpressionPair(key, ParseSingleExpression(0)));

						if (!t.SkipOptional(Token.comma))
							break;
					}

					t.SkipRequired(Token.closeBrace);
					return temp;
				}

				case Token.divide:
				case Token.divideAssign:
				{
					// Regular expressions
					return new ast.ExprNodeRegEx(t.ParseRegEx());
				}

				case Token.kw_function:
				{
					t.Next();
					return ParseFunction();
				}

				case Token.kw_new:
				{
					t.Next();

					// Parse the type
					var newType = ParseExpressionMember(ctx | ParseContext.NoFunctionCalls);

					// Create the new operator
					var newOp = new ast.ExprNodeNew(newType);

					// Parse parameters
					t.SkipRequired(Token.openRound);

					if (t.token != Token.closeRound)
					{
						while (true)
						{
							newOp.Arguments.Add(ParseSingleExpression(0));
							if (t.SkipOptional(Token.comma))
								continue;
							else
								break;
						}
					}

					t.SkipRequired(Token.closeRound);

					return newOp;
				}

				case Token.kw_delete:
				{
					t.Next();
					return new ast.ExprNodeUnary(ParseExpressionMember(ctx), Token.kw_delete);
				}
			}

			throw new CompileError(string.Format("Invalid expression, didn't expect {0}", t.DescribeCurrentToken()), t);
		}

		ast.ExpressionNode ParseExpressionMember(ParseContext ctx)
		{
			var lhs = ParseExpressionTerminal(ctx);

			while (true)
			{
				// Member dot '.'
				if (t.SkipOptional(Token.memberDot))
				{
					t.Require(Token.identifier);
					lhs = new ast.ExprNodeMember(t.identifier, lhs);
					t.Next();
					continue;
				}

				// Array indexer '[]'
				if (t.SkipOptional(Token.openSquare))
				{
					var temp = new ast.ExprNodeIndexer(lhs, ParseCompositeExpression(0));
					t.SkipRequired(Token.closeSquare);
					lhs = temp;
					continue;
				}

				// Function call '()'
				if ((ctx & ParseContext.NoFunctionCalls)==0 && t.SkipOptional(Token.openRound))
				{
					var temp = new ast.ExprNodeCall(lhs);

					if (t.token != Token.closeRound)
					{
						while (true)
						{
							temp.Arguments.Add(ParseSingleExpression(0));
							if (t.SkipOptional(Token.comma))
								continue;
							else
								break;
						}
					}

					t.SkipRequired(Token.closeRound);
					lhs = temp;
					continue;
				}

				break;
			}

			return lhs;
		}

		ast.ExpressionNode ParseExpressionNegation(ParseContext ctx)
		{
			if (t.token == Token.increment || t.token == Token.decrement)
			{
				var temp = t.token;
				t.Next();
				return new ast.ExprNodeUnary(ParseExpressionMember(ctx), temp);
			}

			while (true)
			{
				if (t.token == Token.add)
				{
					// Ignore as it's redundant!
					t.Next();
				}
				else if (t.token == Token.bitwiseNot || 
					t.token == Token.logicalNot || 
					t.token == Token.add || 
					t.token == Token.subtract ||
					t.token == Token.kw_typeof ||
					t.token == Token.kw_void ||
					t.token == Token.kw_delete
					)
				{
					var temp = t.token;
					t.Next();
					return new ast.ExprNodeUnary(ParseExpressionNegation(ctx), temp);
				}
				else
					break;
			}


			var lhs=ParseExpressionMember(ctx);

			if (t.token == Token.increment || t.token == Token.decrement)
			{
				var temp = t.token;
				t.Next();
				return new ast.ExprNodePostfix(lhs, temp);
			}

			return lhs;
		}

		ast.ExpressionNode ParseExpressionMultiply(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionNegation, ctx, 
				x=>x == Token.multiply || x == Token.divide || x == Token.modulus);
		}

		ast.ExpressionNode ParseExpressionAdd(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionMultiply, ctx,
				x => x == Token.add || x == Token.subtract);
		}

		ast.ExpressionNode ParseExpressionShift(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionAdd, ctx,
				x => x == Token.shl || x == Token.shr || x==Token.shrz);
		}

		ast.ExpressionNode ParseExpressionCompareRelation(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionShift,  ctx,
						x => 
							x == Token.compareLT || 
							x == Token.compareLE || 
							x == Token.compareGT ||
							x == Token.compareGE ||
							x == Token.kw_instanceof ||
							(x == Token.kw_in && (ctx & ParseContext.DisableInOperator)==0)
							);
		}


		ast.ExpressionNode ParseExpressionCompareEquality(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionCompareRelation, ctx,
						x =>
							x == Token.compareEQ ||
							x == Token.compareNE ||
							x == Token.compareEQStrict ||
							x == Token.compareNEStrict
							);
		}

		ast.ExpressionNode ParseExpressionBitwiseAnd(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionCompareEquality, ctx,
				x => x == Token.bitwiseAnd);
		}

		ast.ExpressionNode ParseExpressionBitwiseXor(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionBitwiseAnd, ctx,
				x => x == Token.bitwiseXor);
		}

		ast.ExpressionNode ParseExpressionBitwiseOr(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionBitwiseXor, ctx,
				x => x == Token.bitwiseOr);
		}

		ast.ExpressionNode ParseExpressionLogicalAnd(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionBitwiseOr, ctx,
				x => x == Token.logicalAnd);
		}

		ast.ExpressionNode ParseExpressionLogicalOr(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionLogicalAnd, ctx,
				x => x == Token.logicalOr);
		}

		ast.ExpressionNode ParseExpressionTernary(ParseContext ctx)
		{
			var lhs=ParseExpressionLogicalOr(ctx);

			if (t.SkipOptional(Token.ternary))
			{
				var result=new ast.ExprNodeConditional(lhs);

				result.TrueResult=ParseExpressionTernary(ctx);

				t.SkipRequired(Token.colon);

				result.FalseResult=ParseExpressionTernary(ctx);

				return result;
			}

			return lhs;

		}

		ast.ExpressionNode ParseExpressionAssignment(ParseContext ctx)
		{
			return ParseBinary(ParseExpressionTernary, ctx,
						x =>
							x == Token.assign ||
							x == Token.addAssign ||
							x == Token.subtractAssign||
							x == Token.multiplyAssign ||
							x == Token.divideAssign ||
							x == Token.modulusAssign ||
							x == Token.shlAssign ||
							x == Token.shrAssign ||
							x == Token.shrzAssign ||
							x == Token.bitwiseAndAssign ||
							x == Token.bitwiseOrAssign ||
							x == Token.bitwiseXorAssign
							);
		}


		ast.ExpressionNode ParseCompositeExpression(ParseContext ctx)
		{
			var lhs = ParseExpressionAssignment(ctx);
			if (t.token != Token.comma)
				return lhs;

			var expr = new ast.ExprNodeComposite();
			expr.Expressions.Add(lhs);

			while (t.SkipOptional(Token.comma))
			{
				expr.Expressions.Add(ParseExpressionAssignment(ctx));
			}

			return expr;
		}

		ast.ExpressionNode ParseSingleExpression(ParseContext ctx)
		{
			return ParseExpressionAssignment(ctx);
		}
 
		// Parse a single variable declaration
		void ParseVarDecl(ParseContext ctx, ast.StatementVariableDeclaration decl)
		{
			// Variable name
			t.Require(Token.identifier);
			var name = t.identifier;
			t.Next();

			// Initial value?
			ast.ExpressionNode InitialValue = null;
			if (t.SkipOptional(Token.assign))
			{
				InitialValue = ParseSingleExpression(ctx);
			}

			decl.AddDeclaration(name, InitialValue);
		}

		// Parse a variable declaration statement (which might include
		// several variable definitions separated by commas.
		// eg: var x=23, y, z=44;
		ast.Statement ParseVarDeclStatement(ParseContext ctx)
		{
			// Variable declaration?
			if (t.token == Token.kw_var)
			{
				t.Next();

				// Parse the first varaible declaration
				var stmt = new ast.StatementVariableDeclaration();
				
				ParseVarDecl(ctx, stmt);

				// Parse other declarations
				while (t.SkipOptional(Token.comma))
				{
					ParseVarDecl(ctx, stmt);
				}

				// End of statement
				return stmt;
			}
			else
			{
				return new ast.StatementExpression(ParseCompositeExpression(ctx));
			}
		}

		ast.Statement ParseStatement()
		{
			// Special handling for labels
			if (t.token == Token.identifier)
			{
				int mark = t.Mark();
				string label = t.identifier;
				t.Next();
				if (t.SkipOptional(Token.colon))
				{
					return new ast.StatementLabel(label);
				}

				t.Rewind(mark);
			}

			switch (t.token)
			{
				case Token.openBrace:
					return ParseStatementBlock(true);

				case Token.semicolon:
					// Empty statement
					return new ast.StatementBlock();

				case Token.kw_return:
				{
					t.Next();

					if (t.SkipOptional(Token.semicolon))
					{
						return new ast.StatementReturnThrow(Token.kw_return);
					}
					else
					{
						var temp=new ast.StatementReturnThrow(Token.kw_return, ParseCompositeExpression(0));
						t.SkipRequired(Token.semicolon);
						return temp;
					}
				}

				case Token.kw_throw:
				{
					t.Next();
					var temp = new ast.StatementReturnThrow(Token.kw_throw, ParseCompositeExpression(0));
					t.SkipRequired(Token.semicolon);
					return temp;
				}

				case Token.kw_break:
				case Token.kw_continue:
				{
					// Save and skip op
					Token op = t.token;
					t.Next();

					if (!t.SkipOptional(Token.semicolon))
					{
						t.Require(Token.identifier);

						var temp = new ast.StatementBreakContinue(op, t.identifier);
						t.Next();

						t.Require(Token.semicolon);

						return temp;
					}
					else
					{
						return new ast.StatementBreakContinue(op, null);
					}
				}

				case Token.kw_if:
				{
					t.Next();

					// Condition
					t.SkipRequired(Token.openRound);
					var stmt=new ast.StatementIfElse(ParseCompositeExpression(0));
					t.SkipRequired(Token.closeRound);

					// True code block
					stmt.TrueStatement=ParseStatement();

					// Else?
					if (t.SkipOptional(Token.kw_else))
					{
						stmt.FalseStatement=ParseStatement();
					}

					return stmt;
				}

				case Token.kw_switch:
				{
					t.Next();

					// Value
					t.SkipRequired(Token.openRound);
					var stmt = new ast.StatementSwitch(ParseCompositeExpression(0));
					t.SkipRequired(Token.closeRound);

					// Cases
					t.SkipRequired(Token.openBrace);

					ast.StatementSwitch.Case currentCase = null;
					while (t.token!=Token.closeBrace)
					{
						// new case?
						if (t.SkipOptional(Token.kw_case))
						{
							currentCase = stmt.AddCase(ParseCompositeExpression(0));
							t.SkipRequired(Token.colon);
							continue;
						}

						// default case?
						if (t.SkipOptional(Token.kw_default))
						{
							currentCase = stmt.AddCase(null);
							t.SkipRequired(Token.colon);
							continue;
						}

						// Must have a case
						if (currentCase == null)
						{
							throw new CompileError("Unexpected code in switch statement before 'case' or 'default'", t);
						}

						currentCase.AddCode(ParseStatement());
					}

					t.SkipRequired(Token.closeBrace);


					return stmt;
				}

				case Token.kw_for:
				{
					t.Next();

					t.SkipRequired(Token.openRound);

					ast.Statement init = null;

					if (t.token != Token.semicolon)
					{
						// Initializers
						init = ParseVarDeclStatement(ParseContext.DisableInOperator);

						// Foreach iterator
						if (t.SkipOptional(Token.kw_in))
						{
							var stmtForEach = new ast.StatementForIn();

							var decl = init as ast.StatementVariableDeclaration;

							if (decl != null)
							{
								if (decl.HasInitialValue())
								{
									throw new CompileError("Syntax error - unexpected initializer in for-in statement", t);
								}
								if (decl.Variables.Count > 1)
								{
									throw new CompileError("Syntax error - unexpected multiple iterator variables in for-in statement", t);
								}
								stmtForEach.VariableDeclaration = init;
							}
							else
							{
								var exprstmtForEach = init as ast.StatementExpression;
								if (exprstmtForEach == null)
								{
									throw new CompileError("Syntax error - invalid iterator variable declarations in for loop", t);
								}
								if ((exprstmtForEach.Expression as ast.ExprNodeMember) == null &&
									(exprstmtForEach.Expression as ast.ExprNodeIndexer) == null)
								{
									throw new CompileError("Syntax error - invalid iterator variable declarations in for loop", t);
								}
								stmtForEach.Iterator = exprstmtForEach.Expression;
							}


							// Collection
							stmtForEach.Collection = ParseCompositeExpression(0);
							t.SkipRequired(Token.closeRound);

							// Parse content
							stmtForEach.CodeBlock = ParseStatement();
							return stmtForEach;
						}
					}

					var stmt = new ast.StatementFor();
					stmt.Initialize = init;

					t.SkipRequired(Token.semicolon);

					// Condition
					if (t.token!=Token.semicolon)
						stmt.Condition = ParseCompositeExpression(0);
					t.SkipRequired(Token.semicolon);

					// Iterators
					if (t.token!=Token.closeRound)
						stmt.Increment = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);

					// Parse content
					stmt.CodeBlock = ParseStatement();

					return stmt;
				}

				case Token.kw_do:
				{
					var stmt = new ast.StatementDoWhile();
					t.Next();
					stmt.Code = ParseStatement();
					t.SkipRequired(Token.kw_while);
					t.SkipRequired(Token.openRound);
					stmt.Condition = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);
					t.SkipRequired(Token.semicolon);
					return stmt;
				}

				case Token.kw_while:
				{
					var stmt = new ast.StatementWhile();
					t.Next();
					t.SkipRequired(Token.openRound);
					stmt.Condition = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);
					stmt.Code = ParseStatement();
					return stmt;
				}

				case Token.kw_with:
				{
					var stmt = new ast.StatementWith();
					t.Next();
					t.SkipRequired(Token.openRound);
					stmt.Expression = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);
					stmt.Code = ParseStatement();
					return stmt;
				}

				case Token.kw_try:
				{
					t.Next();

					// Create the statement
					var stmt = new ast.StatementTryCatchFinally();

					// The code
					t.Require(Token.openBrace);
					stmt.Code = ParseStatementBlock(false);

					while (t.SkipOptional(Token.kw_catch))
					{
						var cc=new ast.CatchClause();

						// Catch expression
						t.SkipRequired(Token.openRound);

						// Exception variable name
						t.Require(Token.identifier);
						cc.ExceptionVariable=t.identifier;
						t.Next();

						// Optional 'if <condition>'
						if (t.token==Token.kw_if)
						{
							t.Next();
							cc.Condition=ParseCompositeExpression(0);
						}

						// End of expression
						t.SkipRequired(Token.closeRound);

						// Code block
						t.Require(Token.openBrace);
						cc.Code=ParseStatementBlock(false);

						stmt.CatchClauses.Add(cc);
					}

					if (t.SkipOptional(Token.kw_finally))
					{
						t.Require(Token.openBrace);
						stmt.FinallyClause=ParseStatementBlock(false);
					}

					return stmt;

				}

				case Token.kw_function:
				{
					t.Next();
					t.Require(Token.identifier);
					var stmt = new ast.StatementExpression(ParseFunction());
					t.SkipOptional(Token.semicolon);
					return stmt;
				}

				default:
				{
					var stmt = ParseVarDeclStatement(0);
					t.SkipRequired(Token.semicolon);
					return stmt;
				}
			}
		}

		public void ParseStatements(ast.StatementBlock block)
		{
			while (t.token != Token.closeBrace && t.token!=Token.eof)
			{
				if (t.token == Token.semicolon)
				{
					t.Next();
					continue;
				}

				block.Content.Add(ParseStatement());
			}
		}

		ast.Statement ParseStatementBlock(bool bCanReduce)
		{
			t.SkipRequired(Token.openBrace);

			var block = new ast.StatementBlock();
			ParseStatements(block);

			t.SkipRequired(Token.closeBrace);

			if (!bCanReduce)
				return block;

			block.RemoveRedundant();

			if (block.Content.Count == 1)
			{
				return block.Content[0];
			}
			else
			{
				return block;
			}
		}

		void ParseParameters(ast.ExprNodeFunction fn)
		{
			// Must have open paren
			t.SkipRequired(Token.openRound);

			// Empty?
			if (t.SkipOptional(Token.closeRound))
				return;

			while (true)
			{
				// Name
				t.Require(Token.identifier);
				fn.Parameters.Add(new ast.Parameter(t.identifier));
				t.Next();

				// Another?
				if (!t.SkipOptional(Token.comma))
					break;
			}

			// Finished
			t.SkipRequired(Token.closeRound);
		}


		ast.ExprNodeFunction ParseFunction()
		{
			// Create the function
			var fn = new ast.ExprNodeFunction();

			if (t.token == Token.identifier)
			{
				fn.Name = t.identifier;
				t.Next();
			}

			// Parameters
			ParseParameters(fn);

			// Function body
			fn.Body = ParseStatementBlock(false);

			return fn;
		}

		Tokenizer t;

	}
}
