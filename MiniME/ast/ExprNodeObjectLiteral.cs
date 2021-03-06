﻿// 
//   MiniME - http://www.toptensoftware.com/minime
// 
//   The contents of this file are subject to the license terms as 
//	 specified at the web address above.
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MiniME.ast
{
	// Key/value pair for object literal 
	class KeyExpressionPair
	{
		public KeyExpressionPair(object key, ExprNode value)
		{
			Key = key;
			Value = value;
		}
		public object Key;
		public ExprNode Value;
	}

	// Represents an object literal (eg: {a:1,b:2,c:3})
	class ExprNodeObjectLiteral : ExprNode
	{
		// Constructor
		public ExprNodeObjectLiteral(Bookmark bookmark) : base(bookmark)
		{

		}

		// List of values (NB: don't use a dictionary as we need to maintain order)
		public List<KeyExpressionPair> Values = new List<KeyExpressionPair>();

		public override string ToString()
		{
			return "<object literal>";
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "object literal:");
			foreach (var e in Values)
			{
				writeLine(indent + 1, e.Key.ToString());
				e.Value.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			if (Values.Count == 0)
			{
				dest.Append("{}");
				return true;
			}

			dest.Append('{');
			dest.Indent();
			for (var i = 0; i < Values.Count; i++)
			{
				if (i > 0)
					dest.Append(',');

				dest.StartLine();

				// Key - if key is a valid identifier, don't quote it
				var kp = Values[i];
				if (kp.Key.GetType() == typeof(ast.ExprNodeIdentifier))
				{
					dest.Append(dest.Members.GetObfuscatedSymbol(((ast.ExprNodeIdentifier)kp.Key).Name));
				}
				else if (kp.Key.GetType() == typeof(String) && Tokenizer.IsIdentifier((string)kp.Key) && !Tokenizer.IsKeyword((string)kp.Key))
				{
					dest.Append((string)kp.Key);
				}
				else
				{
					ExprNodeLiteral.RenderValue(dest, kp.Key);
				}

				// Value
				dest.Append(':');
				kp.Value.Render(dest);
			}
			dest.Unindent();
			dest.StartLine();
			dest.Append('}');
			return true;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			foreach (var kp in Values)
			{
				kp.Value.Visit(visitor);
			}
		}

		public override ExprNode Simplify()
		{
			foreach (var i in Values)
			{
				i.Value = i.Value.Simplify();
			}
			return this;
		}

		public override bool HasSideEffects()
		{
			return false;
		}

	}
}
