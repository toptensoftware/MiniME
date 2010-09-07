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
	// Represents a preserved comment
	class StatementComment : Statement
	{
		// Constructor
		public StatementComment(Bookmark bookmark, string Comment) : base(bookmark)
		{
			this.Comment = Comment;
		}

		public override void Dump(int indent)
		{
			writeLine(indent, "preserved comment");
		}

		public override bool Render(RenderContext dest)
		{
			dest.ForceLineBreak();
			dest.Append(Comment);
			dest.ForceLineBreak();
			return false;
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
		}

		public string Comment;


	}
}
