using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GreyOverlayGenerator : MonoBehaviour
{
    public Material grayOverlayMaterial; // Optional: Assign in Inspector
    private List<GameObject> overlays = new List<GameObject>();
    private List<Material> overlayMaterials = new List<Material>();
    private Entity entity;

    private bool isFading = false;

    void Awake()
    {
        entity = GetComponentInParent<Entity>();
        if (entity == null)
        {
            Debug.LogWarning("GreyOverlayGenerator: No Entity component found in parent.");
        }
    }

    public void ApplyGreyOverlay()
    {
        if (grayOverlayMaterial == null)
        {
            grayOverlayMaterial = CreateGreyOverlayMaterial();
        }

        overlays.Clear();
        overlayMaterials.Clear();

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            GameObject overlay = new GameObject("GreyOverlay");
            overlay.transform.SetParent(rend.transform, false);
            overlay.transform.localPosition = Vector3.zero;
            overlay.transform.localRotation = Quaternion.identity;
            overlay.transform.localScale = Vector3.one * 1.001f;

            Material instanceMat = new Material(grayOverlayMaterial); // Make it unique

            if (rend is MeshRenderer && rend.GetComponent<MeshFilter>() != null)
            {
                var mf = rend.GetComponent<MeshFilter>();
                overlay.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                overlay.AddComponent<MeshRenderer>().material = instanceMat;
            }
            else if (rend is SkinnedMeshRenderer smr)
            {
                var overlaySMR = overlay.AddComponent<SkinnedMeshRenderer>();
                overlaySMR.sharedMesh = smr.sharedMesh;
                overlaySMR.material = instanceMat;
                overlaySMR.rootBone = smr.rootBone;
                overlaySMR.bones = smr.bones;
            }

            overlays.Add(overlay);
            overlayMaterials.Add(instanceMat);
        }

        if (entity != null)
        {
            entity.isGreyed = true;
            StartCoroutine(FadeOutOverlay(entity.greyOverlayFadeDuration));
        }
        else
        {
            Debug.LogWarning("Entity not assigned — using default fade duration of 10s.");
            StartCoroutine(FadeOutOverlay(10f)); // Fallback
        }
    }

    public void RemoveGreyOverlay()
    {
        foreach (GameObject overlay in overlays)
        {
            if (overlay != null)
                Destroy(overlay);
        }
        overlays.Clear();
        overlayMaterials.Clear();

        if (entity != null)
        {
            entity.isGreyed = false;
        }
    }

    private Material CreateGreyOverlayMaterial()
    {
        var shader = Shader.Find("Custom/URP_TintOverlay");
        if (shader == null)
        {
            Debug.LogError("Custom/URP_TintOverlay shader not found.");
            return null;
        }

        Material mat = new Material(shader);
        mat.SetColor("_Color", new Color(0.4f, 0.4f, 0.4f, 0.6f)); // darker, semi-opaque
        return mat;
    }

    private IEnumerator FadeOutOverlay(float duration)
    {
        isFading = true;
        float elapsed = 0f;

        List<Color> startColors = new List<Color>();
        foreach (var mat in overlayMaterials)
        {
            startColors.Add(mat.GetColor("_Color"));
        }

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            for (int i = 0; i < overlayMaterials.Count; i++)
            {
                Color c = startColors[i];
                c.a = Mathf.Lerp(startColors[i].a, 0f, t);
                overlayMaterials[i].SetColor("_Color", c);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        RemoveGreyOverlay();
        isFading = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && !isFading)
        {
            ApplyGreyOverlay();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            StopAllCoroutines();
            RemoveGreyOverlay();
            isFading = false;
        }
    }
}
