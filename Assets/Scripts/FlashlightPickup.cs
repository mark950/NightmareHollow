using UnityEngine;

// Zseblámpa felvehető tárgy – a játékos lámpa nélkül indul!
// A Security Office íróasztalán találja meg (a tábla: "Find the flashlight").
public class FlashlightPickup : MonoBehaviour
{
    bool taken;

    public void Pickup()
    {
        if (taken) return;
        taken = true;
        PlayerController p = Object.FindFirstObjectByType<PlayerController>();
        if (p != null)
        {
            p.HasFlashlight = true;
            if (p.flashlight != null) p.flashlight.enabled = true;
        }
        if (GameManager.I != null) GameManager.I.FlashlightFound();
        Destroy(gameObject);
    }
}
