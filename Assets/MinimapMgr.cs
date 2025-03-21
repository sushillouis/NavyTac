using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapMgr : MonoBehaviour
{
    public static MinimapMgr inst;

    [Header("Map Parameters")]
    public RectTransform minimapImage;
    public Vector2 worldSize;

    [Header("Parameters for Size Toggling")]
    public RectTransform radar;
    public RectTransform mainCanvas;
    public RectTransform rootPanel;
    public RectTransform minimapPanel;

    [Header("Parameters for Minimap Icons")]
    public GameObject cameraIconPrefab;
    GameObject cameraIcon;
    Dictionary<Entity, GameObject> mapIcons;

    //conversion matrices
    Matrix4x4 worldToMapTransformationMatrix;
    Matrix4x4 mapToWorldTransformationMatrix;

    //varaibles for zoom and move
    float minimapZoom;
    Vector2 mapOffset;

    private void Awake() {
        inst = this;
        minimapZoom = 1;
        cameraIcon = Instantiate(cameraIconPrefab);
        cameraIcon.name = "CameraIcon";
        cameraIcon.transform.SetParent(minimapImage.transform, false);
        mapIcons = new Dictionary<Entity, GameObject>();
        InitWorldToMapTransformationMatrix();
        InitMapToWorldTransformationMatrix();
    }

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        UpdateMinimap();
    }

    //Set the matrix that translates world position to map position
    void InitWorldToMapTransformationMatrix() {
        Vector2 minimapSize = minimapImage.rect.size;

        Vector2 scaleRatio = minimapSize / (worldSize / minimapZoom);

        worldToMapTransformationMatrix = Matrix4x4.TRS(mapOffset, Quaternion.identity, scaleRatio);
    }

    //Set the matrix that translates map position to world position
    void InitMapToWorldTransformationMatrix() {
        Vector2 minimapSize = minimapImage.rect.size;

        Vector2 scaleRatio = (worldSize / minimapZoom) / minimapSize;
        Vector2 translationVector = -mapOffset * scaleRatio;

        mapToWorldTransformationMatrix = Matrix4x4.TRS(translationVector, Quaternion.identity, scaleRatio);
    }

    //Creates the minimap icon for each ent in the scene, called in UIAspect
    public void CreateMinimapIcon(Entity ent, GameObject minimapIcon) {
        var newIcon = Instantiate(minimapIcon);
        newIcon.name = ent.name + "Icon";
        newIcon.GetComponent<Image>().color = ent.owner.playerColor;
        newIcon.transform.SetParent(minimapImage.transform, false);
        mapIcons.Add(ent, newIcon);
    }

    //Uses SetIconLocation to update the map position for all ents and the camera
    public void UpdateMinimap() {
        foreach(var icon in mapIcons) {
            Entity ent = icon.Key;
            var mapIcon = icon.Value;
            SetIconLocation(mapIcon, ent.position, ent.heading);
        }
        SetIconLocation(cameraIcon, Camera.main.transform.position, 0);

    }

    //Sets the postion of a map icon given a world position and heading
    public void SetIconLocation(GameObject mapIcon, Vector3 worldPosition, float heading) {
        //Sets the scale of the icon proportional to the minimap
        float iconScale = 1 / minimapImage.transform.localScale.x;

        //Converts world position to map position
        Vector2 mapPosition = worldToMapTransformationMatrix.MultiplyPoint3x4(new Vector2(worldPosition.x, worldPosition.z));

        //Sets icon position, rotation, and scale
        RectTransform rt = mapIcon.GetComponent<RectTransform>();
        rt.anchoredPosition = mapPosition;
        rt.localRotation = Quaternion.Euler(new Vector3(0, 0, -heading));
        rt.localScale = Vector3.one * iconScale;

        //hides icon if it's off the map
        if(Mathf.Abs(rt.localPosition.x) > minimapImage.rect.width / 2 ||
            Mathf.Abs(rt.localPosition.y) > minimapImage.rect.height / 2)
            mapIcon.SetActive(false);
        else
            mapIcon.SetActive(true);
    }

    /*
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
    */

    //Checks if the mouse is over the minimap
    public bool CursorOverMap(Vector2 mousePos) {
        return RectTransformUtility.RectangleContainsScreenPoint(minimapImage, mousePos);
    }

    //Moves the camera to the spot clicked on the minimap
    public void MoveCameraViaMinimap(Vector2 mousePos) {
        Vector2 localPoint;
        if(CursorOverMap(mousePos)) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapImage, mousePos, null, out localPoint);
            Vector2 worldPos2D = mapToWorldTransformationMatrix.MultiplyPoint3x4(localPoint);
            CameraMgr.inst.YawNode.transform.position = new Vector3(worldPos2D.x, CameraMgr.inst.YawNode.transform.position.y, worldPos2D.y);
            CameraMgr.inst.onCameraMove.Invoke();
        }
    }

    //Zooms in the minimap
    public void ChangeZoom(float delta) {
        minimapZoom -= delta * 0.1f;
        minimapZoom = Mathf.Clamp(minimapZoom, 1, 4);
        InitMapToWorldTransformationMatrix();
        InitWorldToMapTransformationMatrix();
    }

    //Moves the minimap
    public void ChangeCenter(Vector2 delta) {
        mapOffset.x = Mathf.Clamp(mapOffset.x + delta.x,
            -minimapZoom * (minimapImage.rect.width / 2 - (minimapImage.rect.width / (2 * minimapZoom))),
            minimapZoom * (minimapImage.rect.width / 2 - (minimapImage.rect.width / (2 * minimapZoom))));

        mapOffset.y = Mathf.Clamp(mapOffset.y + delta.y,
            -minimapZoom * (minimapImage.rect.height / 2 - (minimapImage.rect.height / (2 * minimapZoom))),
            minimapZoom * (minimapImage.rect.height / 2 - (minimapImage.rect.height / (2 * minimapZoom))));

        InitMapToWorldTransformationMatrix();
        InitWorldToMapTransformationMatrix();
    }

    //Toggles whether map is big on the screen
    bool mapIsBig;
    public void ResizeMap() {
        if(mapIsBig) {
            minimapImage.SetParent(minimapPanel);
            rootPanel.gameObject.SetActive(false);
            minimapImage.anchorMax = new Vector2(0.45f, 0.45f);
            minimapImage.anchorMin = new Vector2(0.45f, 0.45f);
            minimapImage.anchoredPosition = Vector2.zero;
            Vector2 oldSize = minimapImage.sizeDelta;
            minimapImage.sizeDelta = new Vector2(200, 200);
            radar.sizeDelta *= (minimapImage.sizeDelta / oldSize);
        } else {
            minimapImage.SetParent(rootPanel);
            rootPanel.gameObject.SetActive(true);
            minimapImage.anchorMax = new Vector2(0.5f, 0.5f);
            minimapImage.anchorMin = new Vector2(0.5f, 0.5f);
            minimapImage.anchoredPosition = Vector2.zero;
            Vector2 oldSize = minimapImage.sizeDelta;
            float size = Mathf.Min(mainCanvas.rect.width, mainCanvas.rect.height);
            minimapImage.sizeDelta = new Vector2(size, size);
            radar.sizeDelta *= (minimapImage.sizeDelta / oldSize);
        }

        mapIsBig = !mapIsBig;

        InitMapToWorldTransformationMatrix();
        InitWorldToMapTransformationMatrix();
    }
}
