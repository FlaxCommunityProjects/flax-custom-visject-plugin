using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisjectPlugin.Source.VisjectGraphs
{
	/// <summary>
	/// A graph parameter
	/// </summary>
	public class GraphParameter
	{
		public string Name;
		public int Index;
		public object Value;

		public int OutputIndex;

		public GraphParameter(string name, int index, object value, int outputIndex)
		{
			Name = name;
			Index = index;
			Value = value;
			OutputIndex = outputIndex;
		}

		public void Execute(GraphContext context)
		{
			context.Variables[OutputIndex] = Value;
		}
	}
}
