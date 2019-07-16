using System;
using System.Collections.Generic;
using FlaxEditor;
using FlaxEngine;

namespace VisjectPlugin.Source.Editor
{
	/// <summary>
	/// Plugin for the expression graph
	/// </summary>
	public class ExpressionGraphPlugin : EditorPlugin
	{

		private ExpressionGraphProxy _expressionGraphProxy;
		/// <inheritdoc />
		public override void InitializeEditor()
		{
			base.InitializeEditor();

			// Register the proxy
			_expressionGraphProxy = new ExpressionGraphProxy();
			Editor.ContentDatabase.Proxy.Insert(0, _expressionGraphProxy);
		}

		/// <inheritdoc />
		public override void Deinitialize()
		{
			// Un-register the proxy! 
			Editor.ContentDatabase.Proxy.Remove(_expressionGraphProxy);
			base.Deinitialize();
		}
	}
}
