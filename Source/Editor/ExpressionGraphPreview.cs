using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEditor.Viewport.Previews;
using FlaxEngine;
using FlaxEngine.GUI;

namespace VisjectPlugin.Source.Editor
{
    public class ExpressionGraphPreview : AssetPreview
    {
        public ExpressionGraphPreview(bool useWidgets) : base(useWidgets)
        {
        }

        public ExpressionGraph ExpressionGraph { get; set; }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // Manually update simulation
            ExpressionGraph?.Update(deltaTime);
        }

        /// <inheritdoc />
        public override void Draw()
        {
            base.Draw();

            if (ExpressionGraph == null) return;

            Render2D.DrawText(
                Style.Current.FontLarge,
                $"Float: {ExpressionGraph.OutputFloat}\n",
                new Rectangle(Vector2.Zero, Size),
                Color.Wheat,
                TextAlignment.Near,
                TextAlignment.Far);

        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            ExpressionGraph = null;
            base.OnDestroy();
        }

        /*
         * TODO:
         *  /// <inheritdoc />
        protected override void OnParamEditUndo(EditParamAction action, object value)
        {
            base.OnParamEditUndo(action, value);

            // Update the asset value to have nice live preview
            Asset.Parameters[action.Index].Value = value;
        }*/
    }
}
