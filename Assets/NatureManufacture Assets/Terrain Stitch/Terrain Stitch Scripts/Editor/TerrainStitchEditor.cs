using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CSML;
using System.Linq;

namespace NatureManufacture.TerrainStitch.Editor
{
    /// <summary>
    /// Terrain stitch editor.
    /// </summary>
    public class TerrainStitchEditor : EditorWindow
    {
        /// <summary>
        /// The methods list.
        /// </summary>
        private readonly GUIContent[] _options =
        {
            new("Average Power"),
            new("Trend"),
        };

        private readonly TerrainStitch _terrainStitch = new();

        /// <summary>
        /// Init this instance.
        /// </summary>
        [MenuItem("Tools/Nature Manufacture/Terrain Stitcher")]
        private static void Init()
        {
            GetWindow(typeof(TerrainStitchEditor), false, "Terrain Stitcher");
        }

        /// <summary>
        /// Raises the GU event.
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            GUIContent method = new GUIContent("Stitch Method", "Stitching method");
            _terrainStitch.SelectedMethod = EditorGUILayout.Popup(method, _terrainStitch.SelectedMethod, _options);

            EditorGUILayout.Space();

            if (_terrainStitch.SelectedMethod == 0)
            {
                _terrainStitch.LevelSmooth = EditorGUILayout.IntSlider("Smooth level ", (int) _terrainStitch.LevelSmooth, 5, 100);
                _terrainStitch.Power = EditorGUILayout.IntSlider("Power", (int) _terrainStitch.Power, 1, 7);
                _terrainStitch.CheckLength = EditorGUILayout.IntField("Average length", _terrainStitch.CheckLength);
            }
            else
            {
                _terrainStitch.CheckLength = EditorGUILayout.IntField("Trend lenght", _terrainStitch.CheckLength);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Stitch Selected Terrains"))
            {
                List<Terrain> terrains = new();

                foreach (var item in Selection.gameObjects)
                {
                    Terrain terrain = item.GetComponent<Terrain>();
                    if (terrain != null)
                        terrains.Add(terrain);
                }

                _terrainStitch.StitchTerrain(terrains);
            }

            if (GUILayout.Button("Stitch All Terrains"))
            {
                List<Terrain> terrains = new();
                terrains.AddRange(Terrain.activeTerrains);
                _terrainStitch.StitchTerrain(terrains);
            }

            if (_terrainStitch.ErrorLength)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Your terrain is smaller than stitch range!", MessageType.Error);
            }

            EditorGUILayout.Space();


            if (GUILayout.Button("Create terrain neighbours manager"))
            {
                _terrainStitch.CreateTerrainNeighbours();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            _terrainStitch.TestingShow = EditorGUILayout.Foldout(_terrainStitch.TestingShow, "Testing Options - use only on testing scene, will change terrains on scene");
            if (_terrainStitch.TestingShow)
            {
                if (GUILayout.Button("Random Noise on Selected Terrains"))
                {
                    List<Terrain> terrains = new();

                    foreach (var item in Selection.gameObjects)
                    {
                        Terrain terrain = item.GetComponent<Terrain>();
                        if (terrain != null)
                            terrains.Add(terrain);
                    }

                    _terrainStitch.RandomNoise(terrains);
                }

                if (GUILayout.Button("Random Noise on All Terrains"))
                {
                    List<Terrain> terrains = new();
                    terrains.AddRange(Terrain.activeTerrains);
                    _terrainStitch.RandomNoise(terrains);
                }
            }
        }
    }
}