using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEditor;
using FlaxEditor.Content;
using FlaxEditor.Windows;
using FlaxEngine;

namespace VisjectPlugin.Source.Editor
{
	/// <summary>
	/// Asset proxy for the expression graph
	/// </summary>
	public class ExpressionGraphProxy : JsonAssetProxy
	{
		/// <inheritdoc />
		public override string Name => "Expression Graph";

		/// <inheritdoc />
		public override EditorWindow Open(FlaxEditor.Editor editor, ContentItem item)
		{
			// Open the correct window
			return new ExpressionGraphWindow(editor, (JsonAssetItem)item);
		}

		/// <inheritdoc />
		public override Color AccentColor => Color.FromRGB(0x0F0371);

		/// <inheritdoc />
		public override ContentDomain Domain => ContentDomain.Other;

		/// <inheritdoc />
		public override string TypeName { get; } = typeof(ExpressionGraph).FullName;

		/// <inheritdoc />
		public override bool CanCreate(ContentFolder targetLocation)
		{
			return targetLocation.CanHaveAssets;
		}

		/// <inheritdoc />
		public override void Create(string outputPath, object arg)
		{
			// Create a new expression graph
			FlaxEditor.Editor.SaveJsonAsset(outputPath, new ExpressionGraph());
		}
	}
}
