using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEngine;

namespace VisjectPlugin.Source.VisjectGraphs
{
	public class GraphContext
	{
		public delegate void ExecuteAction(GraphNode node);

		[NoSerialize]
		public List<object> Variables;

		[NoSerialize]
		public ExecuteAction[][][] Actions;

		public int IterationIndex;

		[NoSerialize]
		public int IterationsCount;

		public GraphContext(int variablesLength, ExecuteAction[][][] actions, int iterationsCount)
		{
			Variables = new List<object>(new object[variablesLength]);
			Actions = actions ?? throw new ArgumentNullException(nameof(actions));
			IterationsCount = iterationsCount;
		}
	}
}
