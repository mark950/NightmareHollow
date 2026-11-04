using UnityEngine;

// Egyszeri ijesztő-zóna: belépésre üzenet + opcionális Jacko-nevetés.
// Pl. a körhintánál: "A körhinta magától elindult... üresen."
public class TriggerScare : MonoBehaviour
{
    public string message;
    public bool jackoLaugh;
    public bool once = true;

    bool fired;

    void OnTriggerEnter(Collider c)
    {
        if (!c.CompareTag("Player")) return;
        if (once && fired) return;
        fired = true;

        if (GameManager.I != null)
        {
            if (!string.IsNullOrEmpty(message)) GameManager.I.Msg(message);
        }
        if (jackoLaugh)
        {
            JackoAI j = Object.FindFirstObjectByType<JackoAI>();
            if (j != null) j.PlayLaugh();
        }
    }
}
