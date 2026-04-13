using UnityEngine;
using System.Collections.Generic;

namespace NatureManufacture.TerrainStitch
{
    /// <summary>
    /// Sets Terrain neighbours.
    /// </summary>
    public class TerrainNeighbours : MonoBehaviour
    {
        private Terrain[] _terrains;
        private Dictionary<int[], Terrain> _terrainDict;


        /// <summary>
        /// The first position for terrain tile management.
        /// </summary>
        public Vector2 firstPosition;

        /// <summary>
        /// Start this instance and creates neighbours for scene terrains
        /// </summary>
        private void Start()
        {
            CreateNeighbours();
        }

        /// <summary>
        /// Sets the neighbours for all terrains in scenes
        /// </summary>
        public void CreateNeighbours()
        {
            if (_terrainDict == null)
                _terrainDict = new Dictionary<int[], Terrain>(new IntArrayComparer());
            else
            {
                _terrainDict.Clear();
            }

            _terrains = Terrain.activeTerrains;
            if (_terrains.Length > 0)
            {
                firstPosition = new Vector2(_terrains[0].transform.position.x, _terrains[0].transform.position.z);

                int sizeX = (int) _terrains[0].terrainData.size.x;
                int sizeZ = (int) _terrains[0].terrainData.size.z;
                foreach (var terrain in _terrains)
                {
                    var position = terrain.transform.position;
                    int[] posTer =
                    {
                        Mathf.RoundToInt((position.x - firstPosition.x) / sizeX),
                        Mathf.RoundToInt((position.z - firstPosition.y) / sizeZ)
                    };
                    _terrainDict.Add(posTer, terrain);
                }

                foreach (var item in _terrainDict)
                {
                    int[] posTer = item.Key;
                    _terrainDict.TryGetValue(new[]
                    {
                        posTer[0],
                        posTer[1] + 1
                    }, out var top);
                    _terrainDict.TryGetValue(new[]
                    {
                        posTer[0] - 1,
                        posTer[1]
                    }, out var left);
                    _terrainDict.TryGetValue(new[]
                    {
                        posTer[0] + 1,
                        posTer[1]
                    }, out var right);
                    _terrainDict.TryGetValue(new[]
                    {
                        posTer[0],
                        posTer[1] - 1
                    }, out var bottom);
                    item.Value.SetNeighbors(left, top, right, bottom);
                    item.Value.Flush();
                }
            }
        }
    }
}