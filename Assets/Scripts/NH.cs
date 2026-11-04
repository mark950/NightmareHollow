using UnityEngine;

// Közös segédeszközök – Nightmare Hollow
public static class NH
{
    public static Shader LitShader()
    {
        Shader s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Legacy Shaders/Diffuse");
        return s;
    }

    public static Material Mat(Color baseCol, Color emission, float emitIntensity)
    {
        Material m = new Material(LitShader());
        m.color = baseCol;
        if (emitIntensity > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission * emitIntensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        m.SetFloat("_Smoothness", 0.2f);
        return m;
    }

    public static Material MatTex(Color baseCol, Color emission, float emitIntensity, Texture2D tex, float tile)
    {
        Material m = Mat(baseCol, emission, emitIntensity);
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap"))
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTextureScale("_BaseMap", new Vector2(tile, tile));
            }
            if (m.HasProperty("_MainTex"))
            {
                m.SetTexture("_MainTex", tex);
                m.SetTextureScale("_MainTex", new Vector2(tile, tile));
            }
            m.SetFloat("_Smoothness", 0.35f);
        }
        return m;
    }

    // Anyag külön emisszív maszkkal (pl. tök-arc glow a sötétben)
    public static Material MatEmis(Color baseCol, Color emissionColor, float emitIntensity,
                                   Texture2D albedoTex, Texture2D emissionTex, float tile)
    {
        Material m = MatTex(baseCol, emissionColor, emitIntensity, albedoTex, tile);
        Texture2D eTex = emissionTex != null ? emissionTex : albedoTex;
        if (eTex != null)
        {
            if (m.HasProperty("_EmissionMap"))
            {
                m.SetTexture("_EmissionMap", eTex);
                m.SetTextureScale("_EmissionMap", new Vector2(tile, tile));
            }
        }
        return m;
    }

    // Perlin-zajos textúra (aszfalt, ráncos bőr stb.)
    public static Texture2D NoiseTex(int size, Color a, Color b, float contrast, float scale, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * scale / size * 19f, y * scale / size * 19f);
                n = Mathf.Clamp01((n - 0.5f) * contrast + 0.5f + (Random.value - 0.5f) * 0.10f);
                t.SetPixel(x, y, Color.Lerp(a, b, n));
            }
        }
        t.Apply();
        return t;
    }

    // Függőleges sávok (fa deszka, csíkos kabát)
    public static Texture2D StripeTex(int size, Color a, Color b, int bands, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool odd = ((x * bands / size) % 2) == 0;
                float n = (Random.value - 0.5f) * 0.12f;
                Color c = (odd ? a : b) + new Color(n, n, n);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    // Harlekin rombuszmintázat (Jacko kabátja) – két színű gyémántok + kosz
    public static Texture2D DiamondTex(int size, Color a, Color b, int diamonds, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        float step = size / (float)diamonds;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int px = (int)((x + y) / step);
                int py = (int)((x - y + size * 2f) / step);
                bool colA = ((px + py) & 1) == 0;
                float g = (Random.value - 0.5f) * 0.14f
                        + (Mathf.PerlinNoise(x * 0.10f, y * 0.10f) - 0.5f) * 0.25f;
                Color c = (colA ? a : b) + new Color(g, g, g);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    // Sakktábla (Security Office padló)
    public static Texture2D CheckerTex(int size, Color a, Color b, int cells, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool odd = ((x * cells / size) + (y * cells / size)) % 2 == 0;
                float n = (Random.value - 0.5f) * 0.10f
                        + (Mathf.PerlinNoise(x * 0.14f, y * 0.14f) - 0.5f) * 0.30f;
                Color c = (odd ? a : b) + new Color(n, n, n);
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    public static GameObject Mesh(Transform parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = type.ToString();
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
        }
        else
        {
            go.transform.position = pos;
        }
        go.transform.localScale = scale;
        if (mat != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
        return go;
    }

    // =================== TEXTÚRÁK A CONCEPT-TÁBLÁRÓL ===================

    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // 2D kereseszorzás kézzel (a Vector2-nek nincs Cross-ja)
        float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
        float d1 = Cross(p - a, b - a);
        float d2 = Cross(p - b, c - b);
        float d3 = Cross(p - c, a - c);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }

    // Faragott tök-arc (kapu-tök, izzó tökök) – az arc ragyog a sötétben (emisszív maszkkal!)
    public static Texture2D PumpkinFaceTex(int size, Color skinA, Color skinB, Color glow, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;

        // arc-háromszögek (u, v az alsó origóval: v=0 alul)
        Vector2 eyeL_a = new Vector2(0.44f, 0.68f), eyeL_b = new Vector2(0.40f, 0.58f), eyeL_c = new Vector2(0.485f, 0.58f);
        Vector2 eyeR_a = new Vector2(0.56f, 0.68f), eyeR_b = new Vector2(0.515f, 0.58f), eyeR_c = new Vector2(0.60f, 0.58f);
        Vector2 nose_a = new Vector2(0.500f, 0.565f), nose_b = new Vector2(0.472f, 0.525f), nose_c = new Vector2(0.528f, 0.525f);

        const float mouthL = 0.36f, mouthR = 0.64f;
        const float mouthBot = 0.44f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;

                // tök-bordák: szimmetrikus függőleges redők + durva szemcsézettség
                float rib = 0.5f + 0.5f * Mathf.Sin(u * 26f);
                float n = Mathf.PerlinNoise(x * 3.4f / size * 19f, y * 2.2f / size * 19f);
                float grain = (Random.value - 0.5f) * 0.10f;
                Color skin = Color.Lerp(skinA, skinB,
                    Mathf.Clamp01(rib * 0.45f + n * 0.55f + grain));

                bool carved = false;
                Vector2 p = new Vector2(u, v);
                if (InTri(p, eyeL_a, eyeL_b, eyeL_c)) carved = true;
                if (InTri(p, eyeR_a, eyeR_b, eyeR_c)) carved = true;
                if (InTri(p, nose_a, nose_b, nose_c)) carved = true;

                // jagged mosoly: fűrészfog-felül, egyenes alul, fogsor-elválasztók
                if (u > mouthL && u < mouthR)
                {
                    float uu = (u - mouthL) / (mouthR - mouthL);
                    float jag = (0.5f + 0.5f * Mathf.PerlinNoise(uu * 9f, 3.7f)) * 0.018f;
                    float top = 0.495f + jag;                       // fűrészelt felső él
                    float bottom = mouthBot + 0.02f * Mathf.Sin(uu * Mathf.PI);
                    if (v > bottom && v < top)
                    {
                        // fogközök: minden ~7%-ban sötét hasáb
                        if ((x % (size / 12)) < 2 && v > 0.462f) carved = false;
                        else carved = true;
                    }
                }

                t.SetPixel(x, y, carved ? glow : skin);
            }
        }
        t.Apply();
        return t;
    }

    // Fa palánkok: illesztési hézagok, szemek, szemcse (pénztár, bódék, kerítés, ládák)
    public static Texture2D PlankTex(int size, Color a, Color b, int planks, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;

        float plankH = (float)size / planks;
        // előre legenerált pöttyök/szemek
        int knotCount = planks * 2;
        Vector2[] knots = new Vector2[knotCount];
        float[] knotR = new float[knotCount];
        for (int k = 0; k < knotCount; k++)
        {
            int plank = Random.Range(0, planks);
            knots[k] = new Vector2(Random.Range(0f, 1f), (plank + 0.5f) / planks);
            knotR[k] = 0.012f + Random.value * 0.012f;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;
                int plank = Mathf.Min((int)(y / plankH), planks - 1);
                float fy = y - plank * plankH;

                // illesztési hézag a palánka alsó élén + enyhe árnyék felül
                float seam = fy < size * 0.014f ? -0.30f : (fy < size * 0.03f ? -0.12f : 0f);

                // palánka szánhangulata + hosszanti szemcse
                float plankTone = Mathf.PerlinNoise(plank * 0.71f + seed * 0.01f, 4.2f);
                float grain = Mathf.PerlinNoise(u * 55f, v * 4f + plank * 7.3f);
                float n = Mathf.Clamp01(grain * 0.35f + plankTone * 0.65f + (Random.value - 0.5f) * 0.06f);

                Color c = Color.Lerp(a, b, n);
                c += new Color(seam, seam, seam);

                // szemek (szem csomók)
                for (int k = 0; k < knotCount; k++)
                {
                    float d = Vector2.Distance(new Vector2(u, v), knots[k]);
                    if (d < knotR[k]) c *= 0.45f;
                    else if (d < knotR[k] * 1.35f && d > knotR[k] * 0.95f) c *= 0.75f;
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    // Rozsdás, bekarcolt fém (kapu, óriáskerék)
    public static Texture2D RustMetalTex(int size, Color a, Color b, Color rust, float rustAmount, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;

                // alapfém kikopott felülete
                float n = Mathf.PerlinNoise(u * 6f, v * 6f);
                n = Mathf.Clamp01((n - 0.5f) * 1.6f + 0.5f + (Random.value - 0.5f) * 0.08f);
                Color c = Color.Lerp(a, b, n);

                // rozsdafoltok
                float r = Mathf.PerlinNoise(u * 3.1f + 11f, v * 3.1f - 7f);
                float rustMask = Mathf.Clamp01((r - (1f - rustAmount)) / Mathf.Max(0.15f, rustAmount));
                if (rustMask > 0f)
                {
                    float speckle = Mathf.PerlinNoise(u * 22f, v * 22f);
                    c = Color.Lerp(c, rust, rustMask * (0.45f + 0.4f * speckle));
                }

                // vízszintes lekarcolások
                float scratch = Mathf.PerlinNoise(u * 1.2f, v * 40f);
                if (scratch > 0.84f) c *= 1.18f;
                else if (scratch < 0.18f) c *= 0.86f;

                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    // Koptatott márvány / kopott ezüst (iroda falai; jelek a bányász salakjához)
    public static Texture2D MarbleTex(int size, Color a, Color b, float veinScale, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;
                float warp = Mathf.PerlinNoise(u * 2.2f, v * 2.2f) * 2.4f;
                float vein = Mathf.Abs(Mathf.Sin((u + v * 0.6f + warp) * veinScale));
                float n = Mathf.PerlinNoise(u * 8f, v * 8f);

                Color c = Color.Lerp(a, b, Mathf.Clamp01(n * 0.7f + vein * 0.3f));
                if (vein < 0.08f) c *= 1.35f;      // világos erek
                else if (vein > 0.94f) c *= 0.7f;
                c = Color.Lerp(c, c * 1.08f, Mathf.PerlinNoise(u * 30f, v * 30f)); // kopása por
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    // Holdfelszín (a "Hold" gömb): kráterekkel
    public static Texture2D MoonTex(int size, Color a, Color b, int seed)
    {
        Random.InitState(seed);
        Texture2D t = new Texture2D(size, size, TextureFormat.RGB24, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Bilinear;

        int craterCount = 14;
        Vector2[] craters = new Vector2[craterCount];
        float[] cr = new float[craterCount];
        for (int i = 0; i < craterCount; i++)
        {
            craters[i] = new Vector2(Random.Range(0f, 1f), Random.Range(0.15f, 0.85f));
            cr[i] = 0.03f + Random.value * 0.07f;
        }

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size;
                float v = (float)y / size;
                float n = Mathf.PerlinNoise(u * 5f, v * 5f);
                n = Mathf.Clamp01((n - 0.5f) * 1.4f + 0.5f + (Random.value - 0.5f) * 0.06f);
                Color c = Color.Lerp(a, b, n);

                for (int i = 0; i < craterCount; i++)
                {
                    float d = Vector2.Distance(new Vector2(u, v), craters[i]);
                    if (d < cr[i]) c *= 0.72f;                      // kráterfenék
                    else if (d < cr[i] * 1.25f) c *= 1.22f;         // ragyogó szegély
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }
}
