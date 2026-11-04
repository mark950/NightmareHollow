using UnityEngine;

// Biztosíték – a kijutáshoz kell (3 db)
public class FusePickup : MonoBehaviour
{
    bool taken;

    public void Pickup()
    {
        if (taken) return;
        taken = true;
        if (GameManager.I != null) GameManager.I.AddFuse();
        Destroy(gameObject);
    }
}
