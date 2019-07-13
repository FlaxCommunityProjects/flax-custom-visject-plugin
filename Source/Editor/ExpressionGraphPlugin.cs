using System;
using System.Collections.Generic;
using FlaxEditor;
using FlaxEngine;

namespace VisjectPlugin.Source.Editor
{
    public class ExpressionGraphPlugin : EditorPlugin
    {

        private ExpressionGraphProxy _expressionGraphProxy;
        /// <inheritdoc />
        public override void InitializeEditor()
        {
            base.InitializeEditor();

            _expressionGraphProxy = new ExpressionGraphProxy();
            Editor.ContentDatabase.Proxy.Insert(0, _expressionGraphProxy);
        }

        /// <inheritdoc />
        public override void Deinitialize()
        {
            Editor.ContentDatabase.Proxy.Remove(_expressionGraphProxy);
            base.Deinitialize();
        }
    }
}
