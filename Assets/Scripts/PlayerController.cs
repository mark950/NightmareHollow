using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Sebesség")]
    public float walkSpeed = 3.8f;
    public float sprintSpeed = 6.4f;
    public float crouchSpeed = 1.8f;
    public float mouseSens = 2.2f;

    [Header("Hivatkozások (a jelenetépítő tölti)")]
    public Transform camPivot;
    public Light flashlight;

    [HideInInspector] public bool Hidden;
    [HideInInspector] public string Hint = "";
    [HideInInspector] public bool HasFlashlight;   // a lámpát meg kell találni!

    [HideInInspector] public HideSpot currentSpot;
    CharacterController cc;
    float pitch;
    float vertVel;
    bool crouching;
    AudioSource steps;
    float stepTimer;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // A lámpa NÉLKÜL indulunk – előbb meg kell találni!
        if (flashlight != null) flashlight.enabled = false;

        // lépések
        steps = gameObject.AddComponent<AudioSource>();
        steps.playOnAwake = false;
        steps.spatialBlend = 0f;
        steps.clip = SynthStep();
    }

    // Egy lépés: tompa "kopogás" = zajcsattanás + mély puffanás
    AudioClip SynthStep()
    {
        const int rate = 22050;
        const float dur = 0.14f;
        int len = Mathf.RoundToInt(rate * dur);
        float[] data = new float[len];
        System.Random rng = new System.Random(3);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-30f * t / dur) * 0.5f;
            float thud = Mathf.Sin(2f * Mathf.PI * 78f * t) * Mathf.Exp(-18f * t / dur) * 0.9f;
            data[i] = Mathf.Clamp(noise + thud, -1f, 1f) * 0.7f;
        }
        AudioClip clip = AudioClip.Create("lepes", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public bool FlashOn
    {
        get { return flashlight != null && flashlight.enabled; }
    }

    public bool IsSprinting
    {
        get
        {
            return Input.GetKey(KeyCode.LeftShift) && !crouching && !Hidden &&
                   (Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f ||
                    Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f);
        }
    }

    void Update()
    {
        if (Hidden)
        {
            Hint = "[E] - Kiszállás";
            if (Input.GetKeyDown(KeyCode.E)) Unhide();
            return;
        }

        Look();
        Move();
        Gravity();
        Height();

        if (Input.GetKeyDown(KeyCode.F) && HasFlashlight && flashlight != null)
            flashlight.enabled = !flashlight.enabled;

        InteractScan();
    }

    void Look()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSens;
        float my = Input.GetAxis("Mouse Y") * mouseSens;
        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -70f, 82f);
        if (camPivot != null)
            camPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        float spd = crouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
        Vector3 move = transform.right * Input.GetAxisRaw("Horizontal")
                     + transform.forward * Input.GetAxisRaw("Vertical");
        if (move.sqrMagnitude > 1f) move.Normalize();
        cc.Move(move * spd * Time.deltaTime);

        // lépés-hangok: ütem a sebességtől függ
        bool moving = move.sqrMagnitude > 0.01f && cc.isGrounded;
        if (moving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = crouching ? 0.8f : (IsSprinting ? 0.36f : 0.52f);
                if (steps != null && steps.clip != null)
                {
                    steps.pitch = Random.Range(0.9f, 1.15f);
                    steps.volume = crouching ? 0.12f : (IsSprinting ? 0.5f : 0.38f);
                    steps.PlayOneShot(steps.clip);
                }
            }
        }
        else stepTimer = 0.12f; // friss lépés induljon induláskor
    }

    void Gravity()
    {
        if (cc.isGrounded && vertVel < 0f) vertVel = -0.4f;
        vertVel += Physics.gravity.y * Time.deltaTime;
        cc.Move(Vector3.up * vertVel * Time.deltaTime);
    }

    void Height()
    {
        if (Input.GetKeyDown(KeyCode.C)) crouching = !crouching;
        float target = crouching ? 1.1f : 1.8f;
        cc.height = Mathf.MoveTowards(cc.height, target, Time.deltaTime * 5f);
        cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
        if (camPivot != null)
            camPivot.localPosition = new Vector3(0f, cc.height - 0.25f, 0f);
    }

    void InteractScan()
    {
        Hint = "";
        bool pressed = Input.GetKeyDown(KeyCode.E);
        Vector3 origin = transform.position + Vector3.up * 0.9f;
        Collider[] hits = Physics.OverlapSphere(origin + transform.forward * 1.1f, 1.05f);

        foreach (Collider h in hits)
        {
            FlashlightPickup lamp = h.GetComponentInParent<FlashlightPickup>();
            if (lamp != null)
            {
                Hint = "[E] - Lámpás felvétele";
                if (pressed) lamp.Pickup();
                return;
            }
            HideSpot spot = h.GetComponentInParent<HideSpot>();
            if (spot != null)
            {
                Hint = "[E] - Bújkálás a hordóba";
                if (pressed) Hide(spot);
                return;
            }
            FusePickup fuse = h.GetComponentInParent<FusePickup>();
            if (fuse != null)
            {
                Hint = "[E] - Biztosíték felvétele";
                if (pressed) fuse.Pickup();
                return;
            }
            EscapeGate gate = h.GetComponentInParent<EscapeGate>();
            if (gate != null)
            {
                Hint = gate.isOpen ? "[E] - SZÖKÉS!" : "[E] - Kapu (zárva)";
                if (pressed) gate.TryEscape();
                return;
            }
        }
    }

    void Hide(HideSpot spot)
    {
        currentSpot = spot;
        spot.IsPlayerInside = true;
        Hidden = true;
        cc.enabled = false;
        if (spot.enterPoint != null)
        {
            transform.position = spot.enterPoint.position;
            transform.rotation = spot.enterPoint.rotation;
        }
        if (GameManager.I != null)
            GameManager.I.Msg("Elbújtál... de a Jacko néha megtalál!");
    }

    public void Unhide()
    {
        if (currentSpot != null) currentSpot.IsPlayerInside = false;
        currentSpot = null;
        Hidden = false;
        cc.enabled = true;
        transform.position += transform.forward * 1.4f + Vector3.up * 0.1f;
    }

    // A Jacko kiránthat a bújóból
    public void DraggedOut()
    {
        Unhide();
    }
}
