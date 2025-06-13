using UnityEngine;

public class BlinkingCapsule : MonoBehaviour
{
    float blinkInterval = 0.1f;  // seconds between blinks

    private Renderer capsuleRenderer;
    private float timer = 0f;

    void Start()
    {
        capsuleRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= blinkInterval)
        {
            capsuleRenderer.enabled = !capsuleRenderer.enabled;  // Toggle visibility
            timer = 0f;
        }
    }
}
