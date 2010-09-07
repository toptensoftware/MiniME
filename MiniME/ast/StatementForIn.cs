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
	// Represents a for-in loop
	class StatementForIn : Statement
	{
		// Constructor
		public StatementForIn(Bookmark bookmark) : base(bookmark)
		{
		}

		// Attributes
		// Either VariableDeclaration or Iterator used, not both
		public Statement VariableDeclaration;		// for (var x in ...)
		public Expression Iterator;				// for (x in ...)
		public Expression Collection;
		public CodeBlock Code;


		public override void Dump(int indent)
		{
			if (VariableDeclaration != null)
			{
				writeLine(indent, "for each loop, with new variable:");
			}
			else
			{
				writeLine(indent, "for each loop, with existing variable");
			}

			if (VariableDeclaration != null)
				VariableDeclaration.Dump(indent + 1);
			else
				Iterator.Dump(indent + 1);
			writeLine(indent, "collection:");
			Collection.Dump(indent + 1);
			writeLine(indent, "do:");
			Code.Dump(indent + 1);
		}

		public override bool Render(RenderContext dest)
		{
			dest.Append("for(");
			if (VariableDeclaration!=null)
				VariableDeclaration.Render(dest);
			else
				Iterator.Render(dest);
			dest.Append("in");
			Collection.Render(dest);
			dest.Append(")");
			return Code.RenderIndented(dest);
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			if (VariableDeclaration != null)
				VariableDeclaration.Visit(visitor);
			else
				Iterator.Visit(visitor);
			Collection.Visit(visitor);
			Code.Visit(visitor);
		}


	}
}
