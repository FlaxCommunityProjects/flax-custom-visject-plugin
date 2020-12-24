using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlaxEditor;
using FlaxEditor.Surface;
using FlaxEditor.Surface.Elements;
using FlaxEngine;
using VisjectPlugin.Source.VisjectGraphs;

namespace VisjectPlugin.Source.Editor
{
    /// <summary>
    /// Customized Visject surface for the expression graph
    /// </summary>
    public class ExpressionGraphSurface : VisjectSurface
    {
        // Group and type id of the main node
        public const int MainNodeGroupId = 1;
        public const int MainNodeTypeId = 1;

        // Register the custom archetypes
        public ExpressionGraphSurface(IVisjectSurfaceOwner owner, Action onSave, Undo undo = null, SurfaceStyle style = null)
            : base(owner, onSave, undo, style, ExpressionGraphGroups) // Passing in our own archetypes
        {

        }

        // Our own node archetypes
        public static readonly NodeArchetype[] ExpressionGraphNodes =
        {
            // Main node
            new NodeArchetype
            {
                TypeID = 1,
                Title = "ExpressionGraph",
                Description = "Main number graph node",
                Flags = NodeFlags.AllGraphs | NodeFlags.NoRemove | NodeFlags.NoSpawnViaGUI | NodeFlags.NoCloseButton,
                Size = new Vector2(150, 300),
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Input(0, "Float", true, typeof(float), 0) // Last optional param: Value Index
                }
            },
            // Random float
            new NodeArchetype
            {
                TypeID = 2,
                Title = "Random float",
                Description = "A random float",
                Flags = NodeFlags.AllGraphs,
                Size = new Vector2(150, 30),
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Output(0, "Float", typeof(float), 0),
                }
            },
            // X-coordinate
            new NodeArchetype
            {
                TypeID = 3,
                Title = "Get X",
                Description = "Get the x coordinate",
                Flags = NodeFlags.AllGraphs,
                Size = new Vector2(150, 30),
                Elements = new[]
                {
                    NodeElementArchetype.Factory.Output(0, "Float", typeof(float), 0),
                }
            }
        };

        // Our group archetypes
        public static readonly List<GroupArchetype> ExpressionGraphGroups = new List<GroupArchetype>()
        {
            // Our own nodes, including the main node
            new GroupArchetype
            {
                GroupID = 1,
                Name = "ExpressionGraph",
                Color = new Color(231, 231, 60),
                Archetypes = ExpressionGraphNodes
            },
            // All math nodes
            new GroupArchetype
            {
                GroupID = 3,
                Name = "Math",
                Color = new Color(52, 152, 219),
                Archetypes = FlaxEditor.Surface.Archetypes.Math.Nodes
            },
            // Just a single parameter node
            new GroupArchetype
            {
                GroupID = 6,
                Name = "Parameters",
                Color = new Color(52, 73, 94),
                Archetypes = new []{ FlaxEditor.Surface.Archetypes.Parameters.Nodes[0] }
            }
        };

        /// <summary>
        /// Compiles the surface to an expression graph instance
        /// </summary>
        /// <param name="graph">Expression graph instance</param>
        public void CompileSurface(ExpressionGraph graph)
        {
            // We're mapping every output box to an index
            // So we can store the node outputs in an array
            var variableIndexGetter = new ExpressionGraphVariables();

            // Get the parameters
            GetParameterGetterNodeArchetype(out ushort paramNodeGroupId);

            var graphParams = new Dictionary<Guid, VisjectGraphs.GraphParameter>();
            for (int i = 0; i < Parameters.Count; i++)
            {
                var param = Parameters[i];
                graphParams.Add(param.ID, new VisjectGraphs.GraphParameter(param.Name, i, param.Value, variableIndexGetter.RegisterNewVariable()));
            }

            // Set the parameters
            graph.Parameters = graphParams.Values.ToArray();

            // Now go over the nodes (depth first) starting from the main node
            graph.Nodes = FindNode(MainNodeGroupId, MainNodeTypeId)
                .DepthFirstTraversal(true, true)
                // Turn surface nodes into graph nodes
                .Select(node =>
                {
                    // Internal node values
                    object[] nodeValues = (node.Values ?? new object[0]).ToArray();

                    // Input boxes - Values
                    object[] inputValues = node.Elements
                                            .OfType<InputBox>()
                                            .Select(inputBox =>
                                            {
                                                // Input box has a value
                                                int valueIndex = inputBox.Archetype.ValueIndex;
                                                return (valueIndex != -1) ? nodeValues[valueIndex] : null;
                                            })
                                            .ToArray();
                    // Input boxes - Indices
                    int[] inputIndices = node.Elements
                                            .OfType<InputBox>()
                                            .Select(inputBox =>
                                            {
                                                // Set the connections
                                                bool hasConnection = inputBox.HasAnyConnection;
                                                return (hasConnection) ? variableIndexGetter.UseInputBox(inputBox) : -1;
                                            })
                                            .ToArray();

                    // Output boxes
                    int[] outputIndices = node.Elements
                                                .OfType<OutputBox>()
                                                .Select(box => variableIndexGetter.RegisterOutputBox(box))
                                                .ToArray();

                    int groupId = node.GroupArchetype.GroupID;
                    int typeId = node.Archetype.TypeID;
                    int methodId = 0;

                    // Create the nodes
                    if (groupId == MainNodeGroupId && typeId == MainNodeTypeId)
                    {
                        // Main node
                        return new MainNode(groupId, typeId, methodId, nodeValues, inputValues, inputIndices, outputIndices);
                    }
                    else if (groupId == paramNodeGroupId)
                    {
                        // Parameter node
                        var graphParam = graphParams[(Guid)node.Values[0]];
                        return new GraphNode(groupId, typeId, methodId, new object[0], new object[1], new int[1] { graphParam.OutputIndex }, outputIndices);

                    }
                    else
                    {
                        // Generic node
                        return new GraphNode(groupId, typeId, methodId, nodeValues, inputValues, inputIndices, outputIndices);
                    }
                })
                .ToArray();
        }

        /// <summary>
        /// For saving and loading surfaces without having to create a proper surface context
        /// </summary>
        private class FakeSurfaceContext : ISurfaceContext
        {
            public string SurfaceName => throw new NotImplementedException();

            public byte[] SurfaceData { get; set; }

            public void OnContextCreated(VisjectSurfaceContext context)
            {

            }
        }

        /// <summary>
        /// Tries to load surface graph from the asset.
        /// </summary>
        /// <param name="createDefaultIfMissing">True if create default surface if missing, otherwise won't load anything.</param>
        /// <returns>Loaded surface bytes or null if cannot load it or it's missing.</returns>
        public static byte[] LoadSurface(JsonAsset asset, ExpressionGraph assetInstance, bool createDefaultIfMissing)
        {
            if (!asset) throw new ArgumentNullException(nameof(asset));
            if (assetInstance == null) throw new ArgumentNullException(nameof(assetInstance));

            // Return its data
            if (assetInstance.VisjectSurface?.Length > 0)
            {
                return assetInstance.VisjectSurface;
            }

            // Create it if it's missing
            if (createDefaultIfMissing)
            {
                // A bit of a hack
                // Create a Visject Graph with a main node and serialize it!
                var surfaceContext = new VisjectSurfaceContext(null, null, new FakeSurfaceContext());

                // Add the main node
                var node = NodeFactory.CreateNode(ExpressionGraphGroups, 1, surfaceContext, MainNodeGroupId, MainNodeTypeId);
                if (node == null)
                {
                    Debug.LogWarning("Failed to create main node.");
                    return null;
                }
                surfaceContext.Nodes.Add(node);
                // Initialize
                node.Location = Vector2.Zero;

                surfaceContext.Save();

                return surfaceContext.Context.SurfaceData;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the surface graph asset (save new one, discard cached data, reload asset).
        /// </summary>
        /// <param name="data">Surface data.</param>
        /// <returns>True if cannot save it, otherwise false.</returns>
        public static bool SaveSurface(JsonAsset asset, ExpressionGraph assetInstance, byte[] surfaceData)
        {
            if (!asset) throw new ArgumentNullException(nameof(asset));

            assetInstance.VisjectSurface = surfaceData;

            bool success = FlaxEditor.Editor.SaveJsonAsset(asset.Path, assetInstance);
            asset.Reload();
            return success;
        }
    }
}
