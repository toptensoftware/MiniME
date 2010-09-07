// 
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
	// Represents object creation through `new` keyword
	class ExprNodeNew : ExprNode
	{
		// Constructor
		public ExprNodeNew(Bookmark bookmark, ExprNode objectType) : base(bookmark)
		{
			ObjectType= objectType;
		}

		// Attributes
		public ExprNode ObjectType;
		public List<ExprNode> Arguments = new List<ExprNode>();

		public override void Dump(int indent)
		{
			writeLine(indent, "New ");
			ObjectType.Dump(indent + 1);
			writeLine(indent, "with args:");
			foreach (var a in Arguments)
			{
				a.Dump(indent + 1);
			}
		}

		public override OperatorPrecedence GetPrecedence()
		{
			return OperatorPrecedence.terminal;
		}

		public override bool Render(RenderContext dest)
		{
			// Check for new Object and new Array
			var id = ObjectType as ast.ExprNodeIdentifier;
			if (id != null && id.Lhs == null)
			{
				if (id.Name == "Object" && Arguments.Count==0)
				{
					dest.Append("{}");
					return true;
				}
				if (id.Name == "Array")
				{
					if (Arguments.Count!=1)
					{
						dest.Append("[");
						bool first = true;
						foreach (var a in Arguments)
						{
							if (!first)
								dest.Append(",");
							a.Render(dest);
							first = false;
						}
						dest.Append("]");
						return true;
					}
					else
					{
						dest.Compiler.RecordWarning(Bookmark, "use of `new Array()` with one argument creates a sized array - are you sure?");
					}
				}
			}



			dest.Append("new");
			WrapAndRender(dest, ObjectType, false);
			dest.Append("(");
			bool bFirst = true;
			foreach (var a in Arguments)
			{
				if (!bFirst)
					dest.Append(",");
				a.Render(dest);
				bFirst = false;
			}
			dest.Append(")");
			return true;
		}
		public override void OnVisitChildNodes(IVisitor visitor)
		{
			ObjectType.Visit(visitor);
			foreach (var c in Arguments)
				c.Visit(visitor);
		}

		public override ExprNode Simplify()
		{
			ObjectType = ObjectType.Simplify();
			SimplifyList(Arguments);
			return this;
		}

		public override bool HasSideEffects()
		{
			return false;
		}

	}
}
