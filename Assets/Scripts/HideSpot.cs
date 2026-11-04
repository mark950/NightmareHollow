using UnityEngine;

// Bújóhely (pl. hordó) – ide rejtozik a játékos
public class HideSpot : MonoBehaviour
{
    public int id;
    public bool IsPlayerInside;

    [Tooltip("Ide teleportál a kamera bújás közben (a néző rés).")]
    public Transform enterPoint;
}
