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

	// Implementation of the Javascript parser.
	// Reads input from a Tokenizer and builds a complete abtract
	// syntax tree for all contained statements, functions, variables, 
	// expressions etc...
	class Parser
	{
		// Constructor
		public Parser(Tokenizer t)
		{
			this.t = t;
		}

		// Attributes
		Tokenizer t;

	
		// Parse binary is a helper function to parse binary operations.  Uses a function 
		// callbacks (TokenCheck) to check if a token is applicable to the current operator 
		// precedence and a delegate (fnExprNode) to call the next higher precedence parser.
		internal delegate ast.ExpressionNode fnExprNode(ParseContext ctx);
		ast.ExpressionNode ParseBinary(fnExprNode Next, ParseContext ctx, Func<Token, bool> TokenCheck)
		{
			// Parse the LHS
			var lhs = Next(ctx);

			// Parse all consecutive RHS
			while (true)
			{
				// Check operator at same precedence level
				if (TokenCheck(t.token))
				{
					// Save the operator token
					var bmk = t.GetBookmark();
					t.Next();

					// Parse the RHS and join to the LHS with appropriate operator.
					lhs = new ast.ExprNodeBinary(bmk, lhs, Next(ctx), bmk.token);
				}
				else
					return lhs;
			}
		}

		// Parse an expression terminal
		ast.ExpressionNode ParseExpressionTerminal(ParseContext ctx)
		{
			switch (t.token)
			{
				case Token.literal:
				{
					var temp = new ast.ExprNodeLiteral(t.GetBookmark(), t.literal);
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
					var temp = new ast.ExprNodeIdentifier(t.GetBookmark(), t.identifier);
					t.Next();
					return temp;
				}

				case Token.openSquare:
				{
					// Array literal
					t.Next();
					var temp = new ast.ExprNodeArrayLiteral(t.GetBookmark());
					while (true)
					{
						if (t.token == Token.closeSquare)
							break;

						// Empty expression
						if (t.token == Token.comma)
						{
							t.Next();
							temp.Values.Add(null);
						}
						else
						{
							// Non-empty expression
							temp.Values.Add(ParseSingleExpression(0));

							// End of list?
							if (!t.SkipOptional(Token.comma))
								break;
						}

						// Trailing blank element?
						if (t.token == Token.closeSquare)
						{
							temp.Values.Add(null);
						}

					}
					t.SkipRequired(Token.closeSquare);
					return temp;
				}

				case Token.openBrace:
				{
					// Create the literal
					var temp = new ast.ExprNodeObjectLiteral(t.GetBookmark());

					// Object literal
					t.Next();

					while (true)
					{
						if (t.token == Token.closeBrace)
							break;

						// Key 
						//  - can be an identifier, or string/number literal
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

						// Key/value delimiter
						t.SkipRequired(Token.colon);

						// Value
						temp.Values.Add(new ast.KeyExpressionPair(key, ParseSingleExpression(0)));

						// Another key/value pair
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
					return new ast.ExprNodeRegEx(t.GetBookmark(), t.ParseRegEx());
				}

				case Token.kw_function:
				{
					t.Next();
					return ParseFunction();
				}

				case Token.kw_new:
				{
					var bmk = t.GetBookmark();

					t.Next();

					// Parse the type
					var newType = ParseExpressionMember(ctx | ParseContext.NoFunctionCalls);

					// Create the new operator
					var newOp = new ast.ExprNodeNew(bmk, newType);

					// Parse parameters
					if (t.SkipOptional(Token.openRound))
					{
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
					}

					return newOp;
				}

				case Token.kw_delete:
				{
					var bmk = t.GetBookmark();
					t.Next();
					return new ast.ExprNodeUnary(bmk, ParseExpressionMember(ctx), Token.kw_delete);
				}
			}

			throw new CompileError(string.Format("Invalid expression, didn't expect {0}", t.DescribeCurrentToken()), t);
		}

		// Parse member dots, array indexers, function calls
		ast.ExpressionNode ParseExpressionMember(ParseContext ctx)
		{
			var lhs = ParseExpressionTerminal(ctx);

			while (true)
			{
				// Member dot '.'
				if (t.SkipOptional(Token.period))
				{
					t.Require(Token.identifier);
					lhs = new ast.ExprNodeIdentifier(t.GetBookmark(), t.identifier, lhs);
					t.Next();
					continue;
				}

				// Array indexer '[]'
				if (t.SkipOptional(Token.openSquare))
				{
					var temp = new ast.ExprNodeIndexer(t.GetBookmark(), lhs, ParseCompositeExpression(0));
					t.SkipRequired(Token.closeSquare);
					lhs = temp;
					continue;
				}

				// Function call '()'
				if ((ctx & ParseContext.NoFunctionCalls)==0 && t.SkipOptional(Token.openRound))
				{
					var temp = new ast.ExprNodeCall(t.GetBookmark(), lhs);

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

		// Unary operators such as negation, not, increment/decrement
		ast.ExpressionNode ParseExpressionNegation(ParseContext ctx)
		{
			// Prefix increment
			if (t.token == Token.increment || t.token == Token.decrement)
			{
				var bmk = t.GetBookmark();
				t.Next();
				return new ast.ExprNodeUnary(bmk, ParseExpressionMember(ctx), bmk.token);
			}

			// Unary ops
			while (true)
			{
				if (t.token == Token.add)
				{
					// Ignore as it's redundant
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
					var bmk = t.GetBookmark();
					t.Next();
					return new ast.ExprNodeUnary(bmk, ParseExpressionNegation(ctx), bmk.token);
				}
				else
					break;
			}


			// The operand
			var lhs=ParseExpressionMember(ctx);

			// Postfix increment
			if (t.token == Token.increment || t.token == Token.decrement)
			{
				var bmk = t.GetBookmark();
				t.Next();
				return new ast.ExprNodePostfix(bmk, lhs, bmk.token);
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

			// Is it a ternary operator eg: condition ? true : false
			if (t.SkipOptional(Token.question))
			{
				var result=new ast.ExprNodeConditional(t.GetBookmark(), lhs);

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

		// Parse an expression that might include comma separated multiple
		// expressions.
		ast.ExpressionNode ParseCompositeExpression(ParseContext ctx)
		{
			var lhs = ParseExpressionAssignment(ctx);
			if (t.token != Token.comma)
				return lhs;

			var expr = new ast.ExprNodeComposite(t.GetBookmark());
			expr.Expressions.Add(lhs);

			while (t.SkipOptional(Token.comma))
			{
				expr.Expressions.Add(ParseExpressionAssignment(ctx));
			}

			return expr;
		}

		// Parse a single expression (ie: doesn't support comma operator)
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

			// Optional initial value
			ast.ExpressionNode InitialValue = null;
			if (t.SkipOptional(Token.assign))
			{
				InitialValue = ParseSingleExpression(ctx);
			}

			// Store it
			decl.AddDeclaration(name, InitialValue);
		}

		// Parse a variable declaration statement 
		//  - might include several variable definitions separated by commas.
		//     eg: var x=23, y, z=44;
		ast.Statement ParseVarDeclStatement(ParseContext ctx)
		{
			// Is it a variable declaration?
			if (t.token == Token.kw_var)
			{
				var bmk = t.GetBookmark();

				t.Next();

				// Parse the first varaible declaration
				var stmt = new ast.StatementVariableDeclaration(bmk);
				
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
				// Must be just a normal expression statement
				return new ast.StatementExpression(t.GetBookmark(), ParseCompositeExpression(ctx));
			}
		}

		// Parse a brace enclosed statement block
		ast.CodeBlock ParseStatementBlock(TriState BracesInOutput)
		{
			var bmk = t.GetBookmark();

			// Opening brace
			t.SkipRequired(Token.openBrace);

			// Statements
			var code = new ast.CodeBlock(bmk, BracesInOutput);
			ParseStatements(code);

			// Closing brace
			t.SkipRequired(Token.closeBrace);

			return code;
		}


		// Parse a single statement
		ast.CodeBlock ParseStatement()
		{
			if (t.token == Token.openBrace)
			{
				return ParseStatementBlock(TriState.Maybe);
			}
			else
			{
				var stmt = new ast.CodeBlock(t.GetBookmark(), TriState.Maybe);
				stmt.AddStatement(ParseSingleStatement());
				return stmt;
			}
		}

		// Parse a single statement
		ast.Statement ParseSingleStatement()
		{
			var bmk = t.GetBookmark();

			// Special handling for labels
			if (t.token == Token.identifier)
			{
				string label = t.identifier;
				t.Next();
				if (t.SkipOptional(Token.colon))
				{
					return new ast.StatementLabel(bmk, label);
				}

				t.Rewind(bmk);
			}

			switch (t.token)
			{
				case Token.semicolon:
				{
					t.Next();
					return null;
				}

				case Token.openBrace:
				{
					t.Next();
					var stmt = new ast.StatementBlock(t.GetBookmark());
					while (!t.SkipOptional(Token.closeBrace))
					{
						stmt.AddStatement(ParseSingleStatement());
					}
					return stmt;
				}

				case Token.kw_return:
				{
					t.Next();

					if (t.SkipOptional(Token.semicolon))
					{
						return new ast.StatementReturnThrow(bmk, Token.kw_return);
					}
					else
					{
						var temp=new ast.StatementReturnThrow(bmk, Token.kw_return, ParseCompositeExpression(0));
						t.SkipRequired(Token.semicolon);
						return temp;
					}
				}

				case Token.kw_throw:
				{
					t.Next();
					var temp = new ast.StatementReturnThrow(bmk, Token.kw_throw, ParseCompositeExpression(0));
					t.SkipRequired(Token.semicolon);
					return temp;
				}

				case Token.kw_break:
				case Token.kw_continue:
				{
					// Statement
					Token op = t.token;
					t.Next();

					// Optional label
					if (!t.SkipOptional(Token.semicolon))
					{
						t.Require(Token.identifier);

						var temp = new ast.StatementBreakContinue(bmk, op, t.identifier);
						t.Next();

						t.Require(Token.semicolon);

						return temp;
					}

					// No label
					return new ast.StatementBreakContinue(bmk, op, null);
				}

				case Token.kw_if:
				{
					t.Next();

					// Condition
					t.SkipRequired(Token.openRound);
					var stmt=new ast.StatementIfElse(bmk, ParseCompositeExpression(0));
					t.SkipRequired(Token.closeRound);

					// True code block
					stmt.TrueStatement=ParseStatement();

					// Optional else block
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
					var stmt = new ast.StatementSwitch(bmk, ParseCompositeExpression(0));
					t.SkipRequired(Token.closeRound);

					// Opening brace
					t.SkipRequired(Token.openBrace);

					// Parse all cases
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

						currentCase.AddCode(ParseSingleStatement());
					}

					// Done
					t.SkipRequired(Token.closeBrace);
					return stmt;
				}

				case Token.kw_for:
				{
					// Statement
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
							var stmtForEach = new ast.StatementForIn(bmk);

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
								if ((exprstmtForEach.Expression as ast.ExprNodeIdentifier) == null &&
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
							stmtForEach.Code = ParseStatement();
							return stmtForEach;
						}
					}

					// Create the statement, store the initialization expression(s)
					var stmt = new ast.StatementFor(bmk);
					stmt.Initialize = init;
					t.SkipRequired(Token.semicolon);

					// Condition
					if (t.token!=Token.semicolon)
						stmt.Condition = ParseCompositeExpression(0);
					t.SkipRequired(Token.semicolon);

					// Iterator
					if (t.token!=Token.closeRound)
						stmt.Increment = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);

					// Parse code block
					stmt.Code = ParseStatement();
					return stmt;
				}

				case Token.kw_do:
				{
					var stmt = new ast.StatementDoWhile(bmk);
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
					var stmt = new ast.StatementWhile(bmk);
					t.Next();
					t.SkipRequired(Token.openRound);
					stmt.Condition = ParseCompositeExpression(0);
					t.SkipRequired(Token.closeRound);
					stmt.Code = ParseStatement();
					return stmt;
				}

				case Token.kw_with:
				{
					var stmt = new ast.StatementWith(bmk);
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
					var stmt = new ast.StatementTryCatchFinally(bmk);

					// The code
					t.Require(Token.openBrace);
					stmt.Code = ParseStatementBlock(TriState.Yes);

					// Catch clauses
					bmk = t.GetBookmark();
					while (t.SkipOptional(Token.kw_catch))
					{
						var cc=new ast.CatchClause(bmk);

						// Catch expression
						t.SkipRequired(Token.openRound);

						// Exception variable name
						t.Require(Token.identifier);
						cc.ExceptionVariable=t.identifier;
						t.Next();

						// Optional 'if <condition>'   (firefox extension)
						if (t.token==Token.kw_if)
						{
							t.Next();
							cc.Condition=ParseCompositeExpression(0);
						}

						// End of expression
						t.SkipRequired(Token.closeRound);

						// Code block
						t.Require(Token.openBrace);
						cc.Code=ParseStatementBlock(TriState.Yes);

						stmt.CatchClauses.Add(cc);

						bmk = t.GetBookmark();
					}

					// Finally
					if (t.SkipOptional(Token.kw_finally))
					{
						t.Require(Token.openBrace);
						stmt.FinallyClause = ParseStatementBlock(TriState.Yes);
					}

					return stmt;

				}

				case Token.kw_function:
				{
					// Function declaration
					t.Next();
					t.Require(Token.identifier);
					var stmt = new ast.StatementExpression(bmk, ParseFunction());
					t.SkipOptional(Token.semicolon);
					return stmt;
				}

				case Token.directive_comment:
				{
					var stmt = new ast.StatementComment(bmk, t.RawToken.Substring(0,2) + t.RawToken.Substring(3));
					t.Next();
					return stmt;
				}

				case Token.directive_private:
				case Token.directive_public:
				{
					var stmt = new ast.StatementAccessibility(bmk);
					foreach (var symbol in t.identifier.Split(','))
					{
						var spec = new ast.AccessibilitySpec();
						if (!spec.Parse(t.token==Token.directive_private ? Accessibility.Private : Accessibility.Public, 
											symbol))
						{
							throw new CompileError(string.Format("Invalid private member declaration - `{0}`", symbol), t);
						}
						stmt.Specs.Add(spec);
					}

					t.Next();
					return stmt;
				}

				default:
				{
					// Must be a variable declaration or an expression
					var stmt = ParseVarDeclStatement(0);
					t.SkipRequired(Token.semicolon);
					return stmt;
				}
			}
		}

		// Parse series of statements into a statement block
		public void ParseStatements(ast.CodeBlock block)
		{
			while (t.token != Token.closeBrace && t.token!=Token.eof)
			{
				// Skip redundant semicolons
				if (t.token == Token.semicolon)
				{
					t.Next();
					continue;
				}

				// Add the next statement
				block.AddStatement(ParseSingleStatement());
			}
		}

		// Parse the parameter declarations on a function
		void ParseParameters(ast.ExprNodeFunction fn)
		{
			// Must have open paren
			t.SkipRequired(Token.openRound);

			// Empty?
			if (t.SkipOptional(Token.closeRound))
				return;

			// Parameters
			while (true)
			{
				// Name
				t.Require(Token.identifier);
				fn.Parameters.Add(new ast.Parameter(t.GetBookmark(), t.identifier));
				t.Next();

				// Another?
				if (!t.SkipOptional(Token.comma))
					break;
			}

			// Finished
			t.SkipRequired(Token.closeRound);
		}


		// Parse a function declaration
		ast.ExprNodeFunction ParseFunction()
		{
			// Create the function
			var fn = new ast.ExprNodeFunction(t.GetBookmark());

			// Functions can be anonymous
			if (t.token == Token.identifier)
			{
				fn.Name = t.identifier;
				t.Next();
			}

			// Parameters
			ParseParameters(fn);

			// Body
			fn.Code = ParseStatementBlock(TriState.Yes);

			return fn;
		}

	}
}
