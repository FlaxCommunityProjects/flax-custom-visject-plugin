using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor;
using FlaxEngine;
using FlaxEngine.Utilities;
using VisjectPlugin.Source.VisjectGraphs;

namespace VisjectPlugin.Source
{
	public class ExpressionGraph
	{
		/// <summary>
		/// Serialized visject surface
		/// </summary>
		public byte[] VisjectSurface { get; set; }

		public static readonly GraphContext.ExecuteAction[][][] Actions;

		private static readonly Random _rng = new Random();

		static ExpressionGraph()
		{
			var actions = new Dictionary<int, Dictionary<int, Dictionary<int, GraphContext.ExecuteAction>>>();

			void AddAction(int groupId, int typeId, int methodId, GraphContext.ExecuteAction action)
			{
				if (!actions.TryGetValue(groupId, out var groupActions))
				{
					groupActions = new Dictionary<int, Dictionary<int, GraphContext.ExecuteAction>>();
					actions.Add(groupId, groupActions);
				}

				if (!groupActions.TryGetValue(typeId, out var typeActions))
				{
					typeActions = new Dictionary<int, GraphContext.ExecuteAction>();
					groupActions.Add(typeId, typeActions);
				}

				typeActions.Add(methodId, action);
			}
			GraphContext.ExecuteAction[][][] ActionsToArray()
			{
				var groupActionsCount = actions.Keys.Max() + 1;
				GraphContext.ExecuteAction[][][] groupActions = new GraphContext.ExecuteAction[groupActionsCount][][];

				foreach (var groupIdActions in actions)
				{
					var typeActionsCount = groupIdActions.Value.Keys.Max() + 1;
					GraphContext.ExecuteAction[][] typeActions = new GraphContext.ExecuteAction[typeActionsCount][];

					foreach (var typeIdActions in groupIdActions.Value)
					{
						var methodActionsCount = typeIdActions.Value.Keys.Max() + 1;
						GraphContext.ExecuteAction[] methodActions = new GraphContext.ExecuteAction[methodActionsCount];
						foreach (var methodIdAction in typeIdActions.Value)
						{
							methodActions[methodIdAction.Key] = methodIdAction.Value;
						}
						typeActions[typeIdActions.Key] = methodActions;
					}
					groupActions[groupIdActions.Key] = typeActions;
				}

				return groupActions;
			}
			// Main node
			AddAction(1, 1, 0, (_) => { });
			// Random float
			AddAction(1, 2, 0, (node) => { node.Return<float>(0, _rng.NextFloat()); });

			// Add
			AddAction(3, 1, 0, (node) => { node.Return<float>(0, node.InputAs<float>(0) + node.InputAs<float>(1)); });
			// Subtract
			AddAction(3, 2, 0, (node) => { node.Return<float>(0, node.InputAs<float>(0) - node.InputAs<float>(1)); });
			// Multiply
			AddAction(3, 3, 0, (node) => { node.Return<float>(0, node.InputAs<float>(0) * node.InputAs<float>(1)); });
			// Modulo
			AddAction(3, 4, 0, (node) => { node.Return<float>(0, node.InputAs<float>(0) % node.InputAs<float>(1)); });
			// Divide
			AddAction(3, 5, 0, (node) => { node.Return<float>(0, node.InputAs<float>(0) / node.InputAs<float>(1)); });
			// Absolute
			AddAction(3, 7, 0, (node) => { node.Return<float>(0, Mathf.Abs(node.InputAs<float>(0))); });
			// Ceil
			AddAction(3, 8, 0, (node) => { node.Return<float>(0, Mathf.Ceil(node.InputAs<float>(0))); });
			// Cosine
			AddAction(3, 9, 0, (node) => { node.Return<float>(0, Mathf.Cos(node.InputAs<float>(0))); });
			// Floor
			AddAction(3, 10, 0, (node) => { node.Return<float>(0, Mathf.Floor(node.InputAs<float>(0))); });
			// Round
			AddAction(3, 13, 0, (node) => { node.Return<float>(0, Mathf.Round(node.InputAs<float>(0))); });
			// Saturate
			AddAction(3, 14, 0, (node) => { node.Return<float>(0, Mathf.Saturate(node.InputAs<float>(0))); });
			// Sine
			AddAction(3, 15, 0, (node) => { node.Return<float>(0, Mathf.Sin(node.InputAs<float>(0))); });
			// Sqrt
			AddAction(3, 16, 0, (node) => { node.Return<float>(0, Mathf.Sqrt(node.InputAs<float>(0))); });
			// Tangent
			AddAction(3, 17, 0, (node) => { node.Return<float>(0, Mathf.Tan(node.InputAs<float>(0))); });

			// Power
			AddAction(3, 23, 0, (node) => { node.Return<float>(0, Mathf.Pow(node.InputAs<float>(0), node.InputAs<float>(1))); });

			// Parameter
			AddAction(6, 1, 0, (node) => { node.Return<object>(0, node.InputValues[0]); });
			// TODO: node.ContextAs<ExpressionGraphContext>()

			Actions = ActionsToArray();
		}

		private float _accumulatedTime = 0;
		private const float UpdatesPerSecond = 3;
		private const float UpdateDuration = 1f / UpdatesPerSecond;

		public void Update(float deltaTime)
		{
			_accumulatedTime += deltaTime;
			if (_accumulatedTime < UpdateDuration) return;

			_accumulatedTime = 0;

			if (Nodes == null || Nodes.Length <= 0) return;

			if (_context == null)
			{
				_context = new GraphContext(_variablesLength, ExpressionGraph.Actions, 1);
			}

			_context.IterationIndex = 0;
			for (int i = 0; i < Parameters.Length; i++)
			{
				Parameters[i].Execute(_context);
			}
			for (int i = 0; i < Nodes.Length; i++)
			{
				Nodes[i].Execute(_context);
			}

			// Set the outputs
			OutputFloat = Output.InputAs<float>(0);
		}

		private GraphContext _context;

		private GraphOutput _output;

		[Serialize]
		private GraphNode[] _nodes;

		[Serialize]
		private int _variablesLength;

		[Serialize]
		public GraphParameter[] Parameters { get; set; }

		[NoSerialize]
		public GraphNode[] Nodes
		{
			get { return _nodes; }
			set
			{
				_nodes = value;
				_output = null;
				_context = null;
				_variablesLength = Math.Max(Parameters.Max(p => p.OutputIndex) + 1, _nodes.Max(node => node.OutputIndices.DefaultIfEmpty(0).Max()) + 1);
			}
		}

		[NoSerialize]
		public GraphOutput Output
		{
			// Also fixes the Json serialisation
			get { return _output ?? (_output = _nodes.OfType<GraphOutput>().First()); }
		}

		public float OutputFloat { get; private set; }
	}
}
