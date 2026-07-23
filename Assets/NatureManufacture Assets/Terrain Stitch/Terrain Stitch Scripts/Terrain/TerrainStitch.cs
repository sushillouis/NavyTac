// /**
//  * Created by Pawel Homenko on  09/2022
//  */

using System.Collections.Generic;
using System.Linq;
using CSML;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace NatureManufacture.TerrainStitch
{
    public class TerrainStitch
    {
        /// <summary>
        /// The first position for terrain tile management.
        /// </summary>
        private Vector2 _firstPosition;

        /// <summary>
        /// The _terrains to stitch.
        /// </summary>
        private Terrain[] _terrains;

        /// <summary>
        /// The _terrain dict holds terrain positions.
        /// </summary>
        private Dictionary<int[], Terrain> _terrainDict;

        /// <summary>
        /// The testing options show.
        /// </summary>
        private bool _testingShow;

        /// <summary>
        /// The level smooth.
        /// </summary>
        private float _levelSmooth = 16;

        /// <summary>
        /// The length of the stitch check.
        /// </summary>
        private int _checkLength = 100;

        /// <summary>
        /// The power of average function.
        /// </summary>
        private float _power = 7.0f;

        /// <summary>
        /// The selected method.
        /// </summary>
        private int _selectedMethod;

        private bool _errorLength;

        private enum Side
        {
            //Left,
            Right,
            Top,
            // Bottom
        }

        /// <summary>
        /// The first position for terrain tile management.
        /// </summary>
        public Vector2 FirstPosition
        {
            get => _firstPosition;
            set => _firstPosition = value;
        }

        /// <summary>
        /// The _terrains to stitch.
        /// </summary>
        public Terrain[] Terrains
        {
            get => _terrains;
            set => _terrains = value;
        }

        /// <summary>
        /// The _terrain dict holds terrain positions.
        /// </summary>
        public Dictionary<int[], Terrain> TerrainDict
        {
            get => _terrainDict;
            set => _terrainDict = value;
        }

        /// <summary>
        /// The testing options show.
        /// </summary>
        public bool TestingShow
        {
            get => _testingShow;
            set => _testingShow = value;
        }

        /// <summary>
        /// The level smooth.
        /// </summary>
        public float LevelSmooth
        {
            get => _levelSmooth;
            set => _levelSmooth = value;
        }

        /// <summary>
        /// The length of the stitch check.
        /// </summary>
        public int CheckLength
        {
            get => _checkLength;
            set => _checkLength = value;
        }

        /// <summary>
        /// The power of average function.
        /// </summary>
        public float Power
        {
            get => _power;
            set => _power = value;
        }

        /// <summary>
        /// The selected method.
        /// </summary>
        public int SelectedMethod
        {
            get => _selectedMethod;
            set => _selectedMethod = value;
        }

        public bool ErrorLength
        {
            get => _errorLength;
            set => _errorLength = value;
        }

        /// <summary>
        /// Creates the terrain neighbours manager.
        /// </summary>
        public void CreateTerrainNeighbours()
        {
            GameObject go = new GameObject("_TerrainNeighboursManager");
            TerrainNeighbours terrainNeighbours = go.AddComponent<TerrainNeighbours>();
            terrainNeighbours.CreateNeighbours();
        }

        /// <summary>
        /// Randoms the noise on terrains.
        /// </summary>
        /// <param name="terrains">Terrains to randomize height</param>
        public void RandomNoise(List<Terrain> terrains)
        {
#if UNITY_EDITOR
            foreach (var t in terrains)
            {
                Undo.RegisterCompleteObjectUndo(t.terrainData, "Noise terrains");
            }
#endif

            foreach (var item in terrains)
            {
                GenerateHeights(item, 5);
            }
        }

        /// <summary>
        /// Generates the heights.
        /// </summary>
        /// <param name="terrain">Terrain.</param>
        /// <param name="tileSize">Tile size.</param>
        public void GenerateHeights(Terrain terrain, float tileSize)
        {
            Vector2 randomMove = Random.insideUnitCircle * 1000;
            var terrainData = terrain.terrainData;
            float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];

            for (int i = 0; i < terrainData.heightmapResolution; i++)
            {
                for (int k = 0; k < terrainData.heightmapResolution; k++)
                {
                    heights[i, k] = Mathf.PerlinNoise((i / (float) terrainData.heightmapResolution) * tileSize + randomMove.x
                        , (k / (float) terrainData.heightmapResolution) * tileSize + randomMove.y) / 10.0f;
                }
            }

            terrainData.SetHeights(0, 0, heights);
        }

        /// <summary>
        /// Stitchs the terrain.
        /// </summary>
        /// <param name="terrains">Terrains to randomize height</param>
        public void StitchTerrain(List<Terrain> terrains)
        {
            ErrorLength = false;
            if (TerrainDict == null)
                TerrainDict = new Dictionary<int[], Terrain>(new IntArrayComparer());
            else
            {
                TerrainDict.Clear();
            }


            Terrains = terrains.ToArray();

            foreach (var item in terrains)
            {
                if (item.terrainData.heightmapResolution < CheckLength)
                {
                    ErrorLength = true;
                    return;
                }
            }
#if UNITY_EDITOR
            foreach (var t in terrains)
            {
                Undo.RegisterCompleteObjectUndo(t.terrainData, "Stitch terrains");
            }
#endif


            if (Terrains.Length > 0)
            {
                FirstPosition = new Vector2(Terrains[0].transform.position.x, Terrains[0].transform.position.z);

                int sizeX = (int) Terrains[0].terrainData.size.x;
                int sizeZ = (int) Terrains[0].terrainData.size.z;

                foreach (var terrain in Terrains)
                {
                    var terrainPosition = terrain.transform.position;
                    int[] posTer = new[]
                    {
                        Mathf.RoundToInt((terrainPosition.x - FirstPosition.x) / sizeX),
                        Mathf.RoundToInt((terrainPosition.z - FirstPosition.y) / sizeZ)
                    };
                    TerrainDict.Add(posTer, terrain);
                }

                //Checks neighbours and stitches them
                foreach (var item in TerrainDict)
                {
                    int[] posTer = item.Key;
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0],
                        posTer[1] + 1
                    }, out var top);
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0] - 1,
                        posTer[1]
                    }, out var left);
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0] + 1,
                        posTer[1]
                    }, out var right);
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0],
                        posTer[1] - 1
                    }, out var bottom);

                    item.Value.SetNeighbors(left, top, right, bottom);

                    item.Value.Flush();

                    if (SelectedMethod == 0 || CheckLength == 0)
                    {
                        if (right != null)
                        {
                            StitchTerrains(item.Value, right, Side.Right);
                        }

                        if (top != null)
                        {
                            StitchTerrains(item.Value, top, Side.Top);
                        }
                    }
                    else
                    {
                        if (top != null)
                            StitchTerrainsTrend(item.Value, top, Side.Top);

                        if (right != null)
                            StitchTerrainsTrend(item.Value, right, Side.Right);
                    }
                }

                //Repairs corners
                foreach (var item in TerrainDict)
                {
                    int[] posTer = item.Key;
                    // Terrain left = null;
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0],
                        posTer[1] + 1
                    }, out var top);
                    /*   _terrainDict.TryGetValue(new[]
                       {
                           posTer[0] - 1,
                           posTer[1]
                       }, out left);*/
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0] + 1,
                        posTer[1]
                    }, out var right);
                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0],
                        posTer[1] - 1
                    }, out var bottom);


                    int temptLength = CheckLength;
                    CheckLength = 0;

                    if (right != null)
                    {
                        StitchTerrains(item.Value, right, Side.Right, false);
                    }

                    if (top != null)
                    {
                        StitchTerrains(item.Value, top, Side.Top, false);
                    }

                    CheckLength = temptLength;

                    if (right == null || bottom == null) continue;

                    TerrainDict.TryGetValue(new[]
                    {
                        posTer[0] + 1,
                        posTer[1] - 1
                    }, out var rightBottom);
                    if (rightBottom != null)
                        StitchTerrainsRepair(item.Value, right, bottom, rightBottom);
                }
            }
        }

        /// <summary>
        /// Average the specified first and second value.
        /// </summary>
        /// <param name="first">First.</param>
        /// <param name="second">Second.</param>
        private float PowerAverage(float first, float second)
        {
            return Mathf.Pow((Mathf.Pow(first, Power) + Mathf.Pow(second, Power)) / 2.0f, 1 / Power);
        }

        /// <summary>
        /// Stitchs the terrains with trend method.
        /// </summary>
        /// <param name="terrain">First Terrain.</param>
        /// <param name="second">Second Terrain.</param>
        /// <param name="side">Side of stitch.</param>
        private void StitchTerrainsTrend(Terrain terrain, Terrain second, Side side)
        {
            TerrainData terrainData = terrain.terrainData;
            TerrainData secondData = second.terrainData;


            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            float[,] secondHeights = secondData.GetHeights(0, 0, secondData.heightmapResolution, secondData.heightmapResolution);


            if (side == Side.Right)
            {
                //int y = heights.GetLength (0) - 1;
                int x;

                //int x2 = 0;
                //int y2 = 0;

                string matrixAt = "";
                string matrixAtId = "";
                string matrixAtOnes = "";


                for (int i = 1; i <= CheckLength; i++)
                {
                    matrixAt += (i * i);
                    matrixAtId += i;
                    matrixAtOnes += "1";

                    matrixAt += ",";
                    matrixAtId += ",";
                    matrixAtOnes += ",";
                }

                for (int i = CheckLength; i <= CheckLength * 2; i++)
                {
                    matrixAt += (i * i);
                    matrixAtId += i;
                    matrixAtOnes += "1";

                    if (i < CheckLength * 2)
                    {
                        matrixAt += ",";
                        matrixAtId += ",";
                        matrixAtOnes += ",";
                    }
                }

                Matrix at = new Matrix(matrixAt + ";" + matrixAtId + ";" + matrixAtOnes);

                Matrix a = at.Transpose();
                Matrix ata = at * a;
                Matrix ata1 = ata.Inverse();

                for (x = 0; x < heights.GetLength(1); x++)
                {
                    string matrixZ = "";

                    for (int i = heights.GetLength(0) - CheckLength; i < heights.GetLength(0); i++)
                    {
                        matrixZ += heights[x, i] + ";";
                    }


                    for (int i = 0; i <= CheckLength; i++)
                    {
                        matrixZ += secondHeights[x, i];
                        if (i < CheckLength)
                        {
                            matrixZ += ";";
                        }
                    }

                    Matrix zMatrix = new Matrix(matrixZ);

                    Matrix atz = at * zMatrix;
                    Matrix xMatrix = ata1 * atz;


                    double trendAverage = CheckLength * CheckLength * xMatrix[1, 1].Re + CheckLength * xMatrix[2, 1].Re + xMatrix[3, 1].Re;

                    Matrix sAt = new Matrix("1," + (CheckLength * CheckLength) + "," + Mathf.Pow(2 * CheckLength, 2) + ";1," + CheckLength + "," + (CheckLength * 2) + ";1,1,1");

                    Matrix sA = sAt.Transpose();
                    Matrix sAta = sAt * sA;
                    Matrix sAta1 = sAta.Inverse();

                    Matrix sZ = new Matrix(heights[x, heights.GetLength(0) - CheckLength] + ";" + trendAverage + ";" + secondHeights[x, CheckLength]);


                    Matrix sAtz = sAt * sZ;
                    Matrix sX = sAta1 * sAtz;


                    double[] heightTrend = new double[CheckLength];
                    double[] secondHeightTrend = new double[CheckLength + 1];

                    for (int i = 1; i <= CheckLength; i++)
                    {
                        heightTrend[i - 1] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
                    }

                    int j = 0;
                    for (int i = CheckLength; i <= CheckLength * 2; i++)
                    {
                        secondHeightTrend[j] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
                        j++;
                    }


                    for (int i = 0; i < CheckLength; i++)
                    {
                        heights[x, heights.GetLength(1) - i - 1] = (float) heightTrend[CheckLength - i - 1] * (CheckLength - i) / CheckLength + heights[x, heights.GetLength(1) - i - 1] * i / CheckLength;
                    }


                    for (int i = 0; i <= CheckLength; i++)
                    {
                        secondHeights[x, i] = (float) secondHeightTrend[i] * (CheckLength - i) / CheckLength + secondHeights[x, i] * i / CheckLength;
                    }
                }
            }
            else
            {
                if (side == Side.Top)
                {
                    int y;
                    //int x = heights.GetLength (0) - 1;

                    //int x2 = 0;
                    //int y2 = 0;


                    string matrixAt = "";
                    string matrixAtId = "";
                    string matrixAtOnes = "";


                    for (int i = 1; i <= CheckLength; i++)
                    {
                        matrixAt += (i * i);
                        matrixAtId += i;
                        matrixAtOnes += "1";

                        matrixAt += ",";
                        matrixAtId += ",";
                        matrixAtOnes += ",";
                    }

                    for (int i = CheckLength; i <= CheckLength * 2; i++)
                    {
                        matrixAt += (i * i);
                        matrixAtId += i;
                        matrixAtOnes += "1";

                        if (i < CheckLength * 2)
                        {
                            matrixAt += ",";
                            matrixAtId += ",";
                            matrixAtOnes += ",";
                        }
                    }

                    Matrix at = new Matrix(matrixAt + ";" + matrixAtId + ";" + matrixAtOnes);

                    Matrix aMatrix = at.Transpose();
                    Matrix ata = at * aMatrix;
                    Matrix ata1 = ata.Inverse();

                    for (y = 0; y < heights.GetLength(1); y++)
                    {
                        string matrixZ = "";

                        for (int i = heights.GetLength(0) - CheckLength; i < heights.GetLength(0); i++)
                        {
                            matrixZ += heights[i, y] + ";";
                        }

                        for (int i = 0; i <= CheckLength; i++)
                        {
                            matrixZ += secondHeights[i, y];
                            if (i < CheckLength)
                            {
                                matrixZ += ";";
                            }
                        }

                        Matrix zMatrix = new Matrix(matrixZ);

                        Matrix atz = at * zMatrix;
                        Matrix xMatrix = ata1 * atz;


                        double trendAverage = CheckLength * CheckLength * xMatrix[1, 1].Re + CheckLength * xMatrix[2, 1].Re + xMatrix[3, 1].Re;

                        Matrix sAt = new Matrix("1," + (CheckLength * CheckLength) + "," + Mathf.Pow(2 * CheckLength, 2) + ";1," + CheckLength + "," + (CheckLength * 2) + ";1,1,1");

                        Matrix sA = sAt.Transpose();
                        Matrix sAta = sAt * sA;
                        Matrix sAta1 = sAta.Inverse();

                        Matrix sZ = new Matrix(heights[heights.GetLength(0) - CheckLength, y] + ";" + trendAverage + ";" + secondHeights[CheckLength, y]);


                        Matrix sAtz = sAt * sZ;
                        Matrix sX = sAta1 * sAtz;


                        double[] heightTrend = new double[CheckLength];
                        double[] secondHeightTrend = new double[CheckLength + 1];

                        for (int i = 1; i <= CheckLength; i++)
                        {
                            heightTrend[i - 1] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
                        }

                        int j = 0;

                        for (int i = CheckLength; i <= CheckLength * 2; i++)
                        {
                            secondHeightTrend[j] = i * i * sX[1, 1].Re + i * sX[2, 1].Re + sX[3, 1].Re;
                            j++;
                        }


                        for (int i = 0; i < CheckLength; i++)
                        {
                            heights[heights.GetLength(0) - i - 1, y] = (float) heightTrend[CheckLength - i - 1] * (CheckLength - i) / CheckLength + heights[heights.GetLength(0) - i - 1, y] * i / CheckLength;
                        }

                        for (int i = 0; i <= CheckLength; i++)
                        {
                            secondHeights[i, y] = (float) secondHeightTrend[i] * (CheckLength - i) / CheckLength + secondHeights[i, y] * i / CheckLength;
                        }
                    }
                }
            }


            terrainData.SetHeights(0, 0, heights);
            terrain.terrainData = terrainData;

            secondData.SetHeights(0, 0, secondHeights);
            second.terrainData = secondData;

            terrain.Flush();
            second.Flush();
        }

        /// <summary>
        /// Stitchs the terrains with power average.
        /// </summary>
        /// <param name="terrain">First Terrain.</param>
        /// <param name="second">Second Terrain.</param>
        /// <param name="side">Side of stitch.</param>
        /// <param name="smooth">If set to <c>true</c> smooth.</param>
        private void StitchTerrains(Terrain terrain, Terrain second, Side side, bool smooth = true)
        {
            TerrainData terrainData = terrain.terrainData;
            TerrainData secondData = second.terrainData;


            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            float[,] secondHeights = secondData.GetHeights(0, 0, secondData.heightmapResolution, secondData.heightmapResolution);


            if (side == Side.Right)
            {
                int y = heights.GetLength(0) - 1;
                int x;

                int y2 = 0;

                for (x = 0; x < heights.GetLength(1); x++)
                {
                    heights[x, y] = PowerAverage(heights[x, y], secondHeights[x, y2]);

                    if (smooth)
                        heights[x, y] += Mathf.Abs(heights[x, y - 1] - secondHeights[x, y2 + 1]) / LevelSmooth;

                    secondHeights[x, y2] = heights[x, y];

                    for (int i = 1; i < CheckLength; i++)
                    {
                        heights[x, y - i] = (PowerAverage(heights[x, y - i], heights[x, y - i + 1]) + Mathf.Abs(heights[x, y - i] - heights[x, y - i + 1]) / LevelSmooth) * (CheckLength - i) / CheckLength + heights[x, y - i] * i / CheckLength;
                        secondHeights[x, y2 + i] = (PowerAverage(secondHeights[x, y2 + i], secondHeights[x, y2 + i - 1]) + Mathf.Abs(secondHeights[x, y2 + i] - secondHeights[x, y2 + i - 1]) / LevelSmooth) * (CheckLength - i) / CheckLength +
                                                   secondHeights[x, y2 + i] * i / CheckLength;
                    }
                }
            }
            else
            {
                if (side == Side.Top)
                {
                    int y;
                    int x = heights.GetLength(0) - 1;

                    int x2 = 0;

                    for (y = 0; y < heights.GetLength(1); y++)
                    {
                        heights[x, y] = PowerAverage(heights[x, y], secondHeights[x2, y]);

                        if (smooth)
                            heights[x, y] += Mathf.Abs(heights[x - 1, y] - secondHeights[x2 + 1, y]) / LevelSmooth;


                        secondHeights[x2, y] = heights[x, y];

                        for (int i = 1; i < CheckLength; i++)
                        {
                            heights[x - i, y] = (PowerAverage(heights[x - i, y], heights[x - i + 1, y]) + Mathf.Abs(heights[x - i, y] - heights[x - i + 1, y]) / LevelSmooth) * (CheckLength - i) / CheckLength + heights[x - i, y] * i / CheckLength;
                            secondHeights[x2 + i, y] = (PowerAverage(secondHeights[x2 + i, y], secondHeights[x2 + i - 1, y]) + Mathf.Abs(secondHeights[x2 + i, y] - secondHeights[x2 + i - 1, y]) / LevelSmooth) * (CheckLength - i) / CheckLength +
                                                       secondHeights[x2 + i, y] * i / CheckLength;
                        }
                    }
                }
            }


            terrainData.SetHeights(0, 0, heights);
            terrain.terrainData = terrainData;

            secondData.SetHeights(0, 0, secondHeights);
            second.terrainData = secondData;

            terrain.Flush();
            second.Flush();
        }

        /// <summary>
        /// Stitchs the terrains corners.
        /// </summary>
        /// <param name="terrain11">Terrain11.</param>
        /// <param name="terrain21">Terrain21.</param>
        /// <param name="terrain12">Terrain12.</param>
        /// <param name="terrain22">Terrain22.</param>
        private void StitchTerrainsRepair(Terrain terrain11, Terrain terrain21, Terrain terrain12, Terrain terrain22)
        {
            int size = terrain11.terrainData.heightmapResolution - 1;
            int size0 = 0;
            List<float> heights = new()
            {
                terrain11.terrainData.GetHeights(size, size0, 1, 1)[0, 0],
                terrain21.terrainData.GetHeights(size0, size0, 1, 1)[0, 0],
                terrain12.terrainData.GetHeights(size, size, 1, 1)[0, 0],
                terrain22.terrainData.GetHeights(size0, size, 1, 1)[0, 0]
            };


            float[,] height = new float[1, 1];
            height[0, 0] = heights.Max();

            terrain11.terrainData.SetHeights(size, size0, height);
            terrain21.terrainData.SetHeights(size0, size0, height);
            terrain12.terrainData.SetHeights(size, size, height);
            terrain22.terrainData.SetHeights(size0, size, height);

            terrain11.Flush();
            terrain12.Flush();
            terrain21.Flush();
            terrain22.Flush();
        }
    }
}