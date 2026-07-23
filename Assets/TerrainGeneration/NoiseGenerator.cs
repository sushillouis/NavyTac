using UnityEngine;

public static class NoiseGenerator
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale
    ,int octaves, float persistance, float lacunarity,int seed, Vector2 offset) {
        float[,] noiseMap = new float[mapWidth,mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for(int i = 0;i<octaves;i++) {
            float offsetX = prng.Next(-100000,100000) + offset.x;
            float offsetY = prng.Next(-100000,100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        if(scale <=0f) {
            scale = 0.0001f;
        }

        float maxNoise = float.MinValue;
        float minNoise = float.MaxValue;

        float halfWidth = mapWidth/2;
        float halfHeight = mapHeight/2;
        
        for(int x = 0; x < mapWidth; x++) {
            for(int y = 0; y < mapHeight; y++) {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int i=0; i<octaves;i++) {
                    float sampleX = (x-halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y-halfHeight) / scale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX,sampleY) *2 - 1;

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if(noiseHeight > maxNoise) {
                    maxNoise = noiseHeight;
                } else if(noiseHeight < minNoise) {
                    minNoise = noiseHeight;
                }

                noiseMap[x,y] = noiseHeight;
            }
        }

        for(int x = 0; x < mapWidth; x++) {
            for(int y = 0; y < mapHeight; y++) {
                noiseMap[x,y] = Mathf.InverseLerp(minNoise,maxNoise,noiseMap[x,y]);
            }
        }


        return noiseMap;
    }
}
