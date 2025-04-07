using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FXMgr : MonoBehaviour
{
    public static FXMgr inst;
    private void Awake() {
        inst = this;
    }

    public ExplosionSmall explosionSmallPrefab;

    public void CreateExplosionAt(Vector3 pos, float interval = 1) {
        // Debug.Log("Exploding at: " + pos);
        ExplosionSmall et = Instantiate(explosionSmallPrefab, pos, Quaternion.identity, transform);
        StartCoroutine(ExplodeAndDestroy(et, interval));
    }

    IEnumerator ExplodeAndDestroy(ExplosionSmall et, float interval) {
        yield return new WaitForFixedUpdate();
        et.SetExplosionInterval(interval);
        et.Explode();
        yield return new WaitForSeconds(interval+1);
        Destroy(et.gameObject);
    }

    //Need smoke

}
