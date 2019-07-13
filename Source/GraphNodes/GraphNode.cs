using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisjectPlugin.Source.GraphNodes
{
    public delegate void ExecuteAction(GraphNode node);

    /// <summary>
    /// A generic graph node that can execute an action
    /// </summary>
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

        // TODO: Group the variables and actions into something like a context?
        public void Execute(IList<object> variables, ExecuteAction[][][] actions)
        {
            _variables = variables;
            UpdateInputValues(variables);
            actions[GroupId][TypeId][MethodId].Invoke(this);
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
}
