using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEditor.Surface.Elements;
using FlaxEngine;

namespace VisjectPlugin.Source.Editor
{
	// TODO: Well, this part might be too fancy
	public class ExpressionGraphVariables
	{
		private class Variable
		{
			public int Index;
			public int UsageCount;
		}

		private Dictionary<Int2, Variable> _connectionIds = new Dictionary<Int2, Variable>();
		private HashSet<int> _takenConnectionIds = new HashSet<int>();

		/// <summary>
		/// Register a output box
		/// </summary>
		/// <param name="box">An output box</param>
		/// <returns>The id of the output box or -1</returns>
		public int RegisterOutputBox(OutputBox box)
		{
			if (!box.IsOutput)
			{
				throw new ArgumentException("Not an output box", nameof(box));
			}

			if (box.Connections.Count <= 0) return -1;

			// If this box already has been registered
			if (_connectionIds.TryGetValue(GetBoxId(box), out var variable))
			{
				return variable.Index;
			}

			// Otherwise, register it
			for (int i = 0; ; i++)
			{
				if (!_takenConnectionIds.Contains(i))
				{
					_connectionIds.Add(GetBoxId(box), new Variable() { Index = i, UsageCount = box.Connections.Count });
					_takenConnectionIds.Add(i);

					return i;
				}
			}
		}

		/// <summary>
		/// Registers a new variable 
		/// </summary>
		/// <remarks>This variable cannot be unregistered anymore</remarks>
		/// <returns>Index of the variable</returns>
		public int RegisterNewVariable()
		{
			for (int i = 0; ; i++)
			{
				if (!_takenConnectionIds.Contains(i))
				{
					//_connectionIds.Add(GetBoxId(box), new Variable() { Index = i, UsageCount = box.Connections.Count });
					_takenConnectionIds.Add(i);

					return i;
				}
			}
		}

		/// <summary>
		/// Use an input box
		/// </summary>
		/// <param name="box">An input box</param>
		/// <returns>The id of the associated output box or -1</returns>
		public int UseInputBox(InputBox box)
		{
			if (!box.HasAnyConnection) return -1;

			var outputBox = box.Connections[0];

			if (_connectionIds.TryGetValue(GetBoxId(outputBox), out var variable))
			{
				variable.UsageCount--;
				if (variable.UsageCount <= 0)
				{
					RemoveOutputBox(outputBox);
				}
				return variable.Index;
			}
			else
			{
				throw new Exception("Parent box hasn't been registered");
			}
		}

		private Int2 GetBoxId(Box box)
		{
			return new Int2((int)box.ParentNode.ID, box.ID);
		}

		private void RemoveOutputBox(Box box)
		{
			if (_connectionIds.TryGetValue(GetBoxId(box), out var variable))
			{
				_connectionIds.Remove(GetBoxId(box));
				_takenConnectionIds.Remove(variable.Index);
			}
		}
	}
}
