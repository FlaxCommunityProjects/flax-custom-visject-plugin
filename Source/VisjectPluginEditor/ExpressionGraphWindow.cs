using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEditor;
using FlaxEditor.Content;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.GUI;
using FlaxEditor.GUI;
using FlaxEditor.GUI.ContextMenu;
using FlaxEditor.GUI.Drag;
using FlaxEditor.Scripting;
using FlaxEditor.Surface;
using FlaxEditor.Viewport.Previews;
using FlaxEditor.Windows.Assets;
using FlaxEngine;
using FlaxEngine.GUI;

namespace VisjectPlugin.Source.Editor
{
    public class ExpressionGraphWindow : VisjectSurfaceWindow<JsonAsset, ExpressionGraphSurface, ExpressionGraphPreview>
    {
        /// <summary>
        /// Allowed parameter types
        /// </summary>
        private readonly ScriptType[] _newParameterTypes =
        {
            new ScriptType(typeof(float)),
            new ScriptType(typeof(Vector2)),
            new ScriptType(typeof(Vector3)),
            new ScriptType(typeof(Vector4)),
        };

        /// <inheritdoc />
        public override IEnumerable<ScriptType> NewParameterTypes => _newParameterTypes;

        /// <summary>
        /// The properties proxy object.
        /// </summary>
        private sealed class PropertiesProxy
        {
            [EditorOrder(1000), EditorDisplay("Parameters"), CustomEditor(typeof(ParametersEditor)), NoSerialize]
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public ExpressionGraphWindow Window { get; set; }

            [EditorOrder(20), EditorDisplay("General"), Tooltip("It's for demo purposes")]
            public int DemoInteger { get; set; }


            [HideInEditor, Serialize]
            public List<SurfaceParameter> Parameters
            {
                get => Window.Surface.Parameters;
                set => throw new Exception("No setter.");
            }

            /// <summary>
            /// Gathers parameters from the specified window.
            /// </summary>
            /// <param name="window">The window.</param>
            public void OnLoad(ExpressionGraphWindow window)
            {
                // Link
                Window = window;
            }

            /// <summary>
            /// Clears temporary data.
            /// </summary>
            public void OnClean()
            {
                // Unlink
                Window = null;
            }
        }

        private readonly PropertiesProxy _properties;

        private ExpressionGraph _assetInstance;

        /// <inheritdoc />
        public ExpressionGraphWindow(FlaxEditor.Editor editor, AssetItem item)
        : base(editor, item)
        {
            // Asset preview
            _preview = new ExpressionGraphPreview(false)
            {
                Parent = _split2.Panel1
            };

            // Asset properties proxy
            _properties = new PropertiesProxy();
            _propertiesEditor.Select(_properties);

            // Surface
            _surface = new ExpressionGraphSurface(this, Save, _undo)
            {
                Parent = _split1.Panel1,
                Enabled = false
            };

            // Toolstrip
            _toolstrip.AddSeparator();
            _toolstrip.AddButton(editor.Icons.BracketsSlash32, () => ShowSourceCodeWindow(_asset)).LinkTooltip("Show generated shader source code");
        }

        /// <summary>
        /// Shows the source code window.
        /// </summary>
        /// <param name="asset">The asset.</param>
        public static void ShowSourceCodeWindow(JsonAsset asset)
        {
            FlaxEditor.Utilities.Utils.ShowSourceCodeWindow(asset.Data, "Asset JSON");
        }

        /// <inheritdoc />
        protected override void UnlinkItem()
        {
            _properties.OnClean();
            _preview.ExpressionGraph = null;

            base.UnlinkItem();
        }

        /// <inheritdoc />
        protected override void OnAssetLinked()
        {
            _assetInstance = _asset.CreateInstance<ExpressionGraph>();
            _preview.ExpressionGraph = _assetInstance;

            base.OnAssetLinked();
        }

        /// <inheritdoc />
        public override string SurfaceName => "Expression Graph";

        /// <inheritdoc />
        public override byte[] SurfaceData
        {
            get => ExpressionGraphSurface.LoadSurface(_asset, _assetInstance, true);
            set
            {
                // Save data to the temporary asset
                if (ExpressionGraphSurface.SaveSurface(_asset, _assetInstance, value))
                {
                    // Error
                    _surface.MarkAsEdited();
                    Debug.LogError("Failed to save surface data");
                }
                // Optionally reset the preview
            }
        }

        /// <inheritdoc />
        protected override bool LoadSurface()
        {
            // Init asset properties and parameters proxy
            _properties.OnLoad(this);

            // Load surface graph
            if (_surface.Load())
            {
                // Error
                Debug.LogError("Failed to load expression graph surface.");
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        protected override bool SaveSurface()
        {
            _surface.CompileSurface(_assetInstance);
            _surface.Save();
            return false;
        }

        /// <inheritdoc />
        public override void SetParameter(int index, object value)
        {
            // Update the asset value to have nice live preview
            _assetInstance.Parameters.First(p => p.Index == index).Value = value;

            base.SetParameter(index, value);
        }
    }
}
