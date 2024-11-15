using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MinimapMgr : MonoBehaviour
{
    public static MinimapMgr inst;
    public RectTransform minimapImage;
    public Vector2 worldSize;
    Dictionary<Entity, GameObject> mapIcons;
    Matrix4x4 worldToMapTransformationMatrix;
    Matrix4x4 mapToWorldTransformationMatrix;

    private void Awake()
    {
        inst = this;
        mapIcons = new Dictionary<Entity, GameObject>();
        InitWorldToMapTransformationMatrix();
        InitMapToWorldTransformationMatrix();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateMinimap();
        if(Input.GetKeyDown(KeyCode.P))
        {
            GetFrustumOceanIntersection();
        }
    }

    //Assumes world origin is center of the map
    void InitWorldToMapTransformationMatrix()
    {
        Vector2 minimapSize = minimapImage.rect.size;

        Vector2 scaleRatio = minimapSize / worldSize;

        worldToMapTransformationMatrix = Matrix4x4.TRS(Vector2.zero, Quaternion.identity, scaleRatio);
    }

    void InitMapToWorldTransformationMatrix()
    {
        Vector2 minimapSize = minimapImage.rect.size;

        Vector2 scaleRatio = worldSize / minimapSize;

        mapToWorldTransformationMatrix = Matrix4x4.TRS(Vector2.zero, Quaternion.identity, scaleRatio);
    }

    public void CreateMinimapIcon(Entity ent, GameObject minimapIcon)
    {
        var newIcon = Instantiate(minimapIcon);
        newIcon.GetComponent<Image>().color = ent.owner.playerColor;
        newIcon.transform.SetParent(minimapImage.transform, false);
        mapIcons.Add(ent, newIcon);
    }

    public void UpdateMinimap()
    {
        float iconScale = 1/minimapImage.transform.localScale.x;
        foreach(var icon in mapIcons)
        {
            Entity ent = icon.Key;
            var mapIcon = icon.Value;
            Vector2 mapPosition = worldToMapTransformationMatrix.MultiplyPoint3x4(new Vector2(ent.position.x, ent.position.z));

            RectTransform rt = mapIcon.GetComponent<RectTransform>();
            rt.anchoredPosition = mapPosition;
            rt.localRotation = Quaternion.Euler(new Vector3(0, 0, -ent.heading));
            rt.localScale = Vector3.one * iconScale;
        }
    }

    void GetFrustumOceanIntersection()
    {
        Ray bottomLeft = Camera.main.ViewportPointToRay(new Vector3(0, 0, 0));
        Ray topLeft = Camera.main.ViewportPointToRay(new Vector3(0, 1, 0));
        Ray topRight = Camera.main.ViewportPointToRay(new Vector3(1, 1, 0));
        Ray bottomRight = Camera.main.ViewportPointToRay(new Vector3(1, 0, 0));

        Debug.Log("bottom left: " + GetPointAtHeight(bottomLeft, 0));
        Debug.Log("bottom right: " + GetPointAtHeight(bottomRight, 0));
        Debug.Log("top left: " + GetPointAtHeight(topLeft, 0));
        Debug.Log("top right: " + GetPointAtHeight(topRight, 0));
    }

    Vector3 GetPointAtHeight(Ray ray, float height)
    {
        return ray.origin + (((ray.origin.y - height) / -ray.direction.y) * ray.direction);
    }

    public void CheckIfMapClicked(Vector2 mousePos)
    {
        Vector2 localPoint;
        if (RectTransformUtility.RectangleContainsScreenPoint(minimapImage, mousePos))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapImage, mousePos, null, out localPoint);
            Vector2 worldPos2D = mapToWorldTransformationMatrix.MultiplyPoint3x4(localPoint);
            CameraMgr.inst.YawNode.transform.position = new Vector3(worldPos2D.x, CameraMgr.inst.YawNode.transform.position.y, worldPos2D.y);
        }
    }
}
