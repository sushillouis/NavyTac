using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetDiffVis : MonoBehaviour
{

    [SerializeField]
    private Image posDiff;
    [SerializeField]
    private Image rotDiff;

    public void SetPosRot(Vector3 diffPos, float hDiff) {
        posDiff.fillAmount = diffPos.magnitude / 1.0f;
        rotDiff.fillAmount = hDiff / 2f;
    }

}
