using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisjectPlugin.Source.GraphNodes
{
    /// <summary>
    /// A graph node that holds a parameter
    /// </summary>
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

}
