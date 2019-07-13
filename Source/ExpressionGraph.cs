using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor;
using FlaxEngine;
using FlaxEngine.Utilities;
using VisjectPlugin.Source.GraphNodes;

namespace VisjectPlugin.Source
{
    public class ExpressionGraph
    {
        /// <summary>
        /// Serialized visject surface
        /// </summary>
        public byte[] VisjectSurface { get; set; }

        public static readonly ExecuteAction[][][] Actions;

        private static readonly Random _rng = new Random();

        static ExpressionGraph()
        {
            var actions = new Dictionary<int, Dictionary<int, Dictionary<int, ExecuteAction>>>();

            void AddAction(int groupId, int typeId, int methodId, ExecuteAction action)
            {
                if (!actions.TryGetValue(groupId, out var groupActions))
                {
                    groupActions = new Dictionary<int, Dictionary<int, ExecuteAction>>();
                    actions.Add(groupId, groupActions);
                }

                if (!groupActions.TryGetValue(typeId, out var typeActions))
                {
                    typeActions = new Dictionary<int, ExecuteAction>();
                    groupActions.Add(typeId, typeActions);
                }

                typeActions.Add(methodId, action);
            }
            ExecuteAction[][][] ActionsToArray()
            {
                var groupActionsCount = actions.Keys.Max() + 1;
                ExecuteAction[][][] groupActions = new ExecuteAction[groupActionsCount][][];

                foreach (var groupIdActions in actions)
                {
                    var typeActionsCount = groupIdActions.Value.Keys.Max() + 1;
                    ExecuteAction[][] typeActions = new ExecuteAction[typeActionsCount][];

                    foreach (var typeIdActions in groupIdActions.Value)
                    {
                        var methodActionsCount = typeIdActions.Value.Keys.Max() + 1;
                        ExecuteAction[] methodActions = new ExecuteAction[methodActionsCount];
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
            // TODO: Set node type?
            AddAction(6, 1, 0, (node) => { node.Return<object>(0, node.Values[0]); });

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

            // TODO: Cache and reuse this list
            List<object> variables = new List<object>(new object[_variablesLength]);

            for (int i = 0; i < Nodes.Length; i++)
            {
                Nodes[i].Execute(variables, Actions);
            }

            // Set the outputs
            OutputFloat = Output.InputAs<float>(0);
        }

        private GraphParameter[] _parameters;
        private GraphOutput _output;

        [Serialize]
        private GraphNode[] _nodes;

        [Serialize]
        private int _variablesLength;

        [NoSerialize]
        public GraphParameter[] Parameters
        {
            // Also fixes the Json serialisation
            get { return _parameters ?? (_parameters = _nodes.OfType<GraphParameter>().ToArray()); }
        }

        [NoSerialize]
        public GraphNode[] Nodes
        {
            get { return _nodes; }
            set
            {
                _nodes = value;
                _parameters = null;
                _output = null;
                _variablesLength = _nodes.Max(node => node.OutputIndices.DefaultIfEmpty(0).Max()) + 1;
            }
        }

        [NoSerialize]
        public GraphOutput Output
        {
            get { return _output ?? (_output = _nodes.OfType<GraphOutput>().First()); }
        }

        public float OutputFloat { get; private set; }
    }
}
