using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisjectPlugin.Source.VisjectGraphs
{
	/// <summary>
	/// The main output node of the graph
	/// </summary>
	public class GraphOutput : GraphNode
	{
		public GraphOutput(int groupId, int typeId, int methodId, object[] values, object[] inputValues, int[] inputIndices, int[] outputIndices)
			: base(groupId, typeId, methodId, values, inputValues, inputIndices, outputIndices)
		{
		}
	}
}
