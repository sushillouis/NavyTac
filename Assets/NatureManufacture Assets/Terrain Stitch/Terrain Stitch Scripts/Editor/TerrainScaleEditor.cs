using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NatureManufacture.TerrainStitch.Editor
{
    /// <summary>
    /// Terrain scale editor.
    /// </summary>
    public class TerrainScaleEditor : EditorWindow
    {
        /// <summary>
        /// The new height.
        /// </summary>
        private float _newHeight = 600f;

        /// <summary>
        /// The new height offset.
        /// </summary>
        private float _offset;


        /// <summary>
        /// Init this instance.
        /// </summary>
        [MenuItem("Tools/Nature Manufacture/Terrain Scaler")]
        private static void Init()
        {
            EditorWindow.GetWindow(typeof(TerrainScaleEditor), false, "Terrain Scaler");
        }

        /// <summary>
        /// Raises the GU event.
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);

            _newHeight = EditorGUILayout.FloatField("New height", _newHeight);
            _offset = EditorGUILayout.FloatField("Offset", _offset);

            EditorGUILayout.Space();

            if (GUILayout.Button("Rescale selected terrains"))
            {
                RescaleTerrain(true);
            }

            if (GUILayout.Button("Rescale all terrains"))
            {
                RescaleTerrain(false);
            }
        }

        /// <summary>
        /// Rescales the terrain.
        /// </summary>
        private void RescaleTerrain(bool selected)
        {
            List<Terrain> terrains = new List<Terrain>();
            if (selected)
            {
                foreach (var item in Selection.gameObjects)
                {
                    Terrain terrain = item.GetComponent<Terrain>();
                    if (terrain != null)
                        terrains.Add(terrain);
                }
            }
            else
                terrains.AddRange(Terrain.activeTerrains);

            foreach (var t in terrains)
            {
                Undo.RegisterCompleteObjectUndo(t.terrainData, "Rescale terrains");
            }

            foreach (var terrain in terrains)
            {
                TerrainData terrainData = terrain.terrainData;

                float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
                Vector3 terrainSize = terrainData.size;
                float scale = terrainSize.y / _newHeight;
                float offsetScaled = _offset / _newHeight;
                terrainSize.y = _newHeight;
                terrainData.size = terrainSize;
                terrain.Flush();
                for (int i = 0; i < heights.GetLength(0); i++)
                {
                    for (int j = 0; j < heights.GetLength(1); j++)
                    {
                        heights[i, j] = heights[i, j] * scale + offsetScaled;
                    }
                }

                terrainData.SetHeights(0, 0, heights);
                terrain.terrainData = terrainData;
                terrain.Flush();
            }
        }
    }
}