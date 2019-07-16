using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace VisjectPlugin.Source.VisjectGraphs
{
	/// <summary>
	/// Current context, includes the internal variables
	/// </summary>
	public class GraphContext
	{
		public delegate void ExecuteActionCallback(int groupId, int typeId, int methodId, GraphNode graphNode);

		public List<object> Variables;

		public ExecuteActionCallback ExecuteAction;

		// This could include other variables such as "xy-coordinates"

		public GraphContext(int variablesLength, ExecuteActionCallback executeAction)
		{
			Variables = new List<object>(Enumerable.Repeat<object>(null, variablesLength));
			ExecuteAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
		}
	}
}
