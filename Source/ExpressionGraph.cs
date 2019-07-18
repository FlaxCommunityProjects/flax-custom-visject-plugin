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
		public delegate void ExecuteActionHandler(GraphNode node);

		/// <summary>
		/// All possible actions
		/// </summary>
		protected static readonly ExecuteActionHandler[][][] Actions;

		protected static readonly Random _rng = new Random();

		/// <summary>
		/// Fill the possible actions <see cref="Actions"/>
		/// </summary>
		static ExpressionGraph()
		{
			// Some helpers to make filling the Actions[][][] easier.
			var actions = new Dictionary<int, Dictionary<int, Dictionary<int, ExecuteActionHandler>>>();

			void AddAction(int groupId, int typeId, int methodId, ExecuteActionHandler action)
			{
				if (!actions.TryGetValue(groupId, out var groupActions))
				{
					groupActions = new Dictionary<int, Dictionary<int, ExecuteActionHandler>>();
					actions.Add(groupId, groupActions);
				}

				if (!groupActions.TryGetValue(typeId, out var typeActions))
				{
					typeActions = new Dictionary<int, ExecuteActionHandler>();
					groupActions.Add(typeId, typeActions);
				}

				typeActions.Add(methodId, action);
			}
			ExecuteActionHandler[][][] ActionsToArray()
			{
				var groupActionsCount = actions.Keys.Max() + 1;
				ExecuteActionHandler[][][] groupActions = new ExecuteActionHandler[groupActionsCount][][];

				foreach (var groupIdActions in actions)
				{
					var typeActionsCount = groupIdActions.Value.Keys.Max() + 1;
					ExecuteActionHandler[][] typeActions = new ExecuteActionHandler[typeActionsCount][];

					foreach (var typeIdActions in groupIdActions.Value)
					{
						var methodActionsCount = typeIdActions.Value.Keys.Max() + 1;
						ExecuteActionHandler[] methodActions = new ExecuteActionHandler[methodActionsCount];
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

			Actions = ActionsToArray();
		}

		private GraphContext _context;
		private MainNode _outputNode;

		private GraphParameter[] _parameters;
		private GraphNode[] _nodes;

		/// <summary>
		/// Serialized visject surface
		/// </summary>
		public byte[] VisjectSurface { get; set; }

		public GraphParameter[] Parameters
		{
			get => _parameters;
			set => _parameters = value;
		}

		public GraphNode[] Nodes
		{
			get { return _nodes; }
			set
			{
				_nodes = value;
				OnNodesSet();
			}
		}

		[NoSerialize]
		public float OutputFloat { get; private set; }

		public void Update(float deltaTime)
		{
			if (Nodes == null || Nodes.Length <= 0) return;

			// Update the parameters
			// Each parameter will write its Value to the context
			for (int i = 0; i < Parameters.Length; i++)
			{
				Parameters[i].Execute(_context);
			}
			// Update the nodes
			// Each node will get its inputs from the context
			//    Then, each node will execute its associated action
			//    Lastly, it will write the outputs to the context
			for (int i = 0; i < Nodes.Length; i++)
			{
				Nodes[i].Execute(_context);
			}

			// Final outputs
			OutputFloat = _outputNode.InputAs<float>(0);
		}


		protected void OnNodesSet()
		{
			if (Nodes == null || Nodes.Length <= 0)
			{
				_outputNode = null;
				_context = null;

			}
			else
			{
				_outputNode = _nodes.OfType<MainNode>().First();

				int maxVariableIndex = Math.Max(
						Parameters.Select(p => p.OutputIndex).DefaultIfEmpty(0).Max(),
						_nodes.Max(node => node.OutputIndices.DefaultIfEmpty(0).Max())
					);
				_context = new GraphContext(maxVariableIndex + 1, ExecuteAction);
			}
		}

		protected void ExecuteAction(int groupId, int typeId, int methodId, GraphNode graphNode)
		{
			Actions[groupId][typeId][methodId].Invoke(graphNode);
		}
	}
}
