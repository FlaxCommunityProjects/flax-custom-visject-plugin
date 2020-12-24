using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisjectPlugin.Source.VisjectGraphs
{
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

        /// <summary>
        /// Where do we get the inputs from
        /// </summary>
        public int[] InputIndices;

        /// <summary>
        /// Where should we write the outputs to
        /// </summary>
        public int[] OutputIndices;

        public GraphContext Context { get; private set; }

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

        public void Execute(GraphContext context)
        {
            Context = context;
            UpdateInputValues(context.Variables);
            context.ExecuteAction(GroupId, TypeId, MethodId, this);
        }

        protected void UpdateInputValues(IList<object> variables)
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

        public bool HasInputConnection(int index)
        {
            return InputIndices[index] != -1;
        }

        public T ValueAs<T>(int index)
        {
            return CastTo<T>(Values[index]);
        }

        public void Return<T>(int index, T returnValue)
        {
            Context.Variables[OutputIndices[index]] = returnValue;
        }
    }
}
