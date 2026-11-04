using UnityEngine;
using System.Collections;

// A kijárati kapu – 3 biztosíték után nyílik
public class EscapeGate : MonoBehaviour
{
    public Transform door;
    public Light openSignLight;
    public bool isOpen;

    public void TryEscape()
    {
        if (isOpen)
        {
            if (GameManager.I != null) GameManager.I.Win();
        }
        else if (GameManager.I != null)
        {
            GameManager.I.Msg("A kapu zárva van - kell 3 biztosíték! (" + GameManager.I.Fuses + "/3)");
        }
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        if (openSignLight != null) openSignLight.gameObject.SetActive(true);
        if (GameManager.I != null) GameManager.I.Msg("A kapu nyitva! Menekülj ki! (E a kapunál)");
        if (door != null) StartCoroutine(OpenCo());
    }

    // ajtó felpattan, majd eltűnik a boltív mögött
    IEnumerator OpenCo()
    {
        Vector3 start = door.position;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 1.1f)
        {
            door.position = start + Vector3.up * (4.55f * t);
            yield return null;
        }
        door.position = start + Vector3.up * 4.55f;
        door.gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider c)
    {
        if (isOpen && c.CompareTag("Player") && GameManager.I != null)
            GameManager.I.Win();
    }
}
