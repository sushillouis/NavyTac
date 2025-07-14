using UnityEngine;

public class TestFogReveal : MonoBehaviour
{
    public FogOfWarMesh fogOfWar;
    public float moveSpeed = 500f;

    void Update()
    {
        // Move with arrow keys or WASD
        float moveX = Input.GetAxisRaw("Horizontal") * moveSpeed * Time.deltaTime;
        float moveZ = Input.GetAxisRaw("Vertical") * moveSpeed * Time.deltaTime;
        transform.position += new Vector3(moveX, 0, moveZ);

        // Reveal fog around this object
        fogOfWar.RevealArea(transform.position, fogOfWar.revealRadius);
    }
}