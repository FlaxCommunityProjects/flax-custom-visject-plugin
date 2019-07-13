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
using FlaxEditor.Surface;
using FlaxEditor.Viewport.Previews;
using FlaxEditor.Windows.Assets;
using FlaxEngine;
using FlaxEngine.GUI;
using FlaxEngine.Rendering;

namespace VisjectPlugin.Source.Editor
{
    public class ExpressionGraphWindow : VisjectSurfaceWindow<JsonAsset, ExpressionGraphSurface, ExpressionGraphPreview>
    {
        /// <summary>
        /// The properties proxy object.
        /// </summary>
        private sealed class PropertiesProxy
        {
            [EditorOrder(1000), EditorDisplay("Parameters"), CustomEditor(typeof(ParametersEditor)), NoSerialize]
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public ExpressionGraphWindow WinRef { get; set; }

            [EditorOrder(20), EditorDisplay("General"), Tooltip("It's for demo purposes")]
            public int DemoInteger { get; set; }

            /// <summary>
            /// Custom editor for editing parameters collection.
            /// </summary>
            /// <seealso cref="FlaxEditor.CustomEditors.CustomEditor" />
            public class ParametersEditor : CustomEditor
            {
                private static readonly object[] DefaultAttributes = { new LimitAttribute(float.MinValue, float.MaxValue, 0.1f) };

                private enum NewParameterType
                {
                    Float = (int)ParameterType.Float
                }

                /// <inheritdoc />
                public override DisplayStyle Style => DisplayStyle.InlineIntoParent;

                /// <inheritdoc />
                public override void Initialize(LayoutElementsContainer layout)
                {
                    var window = Values[0] as ExpressionGraphWindow;
                    var asset = window?.Asset;
                    if (asset == null || !asset.IsLoaded)
                    {
                        layout.Label("Loading...");
                        return;
                    }
                    var parameters = window.Surface.Parameters;

                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var p = parameters[i];
                        if (!p.IsPublic)
                            continue;

                        var pIndex = i;
                        var pValue = p.Value;
                        Type pType;
                        object[] attributes = null;
                        switch (p.Type)
                        {
                        case ParameterType.CubeTexture:
                            pType = typeof(CubeTexture);
                            break;
                        case ParameterType.Texture:
                        case ParameterType.NormalMap:
                            pType = typeof(Texture);
                            break;
                        case ParameterType.RenderTarget:
                        case ParameterType.RenderTargetArray:
                        case ParameterType.RenderTargetCube:
                        case ParameterType.RenderTargetVolume:
                            pType = typeof(RenderTarget);
                            break;
                        default:
                            pType = p.Value.GetType();
                            // TODO: support custom attributes with defined value range for parameter (min, max)
                            attributes = DefaultAttributes;
                            break;
                        }

                        var propertyValue = new CustomValueContainer(
                            pType,
                            pValue,
                            (instance, index) =>
                            {
                                var win = (ExpressionGraphWindow)instance;
                                return win.Surface.Parameters[pIndex].Value;
                            },
                            (instance, index, value) =>
                            {
                                // Set surface parameter
                                var win = (ExpressionGraphWindow)instance;
                                var action = new EditParamAction
                                {
                                    Window = win,
                                    Index = pIndex,
                                    Before = win.Surface.Parameters[pIndex].Value,
                                    After = value,
                                };
                                win.Surface.Undo.AddAction(action);
                                action.Do();
                            },
                            attributes
                        );

                        var propertyLabel = new DragablePropertyNameLabel(p.Name);
                        propertyLabel.Tag = pIndex;
                        propertyLabel.MouseLeftDoubleClick += (label, location) => StartParameterRenaming(pIndex, label);
                        propertyLabel.MouseRightClick += (label, location) => ShowParameterMenu(pIndex, label, ref location);
                        propertyLabel.Drag = DragParameter;
                        var property = layout.AddPropertyItem(propertyLabel);
                        property.Object(propertyValue);
                    }

                    if (parameters.Count > 0)
                        layout.Space(10);
                    else
                        layout.Label("No parameters");

                    // Parameters creating
                    var paramType = layout.Enum(typeof(NewParameterType));
                    paramType.Value = (int)NewParameterType.Float;
                    var newParam = layout.Button("Add parameter");
                    newParam.Button.Clicked += () => AddParameter((ParameterType)paramType.Value);
                }

                private DragData DragParameter(DragablePropertyNameLabel label)
                {
                    var win = (ExpressionGraphWindow)Values[0];
                    var parameter = win.Surface.Parameters[(int)label.Tag];
                    return DragNames.GetDragData(SurfaceParameter.DragPrefix, parameter.Name);
                }

