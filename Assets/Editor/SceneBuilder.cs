using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

// Szerkesztői menü: egy kattintásra felépíti a teljes demo jelenetet
public static class SceneBuilder
{
    static List<Vector3> waypoints;
    static List<Vector3> boothPos;

    [MenuItem("Hollow Park/Demo jelenet építése")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        waypoints = new List<Vector3>();
        boothPos = new List<Vector3>();

        Atmosphere();
        Ground();
        Moon(new Vector3(30f, 38f, -70f));
        FerrisWheel(new Vector3(-20f, 0f, -26f));
        Carousel(new Vector3(20f, 0f, 36f));
        Gate();
        TicketBooth(new Vector3(9.5f, 0f, 43f), -155f);
        SecurityOffice(new Vector3(-30f, 0f, 32f), 35f);

        for (int i = 0; i < 12; i++)
        {
            if (i % 4 == 3) continue;
            float a = i * 30f * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 26f, 0f, Mathf.Cos(a) * 26f);
            Booth(p, i * 30f + 180f, i);
        }

        StringLights();
        Fence();
        Crates();
        Pumpkins();
        Barrels();
        Fuses();
        Particles();
        Player();
        Jacko();
        Manager();

        string dir = "Assets/Scenes";
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        string path = "Assets/Scenes/Demo.unity";
        EditorSceneManager.SaveScene(scene, path);

        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == path))
        {
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        Debug.Log("<b>HOLLOW PARK</b>: jelenet kész! Nyomd meg a Play gombot. " +
                  "(WASD: mozgás, Shift: futás, C: guggolás, F: lámpa, E: interakció) " +
                  "A lámpát a Security Office-ban találod!");
    }

    // ---------------- hangulat ----------------

    static void Atmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.024f;
        RenderSettings.fogColor = new Color(0.05f, 0.032f, 0.078f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.08f, 0.15f);
        RenderSettings.skybox = null;

        GameObject moon = new GameObject("Holdfeny");
        Light l = moon.AddComponent<Light>();
        l.type = LightType.Directional;
        l.color = new Color(0.55f, 0.6f, 0.9f);
        l.intensity = 0.3f;
        l.shadows = LightShadows.Soft;
        moon.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
    }

    static void Moon(Vector3 pos)
    {
        // holdfelszín a tábláról (kráterek)
        Texture2D holdT = NH.MoonTex(256, new Color(0.72f, 0.68f, 0.56f), new Color(0.97f, 0.93f, 0.80f), 21);
        var m = NH.Mesh(null, PrimitiveType.Sphere,
            NH.MatEmis(new Color(0.95f, 0.9f, 0.75f), new Color(0.9f, 0.85f, 0.65f), 1.6f, holdT, holdT, 1f),
            pos, new Vector3(5f, 5f, 5f));
        m.name = "Hold";
        var halo = NH.Mesh(null, PrimitiveType.Sphere,
            NH.Mat(new Color(0.4f, 0.42f, 0.55f), new Color(0.35f, 0.4f, 0.55f), 0.5f),
            pos, new Vector3(7.5f, 7.5f, 7.5f));
        halo.name = "Korona";
    }

    static void Ground()
    {
        Texture2D aszfalt = NH.NoiseTex(256, new Color(0.06f, 0.05f, 0.085f), new Color(0.14f, 0.11f, 0.16f), 1.3f, 2.2f, 5);
        Material gm = NH.MatTex(new Color(0.09f, 0.07f, 0.11f), Color.clear, 0f, aszfalt, 22f);
        gm.SetFloat("_Smoothness", 0.45f);

        var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
        g.name = "Talaj";
        g.transform.localScale = new Vector3(14f, 1f, 14f);
        g.GetComponent<Renderer>().sharedMaterial = gm;

        // sötét tócsák / kopott foltok
        Texture2D foltt = NH.NoiseTex(128, new Color(0.03f, 0.025f, 0.05f), new Color(0.10f, 0.07f, 0.13f), 1.6f, 3f, 9);
        Material foltm = NH.MatTex(new Color(0.08f, 0.06f, 0.1f), Color.clear, 0f, foltt, 2f);
        foltm.SetFloat("_Smoothness", 0.75f);
        Vector3[] foltok =
        {
            new Vector3(7f, 0.025f, 7f),   new Vector3(-12f, 0.025f, 4f),
            new Vector3(16f, 0.025f, -13f),new Vector3(-6f, 0.025f, -18f),
            new Vector3(0f, 0.025f, 24f),  new Vector3(-22f, 0.025f, -4f)
        };
        for (int i = 0; i < foltok.Length; i++)
        {
            var f = GameObject.CreatePrimitive(PrimitiveType.Plane);
            f.name = "Folt" + i;
            f.transform.position = foltok[i];
            float s = 1.6f + ((i * 7) % 5) * 0.5f;
            f.transform.localScale = new Vector3(s, 1f, s);
            f.GetComponent<Renderer>().sharedMaterial = foltm;
        }
    }

    // ---------------- részecskék ----------------

    static void Particles()
    {
        Ember("Szikrak", Vector3.zero, 55f, new Color(1f, 0.55f, 0.15f), 0.1f, 0.55f, 8f);
        Ember("TalajKod", new Vector3(0f, 0.5f, 0f), 60f, new Color(0.35f, 0.3f, 0.5f), 3.5f, 0.25f, 2.5f);
        Ember("OriasKerekekKod", new Vector3(-20f, 1f, -26f), 12f, new Color(0.6f, 0.85f, 1f), 0.07f, 0.3f, 6f);
    }

    static void Ember(string name, Vector3 pos, float radius, Color col, float size, float speed, float rate)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(col);
        main.startSize = size;
        main.startLifetime = new ParticleSystem.MinMaxCurve(rate, rate * 1.8f);
        main.maxParticles = 2000;
        main.startSpeed = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radius, 0.4f, radius);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.y = new ParticleSystem.MinMaxCurve(speed);
        vel.x = new ParticleSystem.MinMaxCurve(speed * 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(speed * 0.3f);

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = NH.LitShader();
        var pm = new Material(sh);
        Color glow = col * 1.6f;
        if (pm.HasProperty("_BaseColor")) pm.SetColor("_BaseColor", glow);
        if (pm.HasProperty("_Color")) pm.SetColor("_Color", glow);
        if (pm.HasProperty("_EmissionColor"))
        {
            pm.EnableKeyword("_EMISSION");
            pm.SetColor("_EmissionColor", col * 1.1f);
        }
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.material = pm;
        rend.renderMode = ParticleSystemRenderMode.Billboard;
    }

    // ---------------- óriáskerék ----------------

    static void FerrisWheel(Vector3 pos)
    {
        GameObject root = new GameObject("OriasKerekek");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation((-pos).normalized);

        Texture2D wheelT = NH.RustMetalTex(128, new Color(0.30f, 0.10f, 0.12f), new Color(0.44f, 0.15f, 0.15f), new Color(0.5f, 0.2f, 0.06f), 0.3f, 87);
        Material metal = NH.MatTex(new Color(0.35f, 0.10f, 0.12f), Color.clear, 0f, wheelT, 1f);
        Material pink = NH.Mat(new Color(1f, 0.35f, 0.7f), new Color(1f, 0.3f, 0.65f), 1.6f);

        NH.Mesh(root.transform, PrimitiveType.Cylinder, metal, new Vector3(0f, 4.5f, 0f), new Vector3(0.5f, 9f, 0.5f));

        GameObject spin = new GameObject("forgo");
        spin.transform.SetParent(root.transform, false);
        spin.transform.localPosition = new Vector3(0f, 9.5f, 0f);
        var rot = spin.AddComponent<Rotator>();
        rot.axis = new Vector3(0f, 0f, 1f);
        rot.speed = 6f;

        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f;
            var spoke = NH.Mesh(spin.transform, PrimitiveType.Cylinder, metal,
                new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 3.25f, Mathf.Sin(a * Mathf.Deg2Rad) * 3.25f, 0f),
                new Vector3(0.09f, 6.6f, 0.09f));
            spoke.transform.localRotation = Quaternion.Euler(0f, 0f, a);
        }
        for (int i = 0; i < 16; i++)
        {
            float deg = i * 22.5f;
            float rad = deg * Mathf.Deg2Rad;
            var seg = NH.Mesh(spin.transform, PrimitiveType.Cylinder, metal,
                new Vector3(Mathf.Cos(rad) * 6.5f, Mathf.Sin(rad) * 6.5f, 0f),
                new Vector3(0.07f, 1.32f, 0.07f));
            seg.transform.localRotation = Quaternion.Euler(0f, 0f, deg + 90f);
        }
        for (int i = 0; i < 8; i++)
        {
            float rad = i * 45f * Mathf.Deg2Rad;
            NH.Mesh(spin.transform, PrimitiveType.Sphere, pink,
                new Vector3(Mathf.Cos(rad) * 6.9f, Mathf.Sin(rad) * 6.9f, 0f),
                new Vector3(0.22f, 0.22f, 0.22f));
        }
        Material cabin = NH.Mat(new Color(0.7f, 0.15f, 0.2f), Color.clear, 0f);
        for (int i = 0; i < 6; i++)
        {
            float rad = i * 60f * Mathf.Deg2Rad;
            NH.Mesh(spin.transform, PrimitiveType.Cube, cabin,
                new Vector3(Mathf.Cos(rad) * 6.2f, Mathf.Sin(rad) * 6.2f - 0.55f, 0f),
                new Vector3(0.55f, 0.7f, 0.55f));
        }
    }

    // ---------------- bódék ----------------

    static void Booth(Vector3 pos, float yAngle, int index)
    {
        GameObject g = new GameObject("Booth");
        g.transform.position = pos;
        g.transform.rotation = Quaternion.Euler(0f, yAngle, 0f);
        boothPos.Add(pos);

        Texture2D woodT = NH.PlankTex(128, new Color(0.14f, 0.09f, 0.065f), new Color(0.22f, 0.145f, 0.10f), 5, 40 + index);
        Material post = NH.MatTex(new Color(0.2f, 0.13f, 0.1f), Color.clear, 0f, woodT, 1.6f);
        Material roof = NH.Mat(new Color(0.15f, 0.08f, 0.2f), Color.clear, 0f);

        NH.Mesh(g.transform, PrimitiveType.Cylinder, post, new Vector3(2.0f, 1.5f, 1.35f), new Vector3(0.18f, 3.0f, 0.18f));
        NH.Mesh(g.transform, PrimitiveType.Cylinder, post, new Vector3(-2.0f, 1.5f, 1.35f), new Vector3(0.18f, 3.0f, 0.18f));
        NH.Mesh(g.transform, PrimitiveType.Cylinder, post, new Vector3(2.0f, 1.5f, -1.35f), new Vector3(0.18f, 3.0f, 0.18f));
        NH.Mesh(g.transform, PrimitiveType.Cylinder, post, new Vector3(-2.0f, 1.5f, -1.35f), new Vector3(0.18f, 3.0f, 0.18f));
        NH.Mesh(g.transform, PrimitiveType.Cube, post, new Vector3(0f, 0.55f, 1.05f), new Vector3(4.3f, 1.1f, 0.5f));
        NH.Mesh(g.transform, PrimitiveType.Cube, roof, new Vector3(0f, 3.3f, 0f), new Vector3(5.0f, 0.25f, 3.4f));

        for (int s = 0; s < 6; s++)
        {
            bool lit = s % 2 == 0;
            Color c = lit ? new Color(0.85f, 0.25f, 0.2f) : new Color(0.9f, 0.85f, 0.75f);
            Material m = NH.Mat(c, lit ? new Color(1f, 0.3f, 0.25f) : Color.clear, lit ? 0.5f : 0f);
            var stripe = NH.Mesh(g.transform, PrimitiveType.Cube, m,
                new Vector3(-2.5f + s * 0.85f, 3.62f, 0.6f), new Vector3(0.86f, 0.08f, 1.0f));
            stripe.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        }

        Color signC = new Color(1f, 0.45f, 0.8f);
        NH.Mesh(g.transform, PrimitiveType.Cube, NH.Mat(signC, signC, 1.2f),
            new Vector3(0f, 2.9f, 1.28f), new Vector3(2.2f, 0.5f, 0.08f));

        // minden harmadik bódé fölé színes fénysrott
        if (index % 3 == 0)
        {
            Color[] cols = { new Color(1f, 0.5f, 0.2f), new Color(0.6f, 0.4f, 1f), new Color(0.3f, 1f, 0.8f) };
            Color c = cols[(index / 3) % cols.Length];
            GameObject dgo = new GameObject("DownLight");
            dgo.transform.SetParent(g.transform, false);
            dgo.transform.localPosition = new Vector3(0f, 3.1f, 0.6f);
            var dl = dgo.AddComponent<Light>();
            dl.type = LightType.Spot;
            dl.color = c;
            dl.intensity = 2.2f;
            dl.range = 8f;
            dl.spotAngle = 55f;
        }

        waypoints.Add(pos + new Vector3(0f, 0f, 3.5f));
    }

    // ---------------- szalagfények a bódék között ----------------

    static void StringLights()
    {
        if (boothPos.Count < 2) return;
        Color[] cols =
        {
            new Color(1f, 0.65f, 0.25f),
            new Color(0.85f, 0.3f, 0.9f),
            new Color(1f, 0.35f, 0.3f)
        };
        for (int b = 0; b < boothPos.Count; b++)
        {
            Vector3 a = boothPos[b];
            Vector3 c = boothPos[(b + 1) % boothPos.Count];
            Vector3 mid = Vector3.zero; // átlátszó irány a közép felé
            Vector3 dir = (c - a);
            float len = dir.magnitude;
            if (len < 0.5f) continue;
            int steps = Mathf.CeilToInt(len / 0.85f);
            for (int k = 0; k <= steps; k++)
            {
                float tt = k / (float)steps;
                Vector3 p = Vector3.Lerp(a, c, tt);
                p.y = 2.9f - Mathf.Sin(tt * Mathf.PI) * 0.85f;
                Material m = NH.Mat(cols[(k + b) % cols.Length], cols[(k + b) % cols.Length], 1.35f);
                NH.Mesh(null, PrimitiveType.Sphere, m, p, new Vector3(0.1f, 0.1f, 0.1f)).name = "Bulb";
            }
        }
    }

    // ---------------- kerítés ----------------

    static void Fence()
    {
        Texture2D woodT = NH.PlankTex(96, new Color(0.12f, 0.08f, 0.06f), new Color(0.18f, 0.12f, 0.085f), 3, 77);
        Material postM = NH.MatTex(new Color(0.15f, 0.1f, 0.07f), Color.clear, 0f, woodT, 1f);
        Material tipM = NH.Mat(new Color(1f, 0.55f, 0.2f), new Color(1f, 0.5f, 0.18f), 1.1f);

        int count = 72;
        for (int i = 0; i < count; i++)
        {
            float a = i * 360f / count * Mathf.Deg2Rad;
            Vector3 p = new Vector3(Mathf.Sin(a) * 51f, 0f, Mathf.Cos(a) * 51f);
            NH.Mesh(null, PrimitiveType.Cube, postM, p + Vector3.up * 0.9f, new Vector3(0.14f, 1.8f, 0.14f)).name = "Kertesz";
            if (i % 6 == 0)
                NH.Mesh(null, PrimitiveType.Sphere, tipM, p + Vector3.up * 1.95f, new Vector3(0.12f, 0.12f, 0.12f)).name = "Lampa";
        }
    }

    // ---------------- ládák ----------------

    static void Crates()
    {
        Texture2D woodT = NH.PlankTex(96, new Color(0.2f, 0.13f, 0.09f), new Color(0.28f, 0.19f, 0.12f), 3, 88);
        Material crateM = NH.MatTex(new Color(0.24f, 0.16f, 0.11f), Color.clear, 0f, woodT, 1.2f);
        Vector3[] spots =
        {
            new Vector3(6f, 0f, 18f),   new Vector3(-16f, 0f, 7f),
            new Vector3(19f, 0f, -8f),  new Vector3(-11f, 0f, -20f),
            new Vector3(29f, 0f, 12f),  new Vector3(-28f, 0f, 20f)
        };
        for (int i = 0; i < spots.Length; i++)
        {
            float s = 0.7f + ((i * 5) % 4) * 0.15f;
            NH.Mesh(null, PrimitiveType.Cube, crateM, spots[i] + Vector3.up * s * 0.5f, new Vector3(s, s, s)).name = "Lada";
            if (i % 2 == 0)
                NH.Mesh(null, PrimitiveType.Cube, crateM, spots[i] + Vector3.up * (s + s * 0.45f), new Vector3(s * 0.9f, s * 0.9f, s * 0.9f)).name = "Lada2";
        }
    }

    // ---------------- tökök ----------------

    static void Pumpkins()
    {
        // izzó tökök: faragott arc (emisszív maszkkal) – hunyó tökök maradnak sima noise
        Texture2D tokArc = NH.PumpkinFaceTex(192, new Color(0.38f, 0.16f, 0.02f), new Color(0.95f, 0.52f, 0.10f), new Color(1f, 0.55f, 0.08f), 33);
        Texture2D tokHunyT = NH.NoiseTex(64, new Color(0.28f, 0.13f, 0.03f), new Color(0.5f, 0.26f, 0.06f), 1.4f, 3f, 32);
        for (int i = 0; i < 22; i++)
        {
            float a = i * 2.399f;
            float r = 8f + ((i * 37) % 34);
            Vector3 p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (Vector3.Distance(p, new Vector3(-20f, 0f, -26f)) < 9f) continue;

            bool lit = i % 3 == 0;
            Material m = lit
                ? NH.MatEmis(new Color(0.9f, 0.5f, 0.12f), new Color(1f, 0.5f, 0.08f), 1.35f, tokArc, tokArc, 1f)
                : NH.MatTex(new Color(0.45f, 0.27f, 0.1f), Color.clear, 0f, tokHunyT, 2f);
            var tok = NH.Mesh(null, PrimitiveType.Sphere, m,
                p + new Vector3(0f, 0.28f, 0f), new Vector3(0.62f, 0.5f, 0.62f));
            tok.name = "Tok";
            if (lit) tok.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // arc véletlen irányba
            NH.Mesh(null, PrimitiveType.Cylinder,
                NH.Mat(new Color(0.25f, 0.3f, 0.12f), Color.clear, 0f),
                p + new Vector3(0f, 0.62f, 0f), new Vector3(0.1f, 0.18f, 0.1f)).name = "Szar";
        }
    }

    // ---------------- bújó hordók ----------------

    static void Barrels()
    {
        Texture2D hordoT = NH.NoiseTex(96, new Color(0.28f, 0.2f, 0.12f), new Color(0.38f, 0.28f, 0.18f), 1.5f, 4f, 44);
        Material barrelM = NH.MatTex(new Color(0.32f, 0.24f, 0.16f), Color.clear, 0f, hordoT, 2.4f);
        Vector3[] spots =
        {
            new Vector3(9f, 0f, 9f),   new Vector3(-9f, 0f, 9f),
            new Vector3(9f, 0f, -9f),  new Vector3(-9f, 0f, -9f),
            new Vector3(14f, 0f, -2f), new Vector3(-14f, 0f, 2f),
            new Vector3(2f, 0f, 14f),  new Vector3(-2f, 0f, -14f),
            new Vector3(20f, 0f, 20f), new Vector3(-20f, 0f, -20f)
        };
        for (int i = 0; i < spots.Length; i++)
        {
            var b = NH.Mesh(null, PrimitiveType.Cylinder, barrelM,
                spots[i] + Vector3.up * 0.6f, new Vector3(0.7f, 1.2f, 0.7f));
            b.name = "BuoHordo" + i;

            var hs = b.AddComponent<HideSpot>();
            hs.id = i;
            hs.enterPoint = new GameObject("Peek").transform;
            hs.enterPoint.SetParent(b.transform, false);
            hs.enterPoint.localPosition = new Vector3(0f, 0.35f, -0.55f);
            Vector3 lookDir = (-spots[i]).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
                hs.enterPoint.rotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0f, lookDir.z));

            NH.Mesh(b.transform, PrimitiveType.Cylinder,
                NH.Mat(new Color(0.42f, 0.32f, 0.2f), Color.clear, 0f),
                new Vector3(0f, 0f, 0f), new Vector3(0.75f, 0.12f, 0.75f));
        }
    }

    // ---------------- biztosíték-stációk (6 hely, minden körben 3 random) ----------------

    static void Fuses()
    {
        Vector3[] spots =
        {
            new Vector3(-24f, 0f, 16f),
            new Vector3(24f, 0f, 4f),
            new Vector3(6f, 0f, 26f),
            new Vector3(30f, 0f, -16f),
            new Vector3(-28f, 0f, -20f),
            new Vector3(12f, 0f, -24f)
        };
        for (int i = 0; i < spots.Length; i++)
            FuseStation(spots[i], i);
    }

    static void FuseStation(Vector3 spot, int i)
    {
        GameObject s = new GameObject("FuseStation" + i);
        s.transform.position = spot;

        NH.Mesh(s.transform, PrimitiveType.Cube,
            NH.Mat(new Color(0.25f, 0.22f, 0.2f), Color.clear, 0f),
            new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 1f, 0.6f)).name = "Alap";

        Color glow = new Color(0.6f, 1f, 0.3f);
        var fuse = NH.Mesh(s.transform, PrimitiveType.Capsule,
            NH.Mat(new Color(0.8f, 0.9f, 0.5f), glow, 2.0f),
            new Vector3(0f, 1.25f, 0f), new Vector3(0.16f, 0.3f, 0.16f));
        fuse.name = "Biztositek";
        fuse.AddComponent<FusePickup>();

        GameObject lgo = new GameObject("FuseLight");
        lgo.transform.SetParent(s.transform, false);
        lgo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Light l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = glow;
        l.intensity = 1.4f;
        l.range = 5f;
    }

    // ---------------- kapu (a tábla: nagy ív + tök a csúcson) ----------------

    static void Gate()
    {
        GameObject g = new GameObject("Gate");
        g.transform.position = new Vector3(0f, 0f, 48f);
        g.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Texture2D metalT = NH.RustMetalTex(96, new Color(0.17f, 0.17f, 0.21f), new Color(0.24f, 0.24f, 0.28f), new Color(0.5f, 0.2f, 0.06f), 0.25f, 51);
        Material metal = NH.MatTex(new Color(0.2f, 0.2f, 0.25f), Color.clear, 0f, metalT, 2f);
        Texture2D tokArc = NH.PumpkinFaceTex(256, new Color(0.38f, 0.16f, 0.02f), new Color(0.95f, 0.52f, 0.10f), new Color(1f, 0.55f, 0.08f), 61);

        // oszlopok + lámpafejek
        NH.Mesh(g.transform, PrimitiveType.Cylinder, metal, new Vector3(-4.6f, 3f, 0f), new Vector3(0.9f, 6f, 0.9f));
        NH.Mesh(g.transform, PrimitiveType.Cylinder, metal, new Vector3(4.6f, 3f, 0f), new Vector3(0.9f, 6f, 0.9f));
        foreach (float sx in new float[] { -4.6f, 4.6f })
        {
            NH.Mesh(g.transform, PrimitiveType.Sphere,
                NH.Mat(new Color(1f, 0.75f, 0.4f), new Color(1f, 0.65f, 0.3f), 1.4f),
                new Vector3(sx, 6.35f, 0f), new Vector3(0.26f, 0.3f, 0.26f)).name = "OszlopLampa";
            GameObject pgo = new GameObject("OszlopFeny");
            pgo.transform.SetParent(g.transform, false);
            pgo.transform.localPosition = new Vector3(sx, 6.2f, 0.5f);
            Light p2 = pgo.AddComponent<Light>();
            p2.type = LightType.Point;
            p2.color = new Color(1f, 0.6f, 0.25f);
            p2.intensity = 1.2f;
            p2.range = 10f;
        }

        // gerendák + ívdísz
        NH.Mesh(g.transform, PrimitiveType.Cube, metal, new Vector3(0f, 5.4f, 0f), new Vector3(10.6f, 1.5f, 0.7f));
        NH.Mesh(g.transform, PrimitiveType.Cube, metal, new Vector3(0f, 6.35f, 0f), new Vector3(8.0f, 0.5f, 0.55f));

        // tök a csúcson (az arc ragyog a sötétben – emisszív maszkkal)
        var kapuTok = NH.Mesh(g.transform, PrimitiveType.Sphere,
            NH.MatEmis(new Color(0.95f, 0.5f, 0.1f), new Color(1f, 0.5f, 0.08f), 1.6f, tokArc, tokArc, 1f),
            new Vector3(0f, 6.85f, 0f), new Vector3(1.0f, 0.85f, 1.0f));
        kapuTok.name = "KapuTok";
        kapuTok.transform.localRotation = Quaternion.Euler(-25f, 180f, 0f); // az arc a park felé, kissé leirányuljon
        GameObject tl = new GameObject("KapuTokFeny");
        tl.transform.SetParent(g.transform, false);
        tl.transform.localPosition = new Vector3(0f, 6.8f, 0.4f);
        Light tl2 = tl.AddComponent<Light>();
        tl2.type = LightType.Point;
        tl2.color = new Color(1f, 0.5f, 0.15f);
        tl2.intensity = 1.6f;
        tl2.range = 11f;

        GameObject door = NH.Mesh(g.transform, PrimitiveType.Cube,
            NH.MatTex(new Color(0.1f, 0.08f, 0.12f), Color.clear, 0f, metalT, 3f),
            new Vector3(0f, 2.1f, 0f), new Vector3(7.6f, 4.2f, 0.25f));
        door.name = "Door";

        // zöld KIJÁRAT tábla a gerendán
        Color exitC = new Color(0.3f, 1f, 0.4f);
        NH.Mesh(g.transform, PrimitiveType.Cube, NH.Mat(exitC, exitC, 2.2f),
            new Vector3(0f, 5.15f, 0.45f), new Vector3(2.4f, 0.6f, 0.1f)).name = "ExitTavla";

        GameObject lgo = new GameObject("OpenLight");
        lgo.transform.SetParent(g.transform, false);
        lgo.transform.localPosition = new Vector3(0f, 3.6f, -1.5f);
        Light pl = lgo.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = exitC;
        pl.intensity = 3f;
        pl.range = 10f;
        lgo.SetActive(false);

        // ---- izzó "HOLLOW PARK" felirat ----
        Font fnt = null;
        try { fnt = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (fnt == null) { try { fnt = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
        if (fnt != null)
        {
            GameObject title = new GameObject("Felirat");
            title.transform.SetParent(g.transform, false);
            title.transform.localPosition = new Vector3(0f, 5.45f, 0.5f);
            title.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // a játékos felé nézzen
            TextMesh tm = title.AddComponent<TextMesh>();
            tm.text = "HOLLOW PARK";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 40;
            tm.characterSize = 0.2f;
            tm.fontStyle = FontStyle.Bold;
            tm.font = fnt;
            MeshRenderer mr = title.GetComponent<MeshRenderer>();
            mr.sharedMaterial = fnt.material;
            tm.color = new Color(1f, 0.55f, 0.2f);
        }
        else
            Debug.LogWarning("Hollow Park: nincs beépített betűtípus a kapufelirathoz.");

        EscapeGate gate = g.AddComponent<EscapeGate>();
        gate.door = door.transform;
        gate.openSignLight = pl;

        BoxCollider trig = g.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.center = new Vector3(0f, 2.2f, -3.2f);
        trig.size = new Vector3(8.4f, 4.5f, 6f);
    }

    // ---------------- pénztárbódé (a tábla: TICKETS bódé) ----------------

    static void TicketBooth(Vector3 pos, float yDeg)
    {
        GameObject b = new GameObject("Penztar");
        b.transform.position = pos;
        b.transform.rotation = Quaternion.Euler(0f, yDeg, 0f);

        Texture2D woodT = NH.PlankTex(96, new Color(0.14f, 0.09f, 0.07f), new Color(0.22f, 0.15f, 0.1f), 4, 91);
        Material wood = NH.MatTex(new Color(0.2f, 0.13f, 0.1f), Color.clear, 0f, woodT, 1.8f);
        Material roofM = NH.Mat(new Color(0.13f, 0.07f, 0.18f), Color.clear, 0f);

        NH.Mesh(b.transform, PrimitiveType.Cube, wood, new Vector3(0f, 1.2f, 0f), new Vector3(2.2f, 2.4f, 1.6f));
        NH.Mesh(b.transform, PrimitiveType.Cube, roofM, new Vector3(0f, 2.6f, 0f), new Vector3(2.7f, 0.3f, 2.1f));
        // ablak-nyílás + világító üveg
        NH.Mesh(b.transform, PrimitiveType.Cube,
            NH.Mat(new Color(0.9f, 0.75f, 0.4f), new Color(0.9f, 0.7f, 0.35f), 0.9f),
            new Vector3(0f, 1.55f, 0.82f), new Vector3(1.5f, 0.7f, 0.06f)).name = "PenztarAblak";

        // TICKETS felirat
        Font fnt = null;
        try { fnt = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        if (fnt != null)
        {
            GameObject s = new GameObject("Tickets");
            s.transform.SetParent(b.transform, false);
            s.transform.localPosition = new Vector3(0f, 2.2f, 0.85f);
            s.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // a plaza felé olvasható
            TextMesh tm = s.AddComponent<TextMesh>();
            tm.text = "TICKETS";
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = 30;
            tm.characterSize = 0.1f;
            tm.fontStyle = FontStyle.Bold;
            tm.font = fnt;
            s.GetComponent<MeshRenderer>().sharedMaterial = fnt.material;
            tm.color = new Color(1f, 0.8f, 0.45f);
        }

        GameObject lgo = new GameObject("PenztarFeny");
        lgo.transform.SetParent(b.transform, false);
        lgo.transform.localPosition = new Vector3(0f, 2.3f, 1.4f);
        Light l = lgo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.7f, 0.35f);
        l.intensity = 1.1f;
        l.range = 7f;
    }

    // ---------------- körhinta (a tábla jobboldala) ----------------

    static void Carousel(Vector3 pos)
    {
        GameObject root = new GameObject("Korhinta");
        root.transform.position = pos;

        Texture2D woodT = NH.PlankTex(96, new Color(0.28f, 0.12f, 0.08f), new Color(0.4f, 0.2f, 0.12f), 6, 95);
        Material woodM = NH.MatTex(new Color(0.32f, 0.16f, 0.1f), Color.clear, 0f, woodT, 1.5f);
        Material gold = NH.Mat(new Color(0.9f, 0.65f, 0.2f), new Color(0.6f, 0.4f, 0.1f), 0.5f);
        Material horseM = NH.Mat(new Color(0.75f, 0.3f, 0.35f), Color.clear, 0f);

        NH.Mesh(root.transform, PrimitiveType.Cylinder, woodM, new Vector3(0f, 0.15f, 0f), new Vector3(5.2f, 0.3f, 5.2f)).name = "KorhintaTalp";
        NH.Mesh(root.transform, PrimitiveType.Cylinder, gold, new Vector3(0f, 1.6f, 0f), new Vector3(0.3f, 3.2f, 0.3f)).name = "KorhintaRud";

        GameObject spin = new GameObject("KorhintaForgo");
        spin.transform.SetParent(root.transform, false);
        spin.transform.localPosition = Vector3.zero;
        var rot = spin.AddComponent<Rotator>();
        rot.axis = Vector3.up;
        rot.speed = 5f;

        // korlátkarika + mennyezet
        NH.Mesh(spin.transform, PrimitiveType.Cylinder, gold, new Vector3(0f, 1.2f, 0f), new Vector3(4.4f, 0.08f, 4.4f)).name = "Karika";
        NH.Mesh(spin.transform, PrimitiveType.Cylinder, NH.Mat(new Color(0.5f, 0.12f, 0.18f), Color.clear, 0f),
            new Vector3(0f, 3.4f, 0f), new Vector3(5.2f, 0.25f, 5.2f)).name = "Teto";
        NH.Mesh(spin.transform, PrimitiveType.Cylinder, NH.Mat(new Color(0.6f, 0.15f, 0.2f), Color.clear, 0f),
            new Vector3(0f, 3.9f, 0f), new Vector3(1.2f, 0.8f, 1.2f)).name = "TetoGomb";

        // izzósor a peremén
        for (int i = 0; i < 14; i++)
        {
            float a = i * (360f / 14f) * Mathf.Deg2Rad;
            NH.Mesh(spin.transform, PrimitiveType.Sphere,
                NH.Mat(new Color(1f, 0.8f, 0.4f), new Color(1f, 0.7f, 0.3f), 1.3f),
                new Vector3(Mathf.Cos(a) * 4.4f, 3.25f, Mathf.Sin(a) * 4.4f),
                new Vector3(0.12f, 0.12f, 0.12f)).name = "KorhintaIzso";
        }

        // fakó "lovak" (rúd + test + fej, collider nélkül)
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Mathf.Deg2Rad;
            GameObject h = new GameObject("Lo" + i);
            h.transform.SetParent(spin.transform, false);
            h.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.4f, 0f, Mathf.Sin(a) * 3.4f);
            h.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg + 90f, 0f);
            NH.Mesh(h.transform, PrimitiveType.Cylinder, gold, new Vector3(0f, 0.85f, 0f), new Vector3(0.06f, 2.4f, 0.06f));
            NH.Mesh(h.transform, PrimitiveType.Capsule, horseM, new Vector3(0f, 1.35f, 0f), new Vector3(0.32f, 0.85f, 0.55f));
            NH.Mesh(h.transform, PrimitiveType.Sphere, horseM, new Vector3(0f, 1.95f, 0.4f), new Vector3(0.28f, 0.3f, 0.42f));
        }

        // a forgó rész ütköző nélkül (ne taszigolja a játékost)
        DisableColliders(spin.transform);

        // ijesztő-zóna: belépéskor a körhinta "életre kel"
        GameObject z = new GameObject("KorhintaIjeszt");
        z.transform.position = pos;
        BoxCollider zc = z.AddComponent<BoxCollider>();
        zc.isTrigger = true;
        zc.center = new Vector3(0f, 2f, 0f);
        zc.size = new Vector3(11f, 4f, 11f);
        var sc = z.AddComponent<TriggerScare>();
        sc.message = "A körhinta magától forog... üresen.";
        sc.jackoLaugh = true;
    }

    static void DisableColliders(Transform root)
    {
        foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
    }

    // ---------------- Security Office (a tábla: monitor-fal, sakkpadló) ----------------

    static void SecurityOffice(Vector3 pos, float yDeg)
    {
        GameObject o = new GameObject("SecurityOffice");
        o.transform.position = pos;
        o.transform.rotation = Quaternion.Euler(0f, yDeg, 0f);

        Texture2D wallT = NH.MarbleTex(96, new Color(0.085f, 0.08f, 0.10f), new Color(0.17f, 0.155f, 0.185f), 6f, 71);
        Material wall = NH.MatTex(new Color(0.12f, 0.1f, 0.14f), Color.clear, 0f, wallT, 2.5f);
        Material metal = NH.Mat(new Color(0.2f, 0.2f, 0.24f), Color.clear, 0f);
        Material dark = NH.Mat(new Color(0.07f, 0.06f, 0.09f), Color.clear, 0f);

        // sakk-padló
        Texture2D checkT = NH.CheckerTex(128, new Color(0.06f, 0.06f, 0.08f), new Color(0.16f, 0.15f, 0.18f), 8, 72);
        Material floorM = NH.MatTex(new Color(0.1f, 0.1f, 0.12f), Color.clear, 0f, checkT, 6f);
        var fl = GameObject.CreatePrimitive(PrimitiveType.Plane);
        fl.name = "OfficePadlo";
        fl.transform.SetParent(o.transform, false);
        fl.transform.localPosition = Vector3.zero;
        fl.transform.localScale = new Vector3(0.9f, 1f, 0.7f); // ~9x7 m
        fl.GetComponent<Renderer>().sharedMaterial = floorM;

        // falak: 6 széles (x), 5 mély (z), AJTÓ a +z oldalon (középen nyílás)
        NH.Mesh(o.transform, PrimitiveType.Cube, wall, new Vector3(0f, 1.7f, -2.5f), new Vector3(6.2f, 3.4f, 0.2f)).name = "FalHatul";
        NH.Mesh(o.transform, PrimitiveType.Cube, wall, new Vector3(-2.1f, 1.7f, 2.5f), new Vector3(2.1f, 3.4f, 0.2f)).name = "FalE-elol";
        NH.Mesh(o.transform, PrimitiveType.Cube, wall, new Vector3(2.1f, 1.7f, 2.5f), new Vector3(2.1f, 3.4f, 0.2f)).name = "FalE-elol2";
        // ajtófelülvilágító
        NH.Mesh(o.transform, PrimitiveType.Cube, wall, new Vector3(0f, 2.9f, 2.5f), new Vector3(2.2f, 1f, 0.2f)).name = "FalAjanok";
        // oldalak
        NH.Mesh(o.transform, PrimitiveType.Cube, wall, new Vector3(-3f, 1.7f, 0f), new Vector3(0.2f, 3.4f, 5.2f)).name = "FalBal";
        NH.Mesh(o.transform, PrimitiveType.Cube, wall, new Vector3(3f, 1.7f, 0f), new Vector3(0.2f, 3.4f, 5.2f)).name = "FalJobb";
        // tető
        NH.Mesh(o.transform, PrimitiveType.Cube, dark, new Vector3(0f, 3.5f, 0f), new Vector3(6.4f, 0.2f, 5.4f)).name = "Teto";

        // íróasztal + monitorfal
        NH.Mesh(o.transform, PrimitiveType.Cube, metal, new Vector3(-0.5f, 0.9f, 1.9f), new Vector3(3.6f, 1.8f, 0.9f)).name = "Asztal";
        NH.Mesh(o.transform, PrimitiveType.Cube, dark, new Vector3(-0.5f, 1.85f, 1.7f), new Vector3(3.8f, 0.1f, 1.2f)).name = "AsztalLap";

        Color monC = new Color(0.35f, 0.6f, 1f);
        for (int i = 0; i < 5; i++)
        {
            float x = -2.2f + i * 0.85f;
            NH.Mesh(o.transform, PrimitiveType.Cube, dark, new Vector3(x, 1.55f, 2.28f), new Vector3(0.55f, 0.42f, 0.05f)).name = "Monitor" + i;
            if (i != 2) // egy monitor halott
                NH.Mesh(o.transform, PrimitiveType.Cube,
                    NH.Mat(monC * 0.6f, monC, 1.2f),
                    new Vector3(x, 1.55f, 2.24f), new Vector3(0.45f, 0.32f, 0.02f)).name = "MonitorKep" + i;
        }

        GameObject mgo = new GameObject("MonitorFeny");
        mgo.transform.SetParent(o.transform, false);
        mgo.transform.localPosition = new Vector3(-0.5f, 1.7f, 1.4f);
        Light ml = mgo.AddComponent<Light>();
        ml.type = LightType.Point;
        ml.color = new Color(0.4f, 0.6f, 1f);
        ml.intensity = 1.0f;
        ml.range = 6f;

        // asztali lámpa pick-up: A LÁMPA ITT VAN!
        GameObject lamp = new GameObject("ZsebLampa");
        lamp.transform.SetParent(o.transform, false);
        lamp.transform.localPosition = new Vector3(0.9f, 2.0f, 1.9f);
        NH.Mesh(lamp.transform, PrimitiveType.Cylinder,
            NH.Mat(new Color(0.25f, 0.25f, 0.28f), Color.clear, 0f),
            new Vector3(0f, 0f, 0f), new Vector3(0.09f, 0.3f, 0.09f)).name = "LampaTest";
        var lampGlow = NH.Mesh(lamp.transform, PrimitiveType.Sphere,
            NH.Mat(new Color(1f, 0.95f, 0.75f), new Color(1f, 0.9f, 0.6f), 1.3f),
            new Vector3(0f, 0.18f, 0f), new Vector3(0.09f, 0.09f, 0.09f));
        lampGlow.name = "LampaFej";
        lamp.AddComponent<FlashlightPickup>();
        SphereCollider lampC = lamp.AddComponent<SphereCollider>();
        lampC.radius = 0.45f;
        lampC.center = new Vector3(0f, 0.1f, 0f);

        // belső bújóhordó
        Texture2D hordoT = NH.NoiseTex(96, new Color(0.28f, 0.2f, 0.12f), new Color(0.38f, 0.28f, 0.18f), 1.5f, 4f, 44);
        var bh = NH.Mesh(o.transform, PrimitiveType.Cylinder,
            NH.MatTex(new Color(0.32f, 0.24f, 0.16f), Color.clear, 0f, hordoT, 2.4f),
            new Vector3(2.1f, 0.6f, -1.4f), new Vector3(0.7f, 1.2f, 0.7f));
        bh.name = "OfficeHordo";
        var hs = bh.AddComponent<HideSpot>();
        hs.id = 20;
        hs.enterPoint = new GameObject("Peek").transform;
        hs.enterPoint.SetParent(bh.transform, false);
        hs.enterPoint.localPosition = new Vector3(-0.35f, 0.35f, -0.55f);
        hs.enterPoint.localRotation = Quaternion.Euler(0f, 90f, 0f);

        // ijesztő-zóna az ajtó előtt (kövesse az iroda forgatását)
        GameObject z = new GameObject("OfficeIjeszt");
        z.transform.SetParent(o.transform, false);
        z.transform.localPosition = new Vector3(0f, 1.2f, 3.9f);
        BoxCollider zc = z.AddComponent<BoxCollider>();
        zc.isTrigger = true;
        zc.center = Vector3.zero;
        zc.size = new Vector3(4f, 4f, 2f);
        var sc = z.AddComponent<TriggerScare>();
        sc.message = "A monitorok még meggyulladtak... valaki most járt itt.";
        sc.jackoLaugh = false;
    }

    // ---------------- játékos ----------------

    static void Player()
    {
        GameObject p = new GameObject("Player");
        p.transform.position = new Vector3(0f, 0.05f, 34f);
        p.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        p.tag = "Player";

        CharacterController cc = p.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        PlayerController pc = p.AddComponent<PlayerController>();

        GameObject pivot = new GameObject("CamPivot");
        pivot.transform.SetParent(p.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.45f, 0f);

        GameObject camGO = new GameObject("Cam", typeof(Camera), typeof(AudioListener));
        camGO.transform.SetParent(pivot.transform, false);
        Camera cam = camGO.GetComponent<Camera>();
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.08f;
        cam.backgroundColor = new Color(0.01f, 0.008f, 0.03f);
        cam.tag = "MainCamera";

        GameObject flGO = new GameObject("Flashlight");
        flGO.transform.SetParent(camGO.transform, false);
        flGO.transform.localPosition = new Vector3(0.24f, -0.16f, 0.05f);
        Light spot = flGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.range = 30f;
        spot.spotAngle = 58f;
        spot.intensity = 3f;
        spot.color = new Color(1f, 0.88f, 0.72f);
        spot.shadows = LightShadows.Soft;

        pc.camPivot = pivot.transform;
        pc.flashlight = spot;

        AddWall(new Vector3(0f, 3f, -52f), new Vector3(110f, 6f, 2f));
        AddWall(new Vector3(0f, 3f, 52f), new Vector3(110f, 6f, 2f));
        AddWall(new Vector3(-52f, 3f, 0f), new Vector3(2f, 6f, 110f));
        AddWall(new Vector3(52f, 3f, 0f), new Vector3(2f, 6f, 110f));
    }

    static void AddWall(Vector3 pos, Vector3 size)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "Hatar";
        w.transform.position = pos;
        w.transform.localScale = size;
        Object.DestroyImmediate(w.GetComponent<Renderer>());
        Object.DestroyImmediate(w.GetComponent<MeshFilter>());
    }

    // ---------------- Jacko és vezérlő ----------------

    static void Jacko()
    {
        GameObject j = new GameObject("Jacko");
        j.transform.position = new Vector3(0f, 0f, -38f);

        var ai = j.AddComponent<JackoAI>();
        ai.patrolPoints.Add(new Vector3(0f, 0f, 0f));
        ai.patrolPoints.Add(new Vector3(14f, 0f, 10f));
        ai.patrolPoints.Add(new Vector3(-14f, 0f, 10f));
        ai.patrolPoints.Add(new Vector3(18f, 0f, -12f));
        ai.patrolPoints.Add(new Vector3(-18f, 0f, -12f));
        ai.patrolPoints.Add(new Vector3(0f, 0f, 20f));
        ai.patrolPoints.Add(new Vector3(0f, 0f, -24f));
        ai.patrolPoints.Add(new Vector3(30f, 0f, 0f));
        ai.patrolPoints.Add(new Vector3(-30f, 0f, 0f));

        foreach (Vector3 w in waypoints) ai.patrolPoints.Add(w);
    }

    static void Manager()
    {
        new GameObject("GameManager").AddComponent<GameManager>();
    }
}
