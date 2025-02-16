using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap,float heightMultiplier,
     AnimationCurve meshHeightCurve,int levelOfDetail,float mapScale) {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        int vertexIndex = 0;
        float topLeftX = (width-1) /-2f;
        float topLeftZ = (height-1) /2f;

        int simplificationIncrement = (levelOfDetail==0) ? 1 : levelOfDetail*2;
        int verticiesPerLine = (width-1)/simplificationIncrement+1;

        MeshData meshData = new MeshData(verticiesPerLine,verticiesPerLine);

        for(int y = 0; y < height;y+=simplificationIncrement) {
            for(int x = 0; x<width;x+=simplificationIncrement) {
                meshData.verticies[vertexIndex] = new((topLeftX + x)*mapScale,(meshHeightCurve.Evaluate(heightMap[x,y])*heightMultiplier)*mapScale,(topLeftZ - y)*mapScale); 
                meshData.uvs[vertexIndex] = new(x/(float)width,y/(float)height);
                if(x<width-1 && y<height-1) {
                    meshData.AddTriangle(vertexIndex,vertexIndex+verticiesPerLine+1,vertexIndex+verticiesPerLine);
                    meshData.AddTriangle(vertexIndex+verticiesPerLine+1,vertexIndex,vertexIndex+1);
                }

                vertexIndex++;
            }
        }

        return meshData;
    }
}

public class MeshData {
    public Vector3[] verticies;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex =0;

    public MeshData(int meshWidth, int meshHeight) {
        verticies = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth-1) * (meshHeight-1)*6];
    }

    public void AddTriangle(int a, int b, int c) {
        triangles [triangleIndex] = a;
        triangles [triangleIndex+1] = b;
        triangles [triangleIndex+2] = c;
        triangleIndex+=3;
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }
}
