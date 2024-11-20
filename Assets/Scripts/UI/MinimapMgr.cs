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
    [Range(1f, 4f)]
    public float minimapZoom;
    public Vector2 mapOffset;

    private void Awake()
    {
        inst = this;
        minimapZoom = 1;
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
    }

    //Assumes world origin is center of the map
    void InitWorldToMapTransformationMatrix()
    {
        Vector2 minimapSize = minimapImage.rect.size;

        Vector2 scaleRatio = minimapSize / (worldSize / minimapZoom);

        worldToMapTransformationMatrix = Matrix4x4.TRS(mapOffset, Quaternion.identity, scaleRatio);
    }

    void InitMapToWorldTransformationMatrix()
    {
        Vector2 minimapSize = minimapImage.rect.size;

        Vector2 scaleRatio = (worldSize / minimapZoom) / minimapSize;
        //Vector2 translationRatio = -mapOffset *(worldSize.x/(minimapZoom*minimapSize.x));
        Vector2 translationVector = -mapOffset * scaleRatio;

        mapToWorldTransformationMatrix = Matrix4x4.TRS(translationVector, Quaternion.identity, scaleRatio);
    }

    public void CreateMinimapIcon(Entity ent, GameObject minimapIcon)
    {
        var newIcon = Instantiate(minimapIcon);
        newIcon.name = ent.name + "Icon";
        newIcon.GetComponent<Image>().color = ent.owner.playerColor;
        newIcon.transform.SetParent(minimapImage.transform, false);
        mapIcons.Add(ent, newIcon);
    }

    public void UpdateMinimap()
    {
        float iconScale = 1 / minimapImage.transform.localScale.x;
        foreach (var icon in mapIcons)
        {
            Entity ent = icon.Key;
            var mapIcon = icon.Value;
            Vector2 mapPosition = worldToMapTransformationMatrix.MultiplyPoint3x4(new Vector2(ent.position.x, ent.position.z));

            RectTransform rt = mapIcon.GetComponent<RectTransform>();
            rt.anchoredPosition = mapPosition;
            rt.localRotation = Quaternion.Euler(new Vector3(0, 0, -ent.heading));
            rt.localScale = Vector3.one * iconScale;

            if (Mathf.Abs(rt.localPosition.x) > minimapImage.rect.width / 2 ||
                Mathf.Abs(rt.localPosition.y) > minimapImage.rect.height / 2)
                mapIcon.SetActive(false);
            else
                mapIcon.SetActive(true);
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

    public bool CursorOverMap(Vector2 mousePos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(minimapImage, mousePos);
    }

    public void MoveCameraViaMinimap(Vector2 mousePos)
    {
        Vector2 localPoint;
        if (CursorOverMap(mousePos))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapImage, mousePos, null, out localPoint);
            Vector2 worldPos2D = mapToWorldTransformationMatrix.MultiplyPoint3x4(localPoint);
            CameraMgr.inst.YawNode.transform.position = new Vector3(worldPos2D.x, CameraMgr.inst.YawNode.transform.position.y, worldPos2D.y);
        }
    }

    public void ChangeZoom(float delta)
    {
        minimapZoom -= delta * 0.1f;
        minimapZoom = Mathf.Clamp(minimapZoom, 1, 4);
        InitMapToWorldTransformationMatrix();
        InitWorldToMapTransformationMatrix();
    }

    public void ChangeCenter(Vector2 delta)
    {
        mapOffset.x = Mathf.Clamp(mapOffset.x + delta.x,
            -minimapZoom * (minimapImage.rect.width / 2 - (minimapImage.rect.width / (2 * minimapZoom))),
            minimapZoom * (minimapImage.rect.width / 2 - (minimapImage.rect.width / (2 * minimapZoom))));

        mapOffset.y = Mathf.Clamp(mapOffset.y + delta.y,
            -minimapZoom * (minimapImage.rect.height / 2 - (minimapImage.rect.height / (2 * minimapZoom))),
            minimapZoom * (minimapImage.rect.height / 2 - (minimapImage.rect.height / (2 * minimapZoom))));

        InitMapToWorldTransformationMatrix();
        InitWorldToMapTransformationMatrix();
    }
}