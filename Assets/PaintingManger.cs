using System;
using System.Linq.Expressions;
using UnityEngine;

public class PaintingManger : MonoBehaviour
{
    public static PaintingManger inst;
    float[,] currentMap;
    [SerializeField] Terrain terrain;
    [SerializeField] MapGenerator mapGenerator;
    [SerializeField] Transform terrainContainer;
    [SerializeField] GameObject paintingPlane;
    public int brushSize;
    Vector3 mapCordsOrigin = new(0,0,0);
    Vector3 flat = new(1,0,1);

    public void SetCurrentMap(float[,] newMap)
    {
        currentMap=newMap;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetCurrentMap(new float[10,10]);
        mapCordsOrigin = terrain.transform.localPosition+terrainContainer.position;
        print(mapCordsOrigin);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.Space)) 
        {
            int size = currentMap.GetLength(0);
            for(int y=0; y<size;y++) 
            {
                for(int x=0; x<size;x++) 
                {
                    currentMap[x,y] += 1f* Time.deltaTime;
                }
            }
        }

        if(Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            LayerMask layers = (1<<6);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 40000, layers))
            {   //Only Works with square maps atm
                
                float worldSpaceScale = terrain.terrainData.heightmapResolution/terrain.terrainData.size.x;
                
                
                Debug.Log(worldSpaceScale);
                Vector3 flatDelta = Vector3.Scale(hit.point,flat) - Vector3.Scale(mapCordsOrigin,flat);
                float [,] paintMap = new float[brushSize,brushSize];
                // float terrainHeight = terrain.terrainData.GetHeight((int)(hit.point.x*worldSpaceScale),(int)(hit.point.z*worldSpaceScale));
                
                // print(terrainHeight);
                // print(hit.point.x-mapCordsOrigin.x);
                
                // Vector2Int topRight = new((int)(flatDelta.x/worldSpaceScale)+brushSize,MapGenerator.mapChunkSize-(((int)(flatDelta.z/worldSpaceScale))-brushSize));
                // Vector2Int bottomLeft = new((int)(flatDelta.x/worldSpaceScale)-brushSize,MapGenerator.mapChunkSize-(((int)(flatDelta.z/worldSpaceScale))+brushSize));
                Vector2Int topLeft = 
                    new((int)(flatDelta.x*worldSpaceScale)-brushSize/2
                    ,(int)(flatDelta.z*worldSpaceScale)-brushSize/2);                
                // paintMap = terrain.terrainData.GetHeights((int)hit.point.x,(int)hit.point.y,(brushSize*2),(brushSize*2));
                Vector2Int center = 
                    new((int)(flatDelta.x*worldSpaceScale)
                    ,(int)(flatDelta.z*worldSpaceScale));                
                
                paintMap =  terrain.terrainData.GetHeights(topLeft.x,topLeft.y,brushSize,brushSize);
                
                paintingPlane.transform.position = 
                    new(hit.point.x,0,hit.point.z);

                // topLeft.x = Mathf.Clamp(topLeft.x,brushSize,(int)worldSpaceScale-brushSize);
                // topLeft.y = Mathf.Clamp(topLeft.y,brushSize,(int)worldSpaceScale-brushSize);

                terrain.terrainData.SetHeightsDelayLOD(topLeft.x,topLeft.y,paintMap);

                // terrain.terrainData.SetHeightsDelayLOD(9480,9480,funny);
                
                // Debug.Log(topRight);
                // Debug.Log(bottomLeft);
                // Debug.Log(size);
                // for(int y=bottomLeft.y; y<topRight.y;y++) 
                // {
                //     i=0;
                //     for(int x=bottomLeft.x; x<topRight.x;x++) 
                //     {
                //         if(x>=0 && y>=0 && x<size && y<=size) {
                //             paintMap[i,j] = currentMap[x,y];
                //         } else {
                //             paintMap[i,j]=0f;
                //         }
                //         i++;
                //     }
                //     j++;
                // }
                // Debug.Log(paintMap);
                mapGenerator.GeneratePaintMapFromArray(paintMap, paintingPlane.GetComponent<MeshFilter>());
            }
        }
        if(Input.GetMouseButtonUp(0)) {
            terrain.terrainData.SyncHeightmap();
        }
    }

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        inst=this;
    }
}
