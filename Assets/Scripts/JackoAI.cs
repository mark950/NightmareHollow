using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// JACKO – a Töklámpás Bohóc
// Viselkedés: lurkol (figyel) → észrevesz → ólálkodik → üldöz → keres
// A búvóhelyeire EMLÉKSZIK. Üldözése mostantól visszafogottabb.
public class JackoAI : MonoBehaviour
{
    public enum State { Lurk, Stare, Stalk, Chase, Search, Jump }
    public State state = State.Lurk;

    [Header("Érzékelés")]
    public float sightRange = 20f;
    public float flashSightRange = 32f;
    public float fovDeg = 150f;

    [Header("Sebességek (visszafogva)")]
    public float lurkSpeed = 1.5f;
    public float stalkSpeed = 2.0f;
    public float chaseSpeed = 4.4f;

    [Header("Hangolás")]
    public float stareTime = 2.6f;
    public float stalkTime = 6.0f;
    public float loseSightTime = 3.2f;
    public float catchDist = 1.6f;
    public List<Vector3> patrolPoints = new List<Vector3>();

    [Header("Harag-szint (megjegyzett)")]
    public int maxRage = 5;
    int anger;

    // Sebesség- és fény-szorozó a haragtól függően
    public int Anger { get { return Mathf.Min(anger, maxRage); } }
    public int MaxRage { get { return maxRage; } }
    float RageMul { get { return 1f + Mathf.Min(anger, maxRage) * 0.03f; } }

    // ---- belső ----
    PlayerController player;
    GameManager gm;
    Transform body, headNode, armL, armR, eyeL, eyeR, jaw;
    Vector3 eyeBaseL, eyeBaseR;
    Light eyeLight;
    Material glowMat;
    Color glowBase;
    AudioSource voice;

    Vector3 lastSeen;
    float loseTimer, stateTimer;
    int wayIdx;
    readonly HashSet<int> remembered = new HashSet<int>();
    bool curBlind, jumping;

    Dictionary<int, HideSpot> spotMap;
    List<int> searchPlan;
    int planIdx;
    float waitTimer;

    void Start()
    {
        BuildBody();
        player = Object.FindFirstObjectByType<PlayerController>();
        gm = GameManager.I;

        // Harag betöltése – "mindig visszajövök... és emlékszem."
        anger = PlayerPrefs.GetInt("NH_Harag", 0);
        glowBase = new Color(1f, 0.42f, 0.05f);
        ApplyRageVisuals();

        // Jacko hangja (nevetés) – térben hallható
        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        voice.spatialBlend = 1f;
        voice.minDistance = 4f;
        voice.maxDistance = 34f;
        voice.rolloffMode = AudioRolloffMode.Linear;
        voice.clip = SynthLaugh();
    }

    void RegisterRage()
    {
        if (anger < maxRage)
        {
            anger++;
            PlayerPrefs.SetInt("NH_Harag", anger);
            PlayerPrefs.Save();
            ApplyRageVisuals();
        }
    }

    void ApplyRageVisuals()
    {
        float r = Mathf.Min(anger, maxRage);
        if (glowMat != null && glowMat.HasProperty("_EmissionColor"))
            glowMat.SetColor("_EmissionColor", glowBase * (1.1f + r * 0.14f));
        if (eyeLight != null)
            eyeLight.range = 7f + r * 0.6f;
    }

    void Update()
    {
        if (!player || jumping) return;

        curBlind = IsBlinded();
        RememberHideouts();

        switch (state)
        {
            case State.Lurk: DoLurk(); break;
            case State.Stare: DoStare(); break;
            case State.Stalk: DoStalk(); break;
            case State.Chase: DoChase(); break;
            case State.Search: DoSearch(); break;
        }
        UpdateLimbs();
    }

    // ================= ÁLLAPOTOK =================

    void DoLurk()
    {
        if (CanSee())
        {
            SetState(State.Stalk);
            lastSeen = player.transform.position;
            if (gm != null) gm.Msg("Valami figyel téged...");
            return;
        }
        if (patrolPoints.Count == 0) return;
        Vector3 t = patrolPoints[wayIdx % patrolPoints.Count];
        Move(t, lurkSpeed);
        if (Vector3.Distance(transform.position, t) < 1.5f)
        {
            wayIdx++;
            if (Random.value < 0.5f &&
                Vector3.Distance(transform.position, player.transform.position) < 35f)
            {
                SetState(State.Stare);
            }
        }
    }

