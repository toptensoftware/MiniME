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
	// Base class for all expression nodes
	class Expression : Node
	{
		// Constructor
		public Expression(ExprNode rootNode) : base(rootNode.Bookmark)
		{
			RootNode = rootNode;
		}

		public override void Dump(int indent)
		{
			RootNode.Dump(indent);
		}

		public override bool Render(RenderContext dest)
		{
			return RootNode.Render(dest);
		}

		public override void OnVisitChildNodes(IVisitor visitor)
		{
			RootNode.Visit(visitor);
		}

		public ExprNode RootNode;
	}

}
