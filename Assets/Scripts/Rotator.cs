using UnityEngine;

// Nyolc dollár csak alapvető forgás (pl. óriáskerék)
public class Rotator : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float speed = 6f;

    void Update()
    {
        transform.Rotate(axis * speed * Time.deltaTime, Space.Self);
    }
}
