using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager I;

    public int fusesNeeded = 3;
    int fuses;
    public int Fuses { get { return fuses; } }

    bool over, won;
    string endText = "";
    string msg = "";
    float msgTimer;

    // futásidő + rekord
    float runTime;
    float bestTime;
    bool newRecord;

    // célok + pontszám (a tábla szerint: Find the flashlight → Find the fuse)
    int score;
    bool flashlightFound;
    bool gateOpen;
    public int Score { get { return score; } }
    public bool FlashlightFoundFlag { get { return flashlightFound; } }

    public void AddScore(int pts)
    {
        score += pts;
    }

    public void FlashlightFound()
    {
        if (flashlightFound) return;
        flashlightFound = true;
        AddScore(100);
        Msg("+100  Megvan a lámpás! [F] – be/ki");
    }

    JackoAI jacko;
    PlayerController player;
    AudioSource heart, drone, music, sfx;
    AudioClip screech;

    void Awake()
    {
        I = this;
    }

    void Start()
    {
        jacko = Object.FindFirstObjectByType<JackoAI>();
        player = Object.FindFirstObjectByType<PlayerController>();

        RandomizeFuses();

        bestTime = PlayerPrefs.GetFloat("NH_LegjobbIdo", 0f);

        heart = AddLoop(SynthHeart());
        drone = AddLoop(SynthDrone());

        music = AddLoop(SynthMusic());
        music.volume = 0.06f;   // távoli vidámparki zene (calliope)

        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
        screech = SynthScreech();
    }

    // Minden körben 3 random helyszín a 6 stációból
    void RandomizeFuses()
    {
        FusePickup[] all = Object.FindObjectsByType<FusePickup>(FindObjectsSortMode.None);
        if (all.Length <= fusesNeeded) return;

        List<FusePickup> pool = new List<FusePickup>(all);
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            FusePickup tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }
        for (int i = fusesNeeded; i < pool.Count; i++)
        {
            Transform root = pool[i].transform.parent != null ? pool[i].transform.parent : pool[i].transform;
            Destroy(root.gameObject);
        }
    }

    AudioSource AddLoop(AudioClip clip)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = true;
        src.volume = 0f;
        src.spatialBlend = 0f;
        return src;
    }

    public void Msg(string text)
    {
        msg = text;
        msgTimer = 2.6f;
    }

    public void AddFuse()
    {
        fuses++;
        if (flashlightFound) AddScore(100);
        Msg((flashlightFound ? "+100  " : "") + "Biztosíték megszerezve: " + fuses + "/" + fusesNeeded);
        if (fuses >= fusesNeeded)
        {
            EscapeGate gate = Object.FindFirstObjectByType<EscapeGate>();
            if (gate != null) { gate.Open(); gateOpen = true; }
        }
    }

    public void Screech()
    {
        if (sfx != null && screech != null) sfx.PlayOneShot(screech, 0.85f);
    }

    public void Caught(string text)
    {
        if (over || won) return;
        over = true;
        endText = text;
        if (heart != null) heart.volume = 0f;
        if (drone != null) { drone.volume = 0.2f; drone.pitch = 0.65f; }
        if (music != null) music.volume = 0.02f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Win()
    {
        if (over || won) return;
        won = true;

        newRecord = bestTime <= 0f || runTime < bestTime;
        if (newRecord)
        {
            bestTime = runTime;
            PlayerPrefs.SetFloat("NH_LegjobbIdo", bestTime);
            PlayerPrefs.Save();
        }

        AddScore(250); // szökési bónusz
        endText = "Sikerült meglógnod! " + FormatTime(runTime) +
                  "  (" + score + " pont)";
        if (heart != null) heart.volume = 0f;
        if (drone != null) drone.volume = 0f;
        if (music != null) music.volume = 0.09f; // győzelmi hangulat
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static string FormatTime(float t)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(t));
        return (total / 60).ToString("0") + ":" + (total % 60).ToString("00");
    }

    void Update()
    {
        if (msgTimer > 0f)
        {
            msgTimer -= Time.deltaTime;
            if (msgTimer <= 0f) msg = "";
        }

        if (!over && !won) runTime += Time.deltaTime;

        if (!over && !won && jacko != null && player != null)
        {
            // Szívverés és morajás attól függően, hogy milyen messze van a Jacko
            float d = Vector3.Distance(jacko.transform.position, player.transform.position);
            float t = Mathf.Clamp01(1f - d / 32f);
            if (heart != null)
            {
                heart.volume = t * t * 0.75f;
                heart.pitch = Mathf.Lerp(0.85f, 1.55f, t);
            }
            if (drone != null)
                drone.volume = Mathf.Clamp01(1f - d / 14f) * 0.3f;
        }

        if ((over || won) && Input.GetKeyDown(KeyCode.R))
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrEmpty(scene.name))
                SceneManager.LoadScene(scene.name);
            else
                SceneManager.LoadScene("Assets/Scenes/Demo.unity");
        }
    }

    // ================= HANGSZINTÉZIS =================

    AudioClip SynthHeart()
    {
        const int rate = 32000;
        float[] data = new float[rate]; // 1 mp
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / rate;
            float beat = Thump(t - 0.02f, 0.18f, 55f);
            beat += Thump(t - 0.36f, 0.14f, 50f) * 0.7f;
            data[i] = beat * 0.9f;
        }
        AudioClip clip = AudioClip.Create("hbeat", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    float Thump(float t, float dur, float freq)
    {
        if (t < 0f || t > dur) return 0f;
        float env = Mathf.Exp(-7f * t / dur);
        return Mathf.Sin(2f * Mathf.PI * freq * t) * env;
    }

    AudioClip SynthDrone()
    {
        const int rate = 22050;
        int len = rate * 3;
        float[] data = new float[len];
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            float v = Mathf.Sin(2f * Mathf.PI * 72f * t) * 0.5f
                    + Mathf.Sin(2f * Mathf.PI * 144.5f * t) * 0.22f
                    + Mathf.Sin(2f * Mathf.PI * 71.5f * t + Mathf.Sin(t * 1.5f)) * 0.2f;
            data[i] = v * 0.6f;
        }
        AudioClip clip = AudioClip.Create("drone", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Távoli, kísérteties calliope-keringő (A-moll) – halk, folyamatos loop
    AudioClip SynthMusic()
    {
        const int rate = 22050;
        const float beat = 0.5f;
        int steps = 16;
        int len = Mathf.RoundToInt(rate * beat * steps);
        float[] data = new float[len];

        // dallam: 0 = szünet
        float[] tune =
        {
            440f, 0f, 523.25f, 0f,   493.88f, 0f, 440f, 0f,
            523.25f, 0f, 659.25f, 0f, 440f, 0f, 0f, 0f
        };
        float[] bass = { 110f, 82.41f, 110f, 82.41f }; // minden 2 ütemre

        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            int step = Mathf.Min((int)(t / beat), steps - 1);
            float u = t - step * beat; // ütemen belüli idő

            float v = 0f;
            if (tune[step] > 0f)
            {
                float f = tune[step] * (1f + Mathf.Sin(t * 5.5f) * 0.006f);
                float env = Mathf.Exp(-2.2f * u / beat);
                v += (Mathf.Sin(2f * Mathf.PI * f * t) * 0.8f
                    + Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.12f) * env;
            }
            if (step % 2 == 0)
            {
                float bf = bass[(step / 2) % bass.Length];
                v += Mathf.Sin(2f * Mathf.PI * bf * t) * Mathf.Exp(-1.4f * u) * 0.5f;
            }
            data[i] = v;
        }
        AudioClip clip = AudioClip.Create("zene", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Jumpscare rikkanás: dübörgő süllyedő csikorgás + zaj
    AudioClip SynthScreech()
    {
        const int rate = 32000;
        int len = Mathf.RoundToInt(rate * 1.4f);
        float[] data = new float[len];
        System.Random rng = new System.Random(7);
        for (int i = 0; i < len; i++)
        {
            float t = (float)i / rate;
            float k = t / 1.4f;
            float f = Mathf.Lerp(1500f, 320f, k * k);
            float trem = 0.7f + 0.3f * Mathf.Sin(t * 34f);
            float env = Mathf.Exp(-1.6f * k) * Mathf.SmoothStep(0f, 1f, Mathf.Min(t / 0.03f, 1f));
            float v = (Mathf.Sin(2f * Mathf.PI * f * t) * 0.7f
                     + Mathf.Sin(2f * Mathf.PI * f * 2.02f * t) * 0.3f
                     + ((float)rng.NextDouble() * 2f - 1f) * 0.35f) * env * trem;
            data[i] = Mathf.Clamp(v, -1f, 1f) * 0.9f;
        }
        AudioClip clip = AudioClip.Create("rikanas", len, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void OnGUI()
    {
        // célkereszt
        GUIStyle cross = new GUIStyle();
        cross.fontSize = 22;
        cross.alignment = TextAnchor.MiddleCenter;
        cross.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        GUI.Label(new Rect(Screen.width / 2 - 12, Screen.height / 2 - 14, 24, 24), "+", cross);

        GUIStyle hud = new GUIStyle(GUI.skin.label);
        hud.fontSize = 20;
        hud.normal.textColor = Color.white;

        // interakciós tipp
        if (player != null && !string.IsNullOrEmpty(player.Hint))
        {
            GUIStyle hint = new GUIStyle(hud);
            hint.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(0, Screen.height / 2 + 44, Screen.width, 30), player.Hint, hint);
        }

        // biztosítékok
        GUI.Label(new Rect(14, 10, 420, 30), "Biztosíték: " + fuses + "/" + fusesNeeded, hud);

        // célok (a tábla szerint: Find the flashlight → Find the fuse)
        GUIStyle obj = new GUIStyle(hud);
        obj.fontSize = 16;
        GUIStyle doneS = new GUIStyle(obj);
        doneS.normal.textColor = new Color(0.65f, 1f, 0.65f, 0.9f);
        GUIStyle todoS = new GUIStyle(obj);
        todoS.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        int oy = 70;
        GUI.Label(new Rect(14, oy, 300, 24), (flashlightFound ? "✓ " : "○ ") + "Találd meg a lámpást",
            flashlightFound ? doneS : todoS);
        GUI.Label(new Rect(14, oy + 24, 300, 24),
            (fuses >= fusesNeeded ? "✓ " : "○ ") + "3 biztosíték (" + fuses + "/" + fusesNeeded + ")",
            fuses >= fusesNeeded ? doneS : todoS);
        if (gateOpen)
            GUI.Label(new Rect(14, oy + 48, 300, 24), "○ MENEKÜLJ A KAPUN!", doneS);

        // pontszám (a tábla: "+ 100")
        GUIStyle sc = new GUIStyle(hud);
        sc.fontSize = 26;
        sc.alignment = TextAnchor.LowerLeft;
        sc.normal.textColor = new Color(1f, 0.95f, 0.75f);
        GUI.Label(new Rect(14, Screen.height - 44, 300, 40), "+ " + score, sc);

        // harag-szint
        if (jacko != null)
        {
            int a = jacko.Anger, m = Mathf.Max(1, jacko.MaxRage);
            GUIStyle rage = new GUIStyle(hud);
            rage.fontSize = 16;
            rage.normal.textColor = a > 0 ? new Color(1f, 0.35f, 0.2f) : new Color(1f, 1f, 1f, 0.45f);
            string dots = new string('●', Mathf.Clamp(a, 0, m)) + new string('○', Mathf.Max(0, m - Mathf.Clamp(a, 0, m)));
            GUI.Label(new Rect(14, 40, 420, 26), "Jacko haragja: " + dots, rage);
        }

        // idő + legjobb idő
        GUIStyle timeHud = new GUIStyle(hud);
        timeHud.fontSize = 20;
        timeHud.alignment = TextAnchor.UpperRight;
        string bestTxt = bestTime > 0f ? "\nLegjobb: " + FormatTime(bestTime) : "";
        GUI.Label(new Rect(Screen.width - 330, 10, 316, 60),
            "Idő: " + FormatTime(runTime) + bestTxt, timeHud);

        // üzenet
        if (!string.IsNullOrEmpty(msg))
        {
            GUIStyle m = new GUIStyle(hud);
            m.alignment = TextAnchor.MiddleCenter;
            m.fontSize = 18;
            m.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(0, Screen.height - 90, Screen.width, 30), msg, m);
        }

        // vége (győzelem / bukás)
        if (over || won)
        {
            GUI.color = over ? new Color(0.25f, 0f, 0f, 0.75f) : new Color(0f, 0.15f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle big = new GUIStyle(hud);
            big.fontSize = 44;
            big.alignment = TextAnchor.MiddleCenter;
            big.normal.textColor = over ? new Color(1f, 0.3f, 0.2f) : new Color(0.6f, 1f, 0.6f);
            GUI.Label(new Rect(0, Screen.height / 2 - 80, Screen.width, 60), endText, big);

            GUIStyle small = new GUIStyle(hud);
            small.fontSize = 22;
            small.alignment = TextAnchor.MiddleCenter;
            string rec = (won && newRecord) ? "ÚJ REKORD!\n" : "";
            GUI.Label(new Rect(0, Screen.height / 2 - 20, Screen.width, 96),
                rec + (won ? "...de a Jacko emlékszik.\n" : "") +
                "Újra: nyomd meg az [R] billentyűt", small);
        }
    }
}
