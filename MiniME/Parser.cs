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
		AllowCompositeExpressions=0x0004,
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



		// ParseLtr is a helper function to parse ltr binary operations.  Uses a function 
		// callbacks (TokenCheck) to check if a token is applicable to the current operator 
		// precedence and a delegate (fnExprNode) to call the next higher precedence parser.
		internal delegate ast.ExprNode fnExprNode(ParseContext ctx);
		ast.ExprNode ParseLtr(fnExprNode Next, ParseContext ctx, Func<Token, bool> TokenCheck)
		{
			// Parse the LHS
			var lhs = Next(ctx);

			ast.ExprNodeLtr ltr = null;

			// Parse all consecutive RHS
			while (true)
			{
				// Check operator at same precedence level
				if (TokenCheck(t.token))
				{
					if (ltr == null)
					{
						ltr = new ast.ExprNodeLtr(lhs.Bookmark, lhs);
					}

					// Save the operator token
					var bmk = t.GetBookmark();
					t.Next();

					// Parse the rhs
					ltr.AddTerm(bmk.token, Next(ctx));
				}
				else
					return ltr == null ? lhs : ltr;
			}
		}

		// Parse an expression terminal
		ast.ExprNode ParseExpressionTerminal(ParseContext ctx)
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
					var bmk = t.GetBookmark();
					t.Next();
					var temp = ParseCompositeExpressionNode(0);
					t.SkipRequired(Token.closeRound);
					return new ast.ExprNodeParens(bmk, temp);
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
							temp.Values.Add(ParseSingleExpressionNode(0));

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
							key = new ast.ExprNodeIdentifier(t.GetBookmark(), t.identifier);
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
						temp.Values.Add(new ast.KeyExpressionPair(key, ParseSingleExpressionNode(0)));

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
								newOp.Arguments.Add(ParseSingleExpressionNode(0));
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
		ast.ExprNode ParseExpressionMember(ParseContext ctx)
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
					var temp = new ast.ExprNodeIndexer(t.GetBookmark(), lhs, ParseCompositeExpressionNode(0));
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
							temp.Arguments.Add(ParseSingleExpressionNode(0));
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
		ast.ExprNode ParseExpressionNegation(ParseContext ctx)
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

		ast.ExprNode ParseExpressionMultiply(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionNegation, ctx, 
				x=>x == Token.multiply || x == Token.divide || x == Token.modulus);
		}

		ast.ExprNode ParseExpressionAdd(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionMultiply, ctx,
				x => x == Token.add || x == Token.subtract);
		}

		ast.ExprNode ParseExpressionShift(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionAdd, ctx,
				x => x == Token.shl || x == Token.shr || x==Token.shrz);
		}

		ast.ExprNode ParseExpressionCompareRelation(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionShift,  ctx,
						x => 
							x == Token.compareLT || 
							x == Token.compareLE || 
							x == Token.compareGT ||
							x == Token.compareGE ||
							x == Token.kw_instanceof ||
							(x == Token.kw_in && (ctx & ParseContext.DisableInOperator)==0)
							);
		}


		ast.ExprNode ParseExpressionCompareEquality(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionCompareRelation, ctx,
						x =>
							x == Token.compareEQ ||
							x == Token.compareNE ||
							x == Token.compareEQStrict ||
							x == Token.compareNEStrict
							);
		}

		ast.ExprNode ParseExpressionBitwiseAnd(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionCompareEquality, ctx,
				x => x == Token.bitwiseAnd);
		}

		ast.ExprNode ParseExpressionBitwiseXor(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionBitwiseAnd, ctx,
				x => x == Token.bitwiseXor);
		}

		ast.ExprNode ParseExpressionBitwiseOr(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionBitwiseXor, ctx,
				x => x == Token.bitwiseOr);
		}

		ast.ExprNode ParseExpressionLogicalAnd(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionBitwiseOr, ctx,
				x => x == Token.logicalAnd);
		}

		ast.ExprNode ParseExpressionLogicalOr(ParseContext ctx)
		{
			return ParseLtr(ParseExpressionLogicalAnd, ctx,
				x => x == Token.logicalOr);
		}

		ast.ExprNode ParseExpressionTernary(ParseContext ctx)
		{
			var lhs=ParseExpressionLogicalOr(ctx);

			// Is it a ternary operator eg: condition ? true : false
			if (t.SkipOptional(Token.question))
			{
				var result=new ast.ExprNodeTernary(t.GetBookmark(), lhs);

				result.TrueResult=ParseExpressionTernary(ctx);

				t.SkipRequired(Token.colon);

				result.FalseResult=ParseExpressionTernary(ctx);

				return result;
			}

			return lhs;

		}

		ast.ExprNode ParseExpressionAssignment(ParseContext ctx)
		{
			// Parse the LHS
			var lhs = ParseExpressionTernary(ctx);

			// Parse all consecutive RHS
			while (true)
			{
				// Check operator at same precedence level
				if (t.token>=Token.assign && t.token<=Token.bitwiseAndAssign)
				{
					// Save the operator token
					var bmk = t.GetBookmark();
					t.Next();

					// Parse the RHS and join to the LHS with appropriate operator.
					lhs = new ast.ExprNodeAssignment(bmk, lhs, ParseExpressionTernary(ctx), bmk.token);
				}
				else
					return lhs;
			}
		}

		// Parse an expression that might include comma separated multiple
		// expressions.
		ast.ExprNode ParseCompositeExpressionNode(ParseContext ctx)
		{
			var lhs = ParseExpressionAssignment(ctx);
			if (t.token != Token.comma)
				return lhs;

			if ((ctx & ParseContext.AllowCompositeExpressions) == 0 && t.Warnings)
			{
				Console.WriteLine("{0}: warning: use of composite expression - are you sure this is what you intended?", t.GetBookmark());
			}

			var expr = new ast.ExprNodeComposite(t.GetBookmark());
			expr.Expressions.Add(lhs);

			while (t.SkipOptional(Token.comma))
			{
				expr.Expressions.Add(ParseExpressionAssignment(ctx));
			}

			return expr;
		}

		// Parse a single expression (ie: doesn't support comma operator)
		ast.ExprNode ParseSingleExpressionNode(ParseContext ctx)
		{
			return ParseExpressionAssignment(ctx);
		}

		
		// Parse an expression that might include comma separated multiple
		// expressions.
		ast.Expression ParseCompositeExpression(ParseContext ctx)
		{
			return new ast.Expression(ParseCompositeExpressionNode(ctx));
		}

		// Parse a single expression (ie: doesn't support comma operator)
		ast.Expression ParseSingleExpression(ParseContext ctx)
		{
			return new ast.Expression(ParseSingleExpressionNode(ctx));
		}
 
		// Parse a single variable declaration
		void ParseVarDecl(ParseContext ctx, ast.StatementVariableDeclaration decl)
		{
			// Variable name
			t.Require(Token.identifier);
			var name = t.identifier;
			t.Next();

			// Optional initial value
			ast.Expression InitialValue = null;
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
					if (t.Warnings)
					{
						Console.WriteLine("{0}: warning: unnecessary semicolon", t.GetBookmark());
					}
					t.Next();
					return null;
				}

				case Token.openBrace:
				{
					if (t.Warnings)
					{
						Console.WriteLine("{0}: warning: code block doesn't provide variable scope", t.GetBookmark());
					}

					t.Next();
					var stmt = new ast.StatementBlock(t.GetBookmark());
					while (!t.SkipOptional(Token.closeBrace))
					{
						stmt.AddStatement(ParseSingleStatement());
					}
					return stmt;
				}

				case Token.kw_debugger:
				{
					t.Next();
					t.SkipRequired(Token.semicolon);

					return new ast.StatementDebugger(bmk, Token.kw_debugger);
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

				case Token.kw_fallthrough:
				{
					// Fake statement - comment // fall through
					Token op = t.token;
					t.Next();
					return new ast.StatementBreakContinue(bmk, op, null);
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
						init = ParseVarDeclStatement(ParseContext.DisableInOperator | ParseContext.AllowCompositeExpressions);

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
								if ((exprstmtForEach.Expression.RootNode as ast.ExprNodeIdentifier) == null &&
									(exprstmtForEach.Expression.RootNode as ast.ExprNodeIndexer) == null)
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
						stmt.Increment = ParseCompositeExpression(ParseContext.AllowCompositeExpressions);
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
					stmt.Condition = ParseCompositeExpression(ParseContext.AllowCompositeExpressions);
					t.SkipRequired(Token.closeRound);
					t.SkipRequired(Token.semicolon);
					return stmt;
				}

				case Token.kw_while:
				{
					var stmt = new ast.StatementWhile(bmk);
					t.Next();
					t.SkipRequired(Token.openRound);
					stmt.Condition = ParseCompositeExpression(ParseContext.AllowCompositeExpressions);
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
					var stmt = new ast.StatementExpression(bmk, new ast.Expression(ParseFunction()));
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
						var spec = new AccessibilitySpec();
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