    void DoStare()
    {
        stateTimer += Time.deltaTime;
        Vector3 to = player.transform.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(to), 60f * Time.deltaTime);
        if (stateTimer > stareTime) SetState(State.Lurk);
        if (Vector3.Distance(transform.position, player.transform.position) < 9f)
            SetState(State.Stalk);
    }

    void DoStalk()
    {
        stateTimer += Time.deltaTime;
        if (CanSee())
        {
            lastSeen = player.transform.position;
            loseTimer = 0f;
        }
        else
        {
            loseTimer += Time.deltaTime;
        }
        if (player.Hidden) { BeginSearch(); return; }
        if (loseTimer > loseSightTime) { BeginSearch(); return; }

        Move(player.transform.position, stalkSpeed * RageMul);
        float d = Vector3.Distance(transform.position, player.transform.position);
        if (d < 7.5f || stateTimer > stalkTime)
        {
            SetState(State.Chase);
            RegisterRage();
            if (gm != null)
            {
                int a = Mathf.Min(anger, maxRage);
                string rage = a > 0 ? "  [harag " + a + "/" + maxRage + "]" : "";
                gm.Msg("ÉSZREVETT! FUSS!" + rage);
            }
            PlayLaugh();
        }
    }

    void DoChase()
    {
        if (CanSee())
        {
            lastSeen = player.transform.position;
            loseTimer = 0f;
        }
        else
        {
            loseTimer += Time.deltaTime;
        }

        if (!player.Hidden && Vector3.Distance(transform.position, player.transform.position) < catchDist)
        {
            StartJump();
            return;
        }

        if (loseTimer > loseSightTime ||
            (player.Hidden && Vector3.Distance(transform.position, lastSeen) < 2.2f))
        {
            BeginSearch();
            return;
        }

        Vector3 target = player.Hidden ? lastSeen : player.transform.position;
        Move(target, chaseSpeed * RageMul);
    }

    void DoSearch()
    {
        if (CanSee()) { SetState(State.Chase); loseTimer = 0f; RegisterRage(); PlayLaugh(); return; }
        if (searchPlan == null || searchPlan.Count == 0) { SetState(State.Lurk); return; }

        if (waitTimer > 0f) { waitTimer -= Time.deltaTime; return; }

        if (planIdx >= Mathf.Min(searchPlan.Count, 5)) { SetState(State.Lurk); return; }

        int spotId = searchPlan[planIdx];
        HideSpot target = null;
        if (spotMap != null) spotMap.TryGetValue(spotId, out target);
        if (target == null) { planIdx++; return; }

        Move(target.transform.position, chaseSpeed * 0.5f * RageMul);

        if (Vector3.Distance(transform.position, target.transform.position) < 1.6f)
        {
            remembered.Add(target.id);
            if (target.IsPlayerInside && player.currentSpot == target)
            {
                player.DraggedOut();
                SetState(State.Chase);
                StartJump();
                return;
            }
            planIdx++;
            waitTimer = 1.0f;
        }
    }

    void SetState(State s)
    {
        state = s;
        stateTimer = 0f;
        loseTimer = 0f;
    }

    void BeginSearch()
    {
        SetState(State.Search);
        waitTimer = 0.7f;
        if (gm != null) gm.Msg("A Jacko keres téged... nyugi, hol bújsz!");
        spotMap = new Dictionary<int, HideSpot>();
        HideSpot[] all = Object.FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
        foreach (HideSpot s in all)
            if (!spotMap.ContainsKey(s.id)) spotMap.Add(s.id, s);

        searchPlan = new List<int>();
        foreach (int id in remembered)
            if (spotMap.ContainsKey(id)) searchPlan.Add(id);
        HideSpot[] near = SortByDistance(all, lastSeen);
        foreach (HideSpot s in near)
            if (!searchPlan.Contains(s.id)) searchPlan.Add(s.id);
        planIdx = 0;
    }

    static HideSpot[] SortByDistance(HideSpot[] arr, Vector3 at)
    {
        List<HideSpot> list = new List<HideSpot>(arr);
        list.Sort((a, b) =>
            Vector3.Distance(a.transform.position, at)
            .CompareTo(Vector3.Distance(b.transform.position, at)));
        return list.ToArray();
    }

    void RememberHideouts()
    {
        if (!player.Hidden || player.currentSpot == null) return;
        HideSpot s = player.currentSpot;
        Vector3 peek = s.transform.position + Vector3.up * 0.7f;
        float d = Vector3.Distance(headNode.position, peek);
        if (d < 20f)
        {
            Vector3 dir = (peek - headNode.position).normalized;
            Vector3 origin = headNode.position + dir * 0.55f;
            if (!Physics.Raycast(origin, dir, d - 0.6f))
                remembered.Add(s.id);
        }
    }

    // ================= ÉRZÉKELÉS =================

    bool CanSee()
    {
        if (player == null || player.Hidden) return false;
        Vector3 hp = headNode.position;
        Vector3 chest = player.transform.position + Vector3.up * 1.15f;
        Vector3 to = chest - hp;
        float d = to.magnitude;
        float range = player.FlashOn ? flashSightRange : sightRange;
        if (d > range) return false;

        if (d > 6f)
        {
            Vector3 flat = to; flat.y = 0f;
            if (Vector3.Angle(transform.forward, flat) > fovDeg * 0.5f) return false;
        }

        Vector3 dir = to.normalized;
        Vector3 origin = hp + dir * 0.55f;
        if (Physics.Raycast(origin, dir, out RaycastHit hit, d - 0.6f))
        {
            if (hit.transform.root != player.transform.root) return false;
        }
        return true;
    }

    bool IsBlinded()
    {
        if (player == null || !player.FlashOn || player.flashlight == null) return false;
        Transform f = player.flashlight.transform;
        Vector3 toHead = headNode.position - f.position;
        float d = toHead.magnitude;
        if (d > 14f) return false;
        if (Vector3.Angle(f.forward, toHead) > 30f) return false;
        Vector3 dir = toHead.normalized;
        Vector3 origin = f.position + dir * 0.4f;
        return !Physics.Raycast(origin, dir, d - 0.6f);
    }

    // ================= MOZGÁS =================

    void Move(Vector3 target, float speed)
    {
        Vector3 to = target - transform.position; to.y = 0f;
        float d = to.magnitude;
        if (d < 0.05f) return;
        Vector3 dir = to / d;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(dir), 240f * Time.deltaTime);
        float sp = speed * (curBlind ? 0.3f : 1f); // lámpa erősen hátráltatja
        transform.position += dir * sp * Time.deltaTime;
    }

    // ================= ANIMÁCIÓ / ÉLETBOHÓC =================

    void UpdateLimbs()
    {
        float t = Time.time;
        bool reach = state == State.Chase || state == State.Stalk;

        // ---- törzs: lejtés és bandukolás ----
        float bobSpd = state == State.Chase ? 6.5f : 2.2f;
        body.localRotation = Quaternion.Slerp(body.localRotation,
            Quaternion.Euler(reach ? 9f : Mathf.Sin(t * 1.1f) * 2f, 0f,
                             Mathf.Sin(t * (state == State.Chase ? 3.5f : 1.6f)) * 3f),
            Time.deltaTime * 3f);
        body.localPosition = new Vector3(
            Mathf.Sin(t * bobSpd * 0.55f) * (reach ? 0.10f : 0.05f),
            Mathf.Abs(Mathf.Sin(t * bobSpd)) * (reach ? 0.14f : 0.06f),
            0f);

        // ---- karok ----
        float txL, txR, tzL, tzR;
        if (curBlind)
        {
            txL = txR = -128f; tzL = 9f; tzR = -9f;
        }
        else if (state == State.Chase)
        {
            txL = -80f + Mathf.Sin(t * 9f) * 12f;   // felváltva evez
            txR = -92f - Mathf.Sin(t * 9f) * 12f;
            tzL = 18f + Mathf.Sin(t * 5f) * 7f;
            tzR = -18f + Mathf.Sin(t * 5f) * 7f;
        }
        else if (state == State.Stalk)
        {
            txL = -55f + Mathf.Sin(t * 3.5f) * 9f;
            txR = -55f - Mathf.Sin(t * 3.5f) * 9f;
            tzL = 22f + Mathf.Sin(t * 2.5f) * 5f;
            tzR = -22f - Mathf.Sin(t * 2.5f) * 5f;
        }
        else
        {
            txL = Mathf.Sin(t * 1.1f) * 8f;
            txR = -Mathf.Sin(t * 0.9f) * 8f;
            tzL = 12f + Mathf.Sin(t * 1.3f) * 5f;
            tzR = -12f - Mathf.Sin(t * 1.1f) * 5f;
        }
        armL.localRotation = Quaternion.Slerp(armL.localRotation, Quaternion.Euler(txL, 0f, tzL), Time.deltaTime * 4f);
        armR.localRotation = Quaternion.Slerp(armR.localRotation, Quaternion.Euler(txR, 0f, tzR), Time.deltaTime * 4f);

        // ---- fej: követés, billegés, hipnotikus döntés, rángás ----
        float pitchT = Mathf.Sin(t * 1.4f) * 5f;
        float yawT = Mathf.Sin(t * 0.8f) * 8f;
        float tiltT = Mathf.Sin(t * 0.9f) * 4f;

        // nézzen a játékos felé (korlátozottRange: idegre, de ne túl messzire)
        Vector3 headLocal = headNode.parent.InverseTransformPoint(player.transform.position + Vector3.up * 2.9f);
        float dy = headLocal.y - headNode.localPosition.y;
        float dxz = Mathf.Sqrt(headLocal.x * headLocal.x + headLocal.z * headLocal.z);
        pitchT += -Mathf.Atan2(dy, Mathf.Max(0.01f, dxz)) * Mathf.Rad2Deg * 0.6f;
        yawT += Mathf.Atan2(headLocal.x, headLocal.z) * Mathf.Rad2Deg * 0.5f;

        // Csúzli Stare: lassan teljesen oldalra dönti a fejét
        if (state == State.Stare)
            tiltT = Mathf.SmoothStep(0f, 58f, Mathf.Clamp01(stateTimer / 2.2f));

        // Lurkkor alkalmanként idegbeteg részeg rángás
        bool twitch = state == State.Lurk && Mathf.PerlinNoise(t * 0.4f, 7.7f) > 0.92f;
        if (twitch)
        {
            pitchT += Mathf.Sin(t * 43f) * 7f;
            yawT += Mathf.Sin(t * 37f) * 9f;
        }

        pitchT = Mathf.Clamp(pitchT, -28f, 34f);
        yawT = Mathf.Clamp(yawT, -75f, 75f);
        headNode.localRotation = Quaternion.Slerp(headNode.localRotation,
            Quaternion.Euler(pitchT, yawT, tiltT), Time.deltaTime * 3.2f);

        // ---- állkapocs: üldözés közben csattog ----
        if (jaw != null)
        {
            float open = 0.06f;
            if (state == State.Chase) open = Mathf.Abs(Mathf.Sin(t * 10f)) * 0.55f;
            else if (state == State.Stalk) open = 0.22f;
            if (curBlind) open = 0.6f;
            jaw.localRotation = Quaternion.Slerp(jaw.localRotation, Quaternion.Euler(open, 0f, 0f), Time.deltaTime * 12f);
        }

        // ---- szemhúzás ----
        float squint = curBlind ? 0.3f : 1f;
        if (eyeL != null)
            eyeL.localScale = new Vector3(eyeBaseL.x, eyeBaseL.y * squint, eyeBaseL.z);
        if (eyeR != null)
            eyeR.localScale = new Vector3(eyeBaseR.x, eyeBaseR.y * squint, eyeBaseR.z);

        if (eyeLight != null)
            eyeLight.intensity = ((state == State.Chase ? 2.2f : 1.5f) *
                                 (0.85f + 0.15f * Mathf.Sin(t * (state == State.Chase ? 7f : 3.1f)))) *
                                 (1f + Mathf.Min(anger, maxRage) * 0.12f);
    }

    // ================= HANG =================

    public void PlayLaugh()
    {
        if (voice != null && voice.clip != null && !voice.isPlaying)
            voice.Play();
    }

    // "hu-hu-hu-huhu" bohóc-nevetés: süllyedő hangmagasságú pulzusok
    AudioClip SynthLaugh()
    {
        const int rate = 24000;
        const float dur = 1.15f;
        int len = Mathf.RoundToInt(rate * dur);
        float[] data = new float[len];
        System.Random rng = new System.Random(11);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            float v = 0f;
            for (int p = 0; p < 6; p++)
            {
                float t0 = p * 0.17f;
                float u = t - t0;
                if (u < 0f || u > 0.15f) continue;
                float f = 250f - p * 13f;
                float env = Mathf.Exp(-18f * u / 0.15f);
                v += (Mathf.Sin(2f * Mathf.PI * f * u) * 0.75f
                    + Mathf.Sin(2f * Mathf.PI * f * 2.6f * u) * 0.25f
                    + ((float)rng.NextDouble() * 2f - 1f) * 0.12f) * env;
            }
            data[i] = Mathf.Clamp(v * 0.75f, -1f, 1f);
        }
        AudioClip clip = AudioClip.Create("nevetes", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // ================= ÁLLAPOTOK: KERESÉS =================

    void StartJump()
    {
        if (jumping) return;
        jumping = true;
        if (gm != null) gm.Screech();
        StartCoroutine(JumpscareCo());
    }

    IEnumerator JumpscareCo()
    {
        Camera cam = player.GetComponentInChildren<Camera>(true);
        if (cam == null)
        {
            if (gm != null) gm.Caught("ELKAPOTT A JACKO!");
            yield break;
        }
        Vector3 face = cam.transform.position + cam.transform.forward * 1.35f;
        Vector3 stand = new Vector3(face.x, face.y - 3.02f, face.z);
        Vector3 from = transform.position;
        Vector3 headFrom = headNode.localScale;

        for (float t = 0f; t < 0.7f; t += Time.deltaTime)
        {
            float k = t / 0.7f;
            transform.position = Vector3.Lerp(from, stand, Mathf.SmoothStep(0f, 1f, k));
            headNode.localScale = Vector3.Lerp(headFrom, headFrom * 1.5f, k);
            Vector3 look = cam.transform.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(look);
            // rázkódó rángás közben
            headNode.localRotation = Quaternion.Euler(24f + Mathf.Sin(t * 60f) * 5f, 0f, Mathf.Sin(t * 47f) * 6f);
            if (jaw != null) jaw.localRotation = Quaternion.Euler(0.7f, 0f, 0f);
            yield return null;
        }
        if (gm != null) gm.Caught("ELKAPOTT A JACKO!  \"Mindig látlak... és visszajövök.\"");
        while (true)
        {
            Vector3 look = cam.transform.position - transform.position; look.y = 0f;
            if (look.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(look);
            yield return null;
        }
    }

    // ================= TEST ÉPÍTÉS (kódból) =================

    void BuildBody()
    {
        // textúrák a testen
        Texture2D suitTex = NH.StripeTex(128, new Color(0.24f, 0.06f, 0.19f), new Color(0.36f, 0.10f, 0.28f), 7, 11);
        Material darkSuit = NH.MatTex(new Color(0.18f, 0.05f, 0.16f), Color.clear, 0f, suitTex, 2.2f);
        Material suit = NH.MatTex(new Color(0.30f, 0.08f, 0.24f), Color.clear, 0f, suitTex, 2.6f);

        // harlekin rombusz-kabát (a tábla: lila + narancs gyémántok)
        Texture2D harlT = NH.DiamondTex(128, new Color(0.32f, 0.11f, 0.27f), new Color(0.62f, 0.30f, 0.07f), 8, 11);
        Material harl = NH.MatTex(new Color(0.55f, 0.35f, 0.18f), Color.clear, 0f, harlT, 2.4f);
        Material beltM = NH.Mat(new Color(0.14f, 0.09f, 0.06f), Color.clear, 0f);
        Material buckle = NH.Mat(new Color(0.85f, 0.6f, 0.2f), new Color(0.4f, 0.25f, 0.05f), 0.4f);
        Material bone = NH.Mat(new Color(0.82f, 0.76f, 0.68f), Color.clear, 0f);

        Texture2D pumpkinTex = NH.NoiseTex(128, new Color(0.62f, 0.25f, 0.03f), new Color(0.95f, 0.5f, 0.08f), 1.4f, 2.5f, 22);
        Material pumpkin = NH.MatTex(new Color(0.8f, 0.36f, 0.05f), new Color(0.9f, 0.3f, 0.02f), 0.85f, pumpkinTex, 2.0f);
        Material pumpkinDark = NH.MatTex(new Color(0.55f, 0.2f, 0.03f), Color.clear, 0f, pumpkinTex, 2.0f);

        glowMat = NH.Mat(new Color(1f, 0.5f, 0.08f), new Color(1f, 0.42f, 0.05f), 1.1f);
        Material tooth = NH.Mat(new Color(0.92f, 0.88f, 0.78f), new Color(0.9f, 0.8f, 0.5f), 0.25f);
        Material hatM = NH.Mat(new Color(0.13f, 0.11f, 0.16f), Color.clear, 0f);
        Material shoeM = NH.Mat(new Color(0.18f, 0.12f, 0.24f), Color.clear, 0f);

        body = new GameObject("test").transform;
        body.SetParent(transform, false);

        // lábak és cipők
        NH.Mesh(body, PrimitiveType.Capsule, darkSuit, new Vector3(0.24f, 0.85f, 0f), new Vector3(0.16f, 1.7f, 0.16f));
        NH.Mesh(body, PrimitiveType.Capsule, darkSuit, new Vector3(-0.24f, 0.85f, 0f), new Vector3(0.16f, 1.7f, 0.16f));
        NH.Mesh(body, PrimitiveType.Sphere, shoeM, new Vector3(0.27f, 0.12f, 0.16f), new Vector3(0.32f, 0.24f, 0.55f));
        NH.Mesh(body, PrimitiveType.Sphere, shoeM, new Vector3(-0.27f, 0.12f, 0.16f), new Vector3(0.32f, 0.24f, 0.55f));

        // csípő és torzó (torzó: harlekin rombuszmintás kabát)
        NH.Mesh(body, PrimitiveType.Sphere, suit, new Vector3(0f, 1.75f, 0f), new Vector3(0.4f, 0.35f, 0.3f));
        NH.Mesh(body, PrimitiveType.Capsule, harl, new Vector3(0f, 2.32f, 0f), new Vector3(0.27f, 1.05f, 0.27f));
        NH.Mesh(body, PrimitiveType.Sphere, glowMat, new Vector3(0f, 2.45f, 0.24f), new Vector3(0.08f, 0.08f, 0.05f));
        NH.Mesh(body, PrimitiveType.Sphere, glowMat, new Vector3(0f, 2.15f, 0.25f), new Vector3(0.08f, 0.08f, 0.05f));

        //öv csatokkal (a tábla szerint)
        NH.Mesh(body, PrimitiveType.Cube, beltM, new Vector3(0f, 1.98f, 0f), new Vector3(0.55f, 0.14f, 0.4f));
        NH.Mesh(body, PrimitiveType.Sphere, buckle, new Vector3(0f, 1.98f, 0.2f), new Vector3(0.09f, 0.11f, 0.05f));

        // ráncos gallér (két sorban)
        for (int i = 0; i < 9; i++)
        {
            float a = i * 40f * Mathf.Deg2Rad;
            Material m = (i % 2 == 0) ? bone : darkSuit;
            NH.Mesh(body, PrimitiveType.Sphere, m,
                new Vector3(Mathf.Cos(a) * 0.27f, 2.86f, Mathf.Sin(a) * 0.27f),
                new Vector3(0.17f, 0.13f, 0.17f));
            NH.Mesh(body, PrimitiveType.Sphere, m,
                new Vector3(Mathf.Cos(a) * 0.21f, 2.98f, Mathf.Sin(a) * 0.21f),
                new Vector3(0.12f, 0.1f, 0.12f));
        }

        // hosszú karok
        armL = new GameObject("karL").transform;
        armL.SetParent(body, false);
        armL.localPosition = new Vector3(0.48f, 2.7f, 0f);
        armL.localRotation = Quaternion.Euler(0f, 0f, 14f);
        NH.Mesh(armL, PrimitiveType.Capsule, harl, new Vector3(0f, -0.78f, 0f), new Vector3(0.1f, 1.5f, 0.1f));
        NH.Mesh(armL, PrimitiveType.Sphere, bone, new Vector3(0f, -1.58f, 0f), new Vector3(0.13f, 0.18f, 0.22f));

        armR = new GameObject("karR").transform;
        armR.SetParent(body, false);
        armR.localPosition = new Vector3(-0.48f, 2.7f, 0f);
        armR.localRotation = Quaternion.Euler(0f, 0f, -14f);
        NH.Mesh(armR, PrimitiveType.Capsule, harl, new Vector3(0f, -0.78f, 0f), new Vector3(0.1f, 1.5f, 0.1f));
        NH.Mesh(armR, PrimitiveType.Sphere, bone, new Vector3(0f, -1.58f, 0f), new Vector3(0.13f, 0.18f, 0.22f));

        // tökfej
        headNode = new GameObject("fej").transform;
        headNode.SetParent(body, false);
        headNode.localPosition = new Vector3(0f, 3.22f, 0f);
        NH.Mesh(headNode, PrimitiveType.Sphere, pumpkin, Vector3.zero, new Vector3(0.9f, 0.85f, 0.9f));

        // tök bordák (mélyebb redők)
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;
            float x = i * 0.16f;
            NH.Mesh(headNode, PrimitiveType.Capsule, pumpkinDark,
                new Vector3(x, -0.04f, 0.30f), new Vector3(0.045f, 0.72f, 0.18f));
        }

        // kereket szár a fejen
        var stem = NH.Mesh(headNode, PrimitiveType.Cylinder, pumpkinDark, new Vector3(0.02f, 0.36f, -0.02f), new Vector3(0.07f, 0.12f, 0.07f));
        stem.transform.localRotation = Quaternion.Euler(18f, 0f, -14f);

        // izzó szemek
        eyeL = NH.Mesh(headNode, PrimitiveType.Sphere, glowMat, new Vector3(0.15f, 0.06f, 0.34f), new Vector3(0.17f, 0.17f, 0.11f)).transform;
        eyeR = NH.Mesh(headNode, PrimitiveType.Sphere, glowMat, new Vector3(-0.15f, 0.06f, 0.34f), new Vector3(0.17f, 0.17f, 0.11f)).transform;
        eyeBaseL = eyeL.transform.localScale;
        eyeBaseR = eyeR.transform.localScale;

        // haragos szemöldök
        NH.Mesh(headNode, PrimitiveType.Cube, pumpkinDark, new Vector3(0.15f, 0.22f, 0.32f), new Vector3(0.22f, 0.045f, 0.04f));
        NH.Mesh(headNode, PrimitiveType.Cube, pumpkinDark, new Vector3(-0.15f, 0.22f, 0.32f), new Vector3(0.22f, 0.045f, 0.04f));

        // nyitott szájüreg + felső fogsor
        NH.Mesh(headNode, PrimitiveType.Cube, NH.Mat(new Color(0.08f, 0.03f, 0.01f), Color.clear, 0f),
            new Vector3(0f, -0.18f, 0.30f), new Vector3(0.42f, 0.16f, 0.10f));
        for (int i = 0; i < 5; i++)
        {
            float x = Mathf.Lerp(-0.18f, 0.18f, i / 4f);
            NH.Mesh(headNode, PrimitiveType.Sphere, tooth, new Vector3(x, -0.115f, 0.35f), new Vector3(0.06f, 0.07f, 0.05f));
        }

        // mozgó alsó állkapecske saját fogsorral
        jaw = new GameObject("all").transform;
        jaw.SetParent(headNode, false);
        jaw.localPosition = new Vector3(0f, -0.24f, 0.06f);
        jaw.localRotation = Quaternion.Euler(0.06f, 0f, 0f);
        NH.Mesh(jaw, PrimitiveType.Cube, NH.Mat(new Color(0.08f, 0.03f, 0.01f), Color.clear, 0f),
            new Vector3(0f, -0.02f, 0.24f), new Vector3(0.4f, 0.07f, 0.09f));
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Lerp(-0.13f, 0.13f, i / 3f);
            NH.Mesh(jaw, PrimitiveType.Sphere, tooth, new Vector3(x, 0.02f, 0.28f), new Vector3(0.05f, 0.06f, 0.05f));
        }

        // cilinder
        NH.Mesh(headNode, PrimitiveType.Cylinder, hatM, new Vector3(0f, 0.44f, 0f), new Vector3(0.66f, 0.05f, 0.66f));
        NH.Mesh(headNode, PrimitiveType.Cylinder, hatM, new Vector3(0f, 0.70f, 0f), new Vector3(0.42f, 0.5f, 0.42f));

        // szemfény fény
        GameObject lo = new GameObject("szemfeny");
        lo.transform.SetParent(headNode, false);
        lo.transform.localPosition = new Vector3(0f, 0f, 0.2f);
        eyeLight = lo.AddComponent<Light>();
        eyeLight.type = LightType.Point;
        eyeLight.color = new Color(1f, 0.45f, 0.08f);
        eyeLight.intensity = 1.5f;
        eyeLight.range = 7f;

        // publikus (billenthető), lassú talaj-lenzent szemek fényei nem tudnak kitenni…
        eyeL.gameObject.tag = "Untagged";
        eyeR.gameObject.tag = "Untagged";
    }
}
