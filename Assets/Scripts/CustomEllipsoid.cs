using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

public class CustomEllipsoid : MonoBehaviour{
    
    public CesiumEllipsoid customEllipsoid;
    public CesiumGeoreference cesiumGeoreference;

    /// <summary>
    /// Called when the script is loaded or a value is changed in the
    /// inspector (Called in the editor only).
    /// </summary>
    void Start()
    {
        customEllipsoid  = ScriptableObject.CreateInstance<CesiumEllipsoid>();
        customEllipsoid.SetRadii(new double3(3e7,3e7,3e7));
        cesiumGeoreference.ellipsoid = customEllipsoid;
    }

    // void OnValidate() {
    //     CesiumEllipsoid ce = cesiumGeoreference.ellipsoid;
    //     ce.SetRadii(new double3(7e6,7e6,8e6));
    // }
}