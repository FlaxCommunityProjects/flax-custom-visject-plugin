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
    /// <summary>
    /// Small preview, only draws a number
    /// </summary>
    public class ExpressionGraphPreview : AssetPreview
    {
        private float[] _graphValues = new float[0];

        public ExpressionGraphPreview(bool useWidgets) : base(useWidgets)
        {
            ShowDefaultSceneActors = false;
            Task.Enabled = false;
        }

        /// <summary>
        /// Expression graph instance
        /// </summary>
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

            if (ExpressionGraph.OutputFloats.Length != _graphValues.Length)
            {
                _graphValues = new float[ExpressionGraph.OutputFloats.Length];
            }

            Vector2 scale = new Vector2(Width / _graphValues.Length, -10f);
            Vector2 offset = new Vector2(0, Height / 2f);

            // Horizontal line
            Render2D.DrawLine(new Vector2(0, offset.Y), new Vector2(Width, offset.Y), Color.Red);

            // Vertical line
            //Render2D.DrawLine(new Vector2(offset.X, 0), new Vector2(offset.X, Height), Color.Red);

            for (int i = 0; i < _graphValues.Length - 1; i++)
            {
                _graphValues[i] = Mathf.Lerp(_graphValues[i], ExpressionGraph.OutputFloats[i], 0.7f);

                Vector2 from = new Vector2(i, _graphValues[i]) * scale + offset;
                Vector2 to = new Vector2(i + 1, _graphValues[i + 1]) * scale + offset;
                Render2D.DrawLine(from, to, Color.White);
            }

        }

        /// <inheritdoc />
        public override void OnDestroy()
        {
            ExpressionGraph = null;
            base.OnDestroy();
        }
    }
}