                /// <summary>
                /// Shows the parameter context menu.
                /// </summary>
                /// <param name="index">The index.</param>
                /// <param name="label">The label control.</param>
                /// <param name="targetLocation">The target location.</param>
                private void ShowParameterMenu(int index, Control label, ref Vector2 targetLocation)
                {
                    var contextMenu = new ContextMenu();
                    contextMenu.AddButton("Rename", () => StartParameterRenaming(index, label));
                    contextMenu.AddButton("Delete", () => DeleteParameter(index));
                    contextMenu.Show(label, targetLocation);
                }

                /// <summary>
                /// Adds the parameter.
                /// </summary>
                /// <param name="type">The type.</param>
                private void AddParameter(ParameterType type)
                {
                    var win = Values[0] as ExpressionGraphWindow;
                    var asset = win?.Asset;
                    if (asset == null || !asset.IsLoaded)
                        return;

                    var action = new AddRemoveParamAction
                    {
                        Window = win,
                        IsAdd = true,
                        Name = "New parameter",
                        Type = type,
                    };
                    win.Surface.Undo.AddAction(action);
                    action.Do();
                }

                /// <summary>
                /// Starts renaming parameter.
                /// </summary>
                /// <param name="index">The index.</param>
                /// <param name="label">The label control.</param>
                private void StartParameterRenaming(int index, Control label)
                {
                    var win = (ExpressionGraphWindow)Values[0];
                    var parameter = win.Surface.Parameters[index];
                    var dialog = RenamePopup.Show(label, new Rectangle(0, 0, label.Width - 2, label.Height), parameter.Name, false);
                    dialog.Tag = index;
                    dialog.Renamed += OnParameterRenamed;
                }

                private void OnParameterRenamed(RenamePopup renamePopup)
                {
                    var index = (int)renamePopup.Tag;
                    var win = (ExpressionGraphWindow)Values[0];

                    var action = new RenameParamAction
                    {
                        Window = win,
                        Index = index,
                        Before = win.Surface.Parameters[index].Name,
                        After = renamePopup.Text,
                    };
                    win.Surface.Undo.AddAction(action);
                    action.Do();
                }

                /// <summary>
                /// Removes the parameter.
                /// </summary>
                /// <param name="index">The index.</param>
                private void DeleteParameter(int index)
                {
                    var win = (ExpressionGraphWindow)Values[0];

                    var action = new AddRemoveParamAction
                    {
                        Window = win,
                        IsAdd = false,
                        Index = index,
                    };
                    win.Surface.Undo.AddAction(action);
                    action.Do();
                }
            }

            /// <summary>
            /// Gathers parameters from the specified window.
            /// </summary>
            /// <param name="window">The window.</param>
            public void OnLoad(ExpressionGraphWindow window)
            {
                // Link
                WinRef = window;
            }

            /// <summary>
            /// Clears temporary data.
            /// </summary>
            public void OnClean()
            {
                // Unlink
                WinRef = null;
            }
        }

        private readonly PropertiesProxy _properties;

        private ExpressionGraph _assetInstance;

        /// <inheritdoc />
        public ExpressionGraphWindow(FlaxEditor.Editor editor, AssetItem item)
        : base(editor, item)
        {
            // Asset preview
            _preview = new ExpressionGraphPreview(true)
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
            _toolstrip.AddButton(editor.Icons.BracketsSlash32, () => ShowSourceCode(_asset)).LinkTooltip("Show generated shader source code");
        }

        /// <summary>
        /// Shows the source code window.
        /// </summary>
        /// <param name="asset">The asset.</param>
        public static void ShowSourceCode(JsonAsset asset)
        {
            FlaxEditor.Utilities.Utils.ShowSourceCode(asset.Data, "Asset JSON");
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

            // Load surface data from the asset
            byte[] data = ExpressionGraphSurface.LoadSurface(_asset, _assetInstance, true);
            if (data == null)
            {
                // Error
                Debug.LogError("Failed to load expression graph surface data.");
                return true;
            }

            // Load surface graph
            if (_surface.Load(data))
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
            // TODO: Document that I'm compiling it here!
            _surface.CompileSurface(_assetInstance);
            _surface.Save();
            return false;
        }

        /// <inheritdoc />
        protected override void OnParamEditUndo(EditParamAction action, object value)
        {
            base.OnParamEditUndo(action, value);

            // TODO: Update the asset value to have nice live preview
            //_assetInstance.Parameters ...uh, they don't have an index...
            //Asset.Parameters[action.Index].Value = value;
        }
    }
}
