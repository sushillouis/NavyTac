using System.Collections;
using UnityEngine;

public class FXMgr : MonoBehaviour
{
    public static FXMgr inst;
    private void Awake()
    {
        inst = this;
    }

    public GameObject explosionLargePrefab; // Your large explosion prefab

    public void CreateExplosionAt(Vector3 pos, float interval = 1f, float scaleMultiplier = 1f)
    {
        // Spawn explosion
        GameObject explosion = Instantiate(explosionLargePrefab, pos, Quaternion.identity, transform);
        // explosion.transform.localScale *= scaleMultiplier; // Apply scale multiplier

        // Start coroutine to manage timing
        StartCoroutine(ExplodeAndDestroy(explosion, interval));
    }

    IEnumerator ExplodeAndDestroy(GameObject explosion, float interval)
    {
        yield return new WaitForFixedUpdate();
        // Explosion plays automatically via ExplosionEffect.cs
        explosion.GetComponent<ExplosionSmall>().Explode();
        yield return new WaitForSeconds(interval + 1f);
        // Destroy explosion (if not already destroyed by ExplosionEffect.cs)
        if (explosion != null) Destroy(explosion);
    }

    public void ResetEffects()
    {
        StopAllCoroutines(); // Stop any pending coroutines
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject); // Destroy all active effects
        }
    }
}