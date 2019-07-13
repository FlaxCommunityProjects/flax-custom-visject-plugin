using System;
using System.Collections.Generic;
using System.Linq;
using FlaxEditor;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace VisjectPlugin.Source
{
    public class ExpressionGraph
    {
        /// <summary>
        /// Serialized visject surface
        /// </summary>
        public byte[] VisjectSurface { get; set; }

        // TODO: Stuff that you can execute in a built game

        public delegate void ExecuteAction(GraphNode node);

        public static readonly ExecuteAction[][][] Actions;

        private static Random _rng = new Random();

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
            AddAction(1, 1, 0, (node) => { });
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
            AddAction(6, 1, 0, (node) => { node.Return<object>(0, node.Values[0]); });

            Actions = ActionsToArray();
        }
        public class GraphNode
        {
            public int GroupId;
            public int TypeId;
            public int MethodId;

            /// <summary>
            /// Internal node values
            /// </summary>
            public object[] Values;

            /// <summary>
            /// Input values from input boxes
            /// </summary>
            public object[] InputValues;
            public int[] InputIndices;
            public int[] OutputIndices;

            private IList<object> _variables;

            public GraphNode(int groupId, int typeId, int methodId, object[] values, object[] inputValues, int[] inputIndices, int[] outputIndices)
            {
                GroupId = groupId;
                TypeId = typeId;
                MethodId = methodId;

                Values = values ?? throw new ArgumentNullException(nameof(values));
                InputValues = inputValues ?? throw new ArgumentNullException(nameof(inputValues));
                InputIndices = inputIndices ?? throw new ArgumentNullException(nameof(inputIndices));
                OutputIndices = outputIndices ?? throw new ArgumentNullException(nameof(outputIndices));
            }

            public void Execute(IList<object> variables)
            {
                _variables = variables;
                UpdateInputValues(variables);
                Actions[GroupId][TypeId][MethodId].Invoke(this);
            }

            public void UpdateInputValues(IList<object> variables)
            {
                for (int i = 0; i < InputIndices.Length; i++)
                {
                    if (InputIndices[i] != -1)
                    {
                        InputValues[i] = variables[InputIndices[i]];
                    }
                }
            }

            protected T CastTo<T>(object value)
            {
                if (value == null)
                {
                    return default(T);
                }
                else if (typeof(T) == typeof(float))
                {
                    // Special handling for numbers
                    // TODO: Replace this with something more efficient and/or better
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                else if (value is T castedValue)
                {
                    return castedValue;

                }
                else
                {
                    return default(T);
                }
            }

            public T InputAs<T>(int index)
            {
                return CastTo<T>(InputValues[index]);
            }

            public T ValueAs<T>(int index)
            {
                return CastTo<T>(Values[index]);
            }

            public void Return<T>(int index, T returnValue)
            {
                _variables[OutputIndices[index]] = returnValue;
            }
        }

        public class GraphParameter : GraphNode
        {
            public string Name;

            public GraphParameter(int groupId, int typeId, int methodId, string name, object value, int[] outputIndices)
                : base(groupId, typeId, methodId, new object[1] { value }, new object[0], new int[0], outputIndices)
            {
                Name = name;
                Value = value;
            }

            public object Value
            {
                get => Values[0];
                set => Values[0] = Value;
            }
        }

        public class OutputNode : GraphNode
        {
            public OutputNode(int groupId, int typeId, int methodId, object[] values, object[] inputValues, int[] inputIndices, int[] outputIndices)
                : base(groupId, typeId, methodId, values, inputValues, inputIndices, outputIndices)
            {
            }
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
                Nodes[i].Execute(variables);
            }

            // Set the outputs
            OutputFloat = Output.InputAs<float>(0);
        }

        private GraphParameter[] _parameters;
        private OutputNode _output;

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
        public OutputNode Output
        {
            get { return _output ?? (_output = _nodes.OfType<OutputNode>().First()); }
        }

        public float OutputFloat { get; private set; }
    }
}
