using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace MirioShoto
{
    /// <summary>Uma entrada da casa por onde os insetos chegam depois da chuva.</summary>
    public class Entrance
    {
        public string name; public Vector3 outside, inside; public float lastSpawn = -10; public int recent;
        public Renderer marker; public Color color;
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I;
        public static string Selected = "Mirio"; public static bool selectDone; // personagem escolhido / tela de seleção já passou
        int selIdx; static readonly string[] Names = { "Mirio", "Shoto" };
        public enum State { Select, Playing, LevelUp, Paused, GameOver, Victory }

        // ===== Balanceamento =====
        public const float RoomW = 22f, RoomD = 14f;     // largura (X) e profundidade (Z) da sala
        public const float NightDuration = 300f, BossTime = 240f;
        public const int MaxInsects = 300;
        public const float CamYaw = 45f, CamPitch = 35f;

        public State state = State.Playing;
        public Cat cat; public FoodBowl bowl;
        public readonly List<Insect> insects = new();
        public readonly List<Pickup> pickups = new();
        public readonly List<Barricade> barricades = new();
        public readonly List<Entrance> entrances = new();
        public float time; public int level = 1, kills; public float xp;
        public string endMessage;

        float spawnAcc; bool bossSpawned; Insect boss; List<UpgradeOption> offer;
        Camera cam; Vector3 camTarget;
        public float XpNeeded => 5 + level * 4;

        void Awake()
        {
            I = this; Time.timeScale = 1; Barricade.NewRun();
            Feedback.numbers.Clear(); Feedback.shake = 0; Feedback.hitstop = 0;
            BuildRoom();
            bowl = new GameObject("PoteDeRacao").AddComponent<FoodBowl>();
            cat = new GameObject(Selected).AddComponent<Cat>(); cat.catName = Selected;
            cat.transform.position = new Vector3(0, 0, -1.8f);
            cat.weapons.Add(new Patada());
            SetupCameraAndLight();
            selIdx = Selected == "Shoto" ? 1 : 0;
            state = selectDone ? State.Playing : State.Select;
            Time.timeScale = state == State.Select ? 0 : 1;
        }

        void OnDestroy() { if (I == this) I = null; Time.timeScale = 1; }

        // ================== CENÁRIO ==================
        void BuildRoom()
        {
            var room = new GameObject("Sala").transform;
            float hw = RoomW / 2, hd = RoomD / 2;
            Art.Prim(PrimitiveType.Cube, room, new Vector3(0, -.05f, 0), new Vector3(RoomW, .1f, RoomD), new Color(.72f, .52f, .34f));
            for (float x = -hw + 1.1f; x < hw; x += 1.1f) // tábuas
                Art.Prim(PrimitiveType.Cube, room, new Vector3(x, .002f, 0), new Vector3(.03f, .01f, RoomD), new Color(.6f, .43f, .28f));
            Art.Prim(PrimitiveType.Cylinder, room, new Vector3(0, .01f, 0), new Vector3(7f, .01f, 5f), new Color(.65f, .25f, .3f)); // tapete

            // paredes do fundo altas (+X e +Z), da frente baixas para não tapar a visão isométrica
            Color wall = new(.93f, .88f, .8f), baseboard = new(.55f, .42f, .32f);
            Art.Prim(PrimitiveType.Cube, room, new Vector3(0, 1.2f, hd + .15f), new Vector3(RoomW + .6f, 2.4f, .3f), wall);
            Art.Prim(PrimitiveType.Cube, room, new Vector3(hw + .15f, 1.2f, 0), new Vector3(.3f, 2.4f, RoomD + .6f), wall);
            Art.Prim(PrimitiveType.Cube, room, new Vector3(0, .15f, -hd - .15f), new Vector3(RoomW + .6f, .3f, .3f), baseboard);
            Art.Prim(PrimitiveType.Cube, room, new Vector3(-hw - .15f, .15f, 0), new Vector3(.3f, .3f, RoomD + .6f), baseboard);

            // móveis encostados nas paredes (decoração e leitura de escala)
            Art.Prim(PrimitiveType.Cube, room, new Vector3(-5f, .45f, hd - .9f), new Vector3(4f, .9f, 1.4f), new Color(.35f, .45f, .6f));   // sofá
            Art.Prim(PrimitiveType.Cube, room, new Vector3(-5f, 1.05f, hd - .35f), new Vector3(4f, .6f, .35f), new Color(.3f, .4f, .55f));
            Art.Prim(PrimitiveType.Cube, room, new Vector3(hw - 1.5f, .5f, -3.5f), new Vector3(1.6f, .08f, 2.6f), new Color(.5f, .33f, .2f));   // mesinha
            Art.Prim(PrimitiveType.Cylinder, room, new Vector3(hw - 1.5f, .65f, -3f), new Vector3(.25f, .15f, .25f), new Color(.75f, .9f, 1f)); // copo na mesa
            Art.Prim(PrimitiveType.Cylinder, room, new Vector3(-hw + 1f, .4f, -hd + 1f), new Vector3(.7f, .4f, .7f), new Color(.6f, .35f, .25f)); // vaso
            Art.Prim(PrimitiveType.Sphere, room, new Vector3(-hw + 1f, 1.2f, -hd + 1f), new Vector3(1.2f, 1.1f, 1.2f), new Color(.3f, .6f, .3f));

            // entradas bem visíveis, cada uma com cor própria e marcador que acende ao spawnar
            AddEntrance(room, "Porta", new Vector3(3.5f, 0, hd), new Vector3(0, 0, -1), new Color(1f, .6f, .2f), () =>
            {
                Art.Prim(PrimitiveType.Cube, room, new Vector3(3.5f, 1.05f, hd + .02f), new Vector3(1.6f, 2.1f, .34f), new Color(.45f, .28f, .15f));
                Art.Prim(PrimitiveType.Cube, room, new Vector3(3.5f, .5f, hd - .02f), new Vector3(1.2f, 1f, .34f), new Color(.12f, .1f, .08f)); // vão aberto
            });
            AddEntrance(room, "Janela", new Vector3(hw, 0, 2.5f), new Vector3(-1, 0, 0), new Color(.4f, .8f, 1f), () =>
            {
                Art.Prim(PrimitiveType.Cube, room, new Vector3(hw - .02f, 1.4f, 2.5f), new Vector3(.34f, 1.1f, 1.8f), new Color(.5f, .75f, .95f));
                Art.Prim(PrimitiveType.Cube, room, new Vector3(hw - .05f, .85f, 2.5f), new Vector3(.5f, .08f, 2f), new Color(.85f, .82f, .75f));
            });
            AddEntrance(room, "Ralo", new Vector3(hw - 2.5f, 0, -hd + 2f), Vector3.zero, new Color(.6f, 1f, .4f), () =>
            {
                Art.Prim(PrimitiveType.Cylinder, room, new Vector3(hw - 2.5f, .02f, -hd + 2f), new Vector3(.9f, .02f, .9f), new Color(.35f, .35f, .38f));
                for (int i = -1; i <= 1; i++) Art.Prim(PrimitiveType.Cube, room, new Vector3(hw - 2.5f + i * .22f, .04f, -hd + 2f), new Vector3(.06f, .02f, .7f), new Color(.15f, .15f, .15f));
            });
            AddEntrance(room, "Fresta", new Vector3(-hw, 0, -1.5f), new Vector3(1, 0, 0), new Color(1f, .45f, .7f), () =>
            {
                Art.Prim(PrimitiveType.Cube, room, new Vector3(-hw - .02f, .08f, -1.5f), new Vector3(.36f, .12f, 1.2f), new Color(.08f, .06f, .05f));
            });
        }

        void AddEntrance(Transform room, string name, Vector3 edge, Vector3 inward, Color color, Action build)
        {
            build();
            var e = new Entrance { name = name, color = color };
            e.inside = inward == Vector3.zero ? edge : edge + inward * 1.2f;
            e.outside = inward == Vector3.zero ? edge : edge - inward * 1f;
            var m = Art.Prim(PrimitiveType.Cylinder, room, new Vector3(e.inside.x, .015f, e.inside.z), new Vector3(2.6f, .01f, 2.6f), color, default, true);
            var beam = Art.Prim(PrimitiveType.Cylinder, room, new Vector3(e.inside.x, 1.4f, e.inside.z), new Vector3(.12f, 1.4f, .12f), color, default, true);
            beam.GetComponent<Renderer>().sharedMaterial = Art.Emissive(color, 2.2f);
            m.GetComponent<Renderer>().sharedMaterial = Art.Emissive(color, 0f);
            e.marker = m.GetComponent<Renderer>();
            entrances.Add(e);
        }

        public Entrance BusiestEntrance()
        {
            Entrance best = entrances[0];
            foreach (var e in entrances) if (e.recent > best.recent) best = e;
            return best;
        }

        void SetupCameraAndLight()
        {
            cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5.6f;
            cam.transform.rotation = Quaternion.Euler(CamPitch, CamYaw, 0);
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.1f, .1f, .14f);
            cam.nearClipPlane = .1f; cam.farClipPlane = 200f;
            camTarget = Vector3.zero; PlaceCamera(0);

            var sun = FindFirstObjectByType<Light>();
            if (sun == null) sun = new GameObject("Luz").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.color = new Color(1f, .93f, .82f);
            sun.transform.rotation = Quaternion.Euler(55, 20, 0);
            RenderSettings.ambientLight = new Color(.45f, .45f, .55f);
        }

        void PlaceCamera(float dt)
        {
            if (cat != null)
            {
                Vector3 want = cat.transform.position * .6f; // segue o gato, mas mantém o pote no quadro
                camTarget = dt <= 0 ? want : Vector3.Lerp(camTarget, want, 1 - Mathf.Exp(-4f * dt));
            }
            Vector3 shake = Feedback.shake > 0 ? Random.insideUnitSphere * Feedback.shake : Vector3.zero;
            cam.transform.position = camTarget + shake - cam.transform.forward * 40f;
        }

        public Vector3 ClampToRoom(Vector3 p, float r, bool allowOutside = false)
        {
            float m = allowOutside ? -1.5f : 0;
            return new Vector3(Mathf.Clamp(p.x, -RoomW / 2 + r + m, RoomW / 2 - r - m), 0, Mathf.Clamp(p.z, -RoomD / 2 + r + m, RoomD / 2 - r - m));
        }

        // ================== LOOP ==================
        void Update()
        {
            HandleInput();
            float udt = Time.unscaledDeltaTime;
            if (Feedback.shake > 0) Feedback.shake = Mathf.Max(0, Feedback.shake - udt * .8f);
            if (cam != null) PlaceCamera(udt);

            // hitstop: micro-pausa que dá peso aos golpes fortes
            if (state == State.Playing)
            {
                if (Feedback.hitstop > 0) { Feedback.hitstop -= udt; Time.timeScale = .05f; }
                else Time.timeScale = 1;
            }

            // números flutuantes
            for (int i = Feedback.numbers.Count - 1; i >= 0; i--)
            {
                var n = Feedback.numbers[i];
                n.t += Time.deltaTime; n.pos += Vector3.up * Time.deltaTime * 1.2f;
                if (n.t >= n.life) Feedback.numbers.RemoveAt(i); else Feedback.numbers[i] = n;
            }

            // marcadores das entradas acendem com a atividade recente
            foreach (var e in entrances)
            {
                float k = Mathf.Clamp01(1 - (time - e.lastSpawn) / 1.5f);
                e.marker.sharedMaterial.SetColor("_EmissionColor", e.color * (.55f + k * 2.5f) * (.8f + .2f * Mathf.Sin(Time.time * 4)));
                if (Random.value < Time.deltaTime * .3f) e.recent = Mathf.Max(0, e.recent - 1);
            }

            if (state != State.Playing) return;
            time += Time.deltaTime;
            Spawning(Time.deltaTime);
            if (time >= NightDuration && boss == null) Victory();
        }

        void Spawning(float dt)
        {
            if (!bossSpawned && time >= BossTime) { bossSpawned = true; boss = Spawn(InsectType.BarataGigante, 1f); Feedback.Text(boss.transform.position, "BARATA GIGANTE!", new Color(1f, .4f, .3f), 1.8f, 2f); Feedback.Shake(.4f); }
            if (time >= NightDuration) return;
            spawnAcc += (.9f + time / 22f) * dt;
            float hpMult = 1f + time / 150f;
            while (spawnAcc >= 1f)
            {
                spawnAcc -= 1f;
                if (insects.Count >= MaxInsects) break;
                Spawn(PickType(), hpMult);
            }
        }

        InsectType PickType()
        {
            float t = time;
            var pesos = new (InsectType t, float w)[]
            {
                (InsectType.Formiga, 10f), (InsectType.Saudva, t > 40 ? 4f : 0f), (InsectType.Pernilongo, t > 30 ? 4f : 0f),
                (InsectType.Mosca, t > 75 ? 3.5f : 0f), (InsectType.Varejeira, t > 130 ? 2.5f : 0f), (InsectType.Barata, t > 180 ? 2.5f : 0f),
            };
            float total = 0; foreach (var p in pesos) total += p.w;
            float r = Random.value * total;
            foreach (var p in pesos) { if (r < p.w) return p.t; r -= p.w; }
            return InsectType.Formiga;
        }

        Insect Spawn(InsectType type, float hpMult)
        {
            // pernilongos só pela janela; demais por qualquer entrada
            Entrance e = InsectStats.Of(type).flies ? entrances.Find(x => x.name == "Janela") : entrances[Random.Range(0, entrances.Count)];
            e.lastSpawn = time; e.recent++;
            var ins = new GameObject(type.ToString()).AddComponent<Insect>();
            ins.transform.position = e.outside + new Vector3(Random.Range(-.5f, .5f), 0, Random.Range(-.5f, .5f));
            ins.Init(type, hpMult);
            insects.Add(ins);
            return ins;
        }

        public void Kill(Insect ins)
        {
            insects.Remove(ins); kills++;
            Feedback.Burst(ins.transform.position, ins.st.color, Mathf.Clamp((int)(ins.st.size * 12), 4, 24), 3f, ins.st.size * .18f);
            if (ins.st.xp >= 6) { Feedback.Hitstop(.06f); Feedback.Shake(.15f); }
            DropXp(ins.transform.position, ins.st.xp);
            if (ins == boss) { boss = null; Feedback.Hitstop(.25f); Feedback.Shake(.6f); }
            Destroy(ins.gameObject);
        }

        void DropXp(Vector3 pos, float value)
        {
            var c = value >= 6 ? new Color(1f, .5f, .9f) : value >= 3 ? new Color(.4f, .9f, 1f) : new Color(.45f, 1f, .45f);
            string nome = value >= 6 ? "petisco_atum" : value >= 3 ? "petisco_sardinha" : "petisco_peixe";
            var go = new GameObject("Petisco"); go.transform.position = Flat.XZ(pos);
            if (SpriteActor.Create(go.transform, "Items/" + nome, value >= 6 ? .5f : value >= 3 ? .42f : .34f) == null)
            {
                var bola = Art.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * (.16f + .02f * Mathf.Min(value, 10)), c);
                bola.GetComponent<Renderer>().sharedMaterial = Art.Emissive(c, .6f);
            }
            var p = go.AddComponent<Pickup>(); p.value = value; pickups.Add(p);
        }

        public void AddXp(float v)
        {
            xp += v;
            if (xp >= XpNeeded && state == State.Playing)
            {
                xp -= XpNeeded; level++;
                offer = UpgradeOption.Roll(this, 3);
                if (offer.Count > 0) { state = State.LevelUp; Time.timeScale = 0; Feedback.Text(cat.transform.position, "NÍVEL " + level + "!", new Color(.5f, 1f, .6f), 1.5f, 1.2f); }
            }
        }

        void Choose(int i)
        {
            if (offer == null || i >= offer.Count) return;
            offer[i].apply(); offer = null;
            state = State.Playing; Time.timeScale = 1;
            if (xp >= XpNeeded) AddXp(0);
        }

        public void GameOver(string msg)
        {
            if (state == State.GameOver || state == State.Victory) return;
            endMessage = msg; state = State.GameOver; Time.timeScale = 0;
        }

        void Victory()
        {
            endMessage = $"O sol nasceu! A ração está salva e {cat.catName} dormiu dentro do pote.";
            state = State.Victory; Time.timeScale = 0;
        }

        void ConfirmSelect()
        {
            Selected = Names[selIdx]; selectDone = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        static string StatsText(string n) => n == "Mirio"
            ? "Forte e resistente\nVida 140 · Dano +20%\nVelocidade média-baixa\nCrítico 5% (x1,5)"
            : "Ágil e letal\nVida 85 · Dano -5%\nVelocidade alta\nCrítico 22% (x2,2)";

        void DrawSelect(float s)
        {
            Dim();
            GUI.Label(new Rect(0, Screen.height * .08f, Screen.width, 60 * s), "Escolha seu gato", sBig);
            float w = 300 * s, h = 380 * s, gap = 40 * s, x0 = Screen.width / 2 - (w * Names.Length + gap * (Names.Length - 1)) / 2, y = Screen.height * .2f;
            for (int i = 0; i < Names.Length; i++)
            {
                var rc = new Rect(x0 + i * (w + gap), y, w, h);
                bool sel = i == selIdx;
                var c = GUI.color; GUI.color = sel ? new Color(1, .85f, .3f, .9f) : new Color(1, 1, 1, .25f);
                GUI.DrawTexture(rc, Texture2D.whiteTexture); GUI.color = new Color(.08f, .08f, .12f, .95f);
                GUI.DrawTexture(new Rect(rc.x + 4 * s, rc.y + 4 * s, rc.width - 8 * s, rc.height - 8 * s), Texture2D.whiteTexture); GUI.color = c;
                var tex = Resources.Load<Texture2D>($"Cats/{Names[i]}/front_idle");
                if (tex != null)
                {
                    // mesma escala do jogo: o Mirio aparece um pouco maior que a Shoto
                    CatSprite.Metrics(Names[i], out float ppu, out float pivotY);
                    float k = 190 * s, wpx = tex.width / ppu * k, hpx = tex.height / ppu * k, baseY = rc.y + 190 * s;
                    GUI.DrawTexture(new Rect(rc.center.x - wpx / 2, baseY + pivotY * hpx - hpx, wpx, hpx), tex, ScaleMode.StretchToFill);
                }
                GUI.Label(new Rect(rc.x, rc.y + 196 * s, rc.width, 40 * s), Names[i], sBig);
                GUI.Label(new Rect(rc.x + 10 * s, rc.y + 244 * s, rc.width - 20 * s, 130 * s), StatsText(Names[i]), sSmall);
                if (GUI.Button(rc, GUIContent.none, GUIStyle.none)) { if (selIdx == i) ConfirmSelect(); else selIdx = i; }
            }
            GUI.Label(new Rect(0, Screen.height * .88f, Screen.width, 40 * s), "← → trocam · Enter / Espaço / A confirma · clique duplo também", sSmall);
        }

        void HandleInput()
        {
            var kb = Keyboard.current; var gp = Gamepad.current;
            if ((kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.startButton.wasPressedThisFrame))
            {
                if (state == State.Playing) { state = State.Paused; Time.timeScale = 0; }
                else if (state == State.Paused) { state = State.Playing; Time.timeScale = 1; }
            }
            if (kb != null && kb.tabKey.wasPressedThisFrame && (state == State.Playing || state == State.Paused))
            { selectDone = false; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
            if (state == State.Select)
            {
                bool l = (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) || (gp != null && gp.dpad.left.wasPressedThisFrame);
                bool r = (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) || (gp != null && gp.dpad.right.wasPressedThisFrame);
                if (l) selIdx = (selIdx + Names.Length - 1) % Names.Length;
                if (r) selIdx = (selIdx + 1) % Names.Length;
                if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame)) ConfirmSelect();
                return;
            }
            if (state == State.LevelUp && kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Choose(2);
            }
            if ((state == State.GameOver || state == State.Victory) && kb != null && kb.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        // ================== HUD (provisório, IMGUI) ==================
        GUIStyle sLabel, sBig, sBtn, sSmall, sNum;

        Vector2 ToGui(Vector3 world, out bool visible)
        {
            var sp = cam.WorldToScreenPoint(world);
            visible = sp.z > 0;
            return new Vector2(sp.x, Screen.height - sp.y);
        }

        void OnGUI()
        {
            if (cat == null || bowl == null || cam == null) return;
            float s = Screen.height / 720f;
            if (sLabel == null)
            {
                sLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                sSmall = new GUIStyle(GUI.skin.label) { wordWrap = true };
                sBig = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = true };
                sBtn = new GUIStyle(GUI.skin.button) { wordWrap = true, alignment = TextAnchor.UpperLeft, padding = new RectOffset(12, 12, 10, 10) };
                sNum = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            sLabel.fontSize = (int)(18 * s); sSmall.fontSize = (int)(14 * s); sBig.fontSize = (int)(30 * s); sBtn.fontSize = (int)(16 * s);
            sLabel.normal.textColor = sBig.normal.textColor = sSmall.normal.textColor = Color.white;

            // rótulos das entradas
            foreach (var e in entrances)
            {
                var g = ToGui(e.inside + Vector3.up * .6f, out bool vis); if (!vis) continue;
                bool ativo = time - e.lastSpawn < 1.5f;
                Shadowed(new Rect(g.x - 60 * s, g.y - 12 * s, 120 * s, 24 * s), ativo ? $"! {e.name} !" : e.name, ativo ? e.color : Color.Lerp(Color.white, e.color, .6f), (int)(20 * s));
            }

            // barras sobre o gato e o pote
            var gc = ToGui(cat.transform.position + Vector3.up * 1.1f, out _);
            Bar(new Rect(gc.x - 30 * s, gc.y, 60 * s, 7 * s), cat.hp / cat.maxHp, new Color(.95f, .3f, .3f), "");
            var gb = ToGui(bowl.transform.position + Vector3.up * 1.2f, out _);
            Bar(new Rect(gb.x - 45 * s, gb.y, 90 * s, 9 * s), bowl.hp / bowl.maxHp, new Color(.95f, .65f, .25f), "");
            foreach (var b in barricades)
            {
                var gg = ToGui(b.transform.position + Vector3.up * .8f, out _);
                Bar(new Rect(gg.x - 25 * s, gg.y, 50 * s, 5 * s), b.hp / b.maxHp, new Color(.9f, .5f, .75f), "");
            }

            // números de dano
            foreach (var n in Feedback.numbers)
            {
                var g = ToGui(n.pos, out bool vis); if (!vis) continue;
                float k = n.t / n.life;
                var c = n.color; c.a = 1 - k * k;
                float pop = 1 + .4f * Mathf.Clamp01(1 - n.t * 8);
                Shadowed(new Rect(g.x - 120, g.y - 20, 240, 40), n.text, c, (int)(16 * s * n.size * pop));
            }

            // HUD fixo
            Bar(new Rect(16 * s, 12 * s, 260 * s, 18 * s), cat.hp / cat.maxHp, new Color(.9f, .3f, .3f), $"{cat.catName}  {Mathf.CeilToInt(cat.hp)}/{cat.maxHp}");
            Bar(new Rect(16 * s, 36 * s, 260 * s, 18 * s), bowl.hp / bowl.maxHp, new Color(.85f, .6f, .25f), $"Ração  {Mathf.CeilToInt(bowl.hp)}/{bowl.maxHp}");
            Bar(new Rect(0, Screen.height - 14 * s, Screen.width, 14 * s), xp / XpNeeded, new Color(.4f, .9f, .5f), "");
            float rest = Mathf.Max(0, NightDuration - time);
            Shadowed(new Rect(Screen.width / 2 - 150 * s, 8 * s, 300 * s, 36 * s), $"Noite  {(int)rest / 60}:{(int)rest % 60:00}", Color.white, (int)(28 * s));
            GUI.Label(new Rect(Screen.width - 230 * s, 12 * s, 220 * s, 24 * s), $"Nível {level}   Insetos: {kills}", sLabel);
            float y = 62 * s;
            foreach (var w in cat.weapons) { GUI.Label(new Rect(16 * s, y, 320 * s, 22 * s), $"• {w.Name}  nv {w.level}", sSmall); y += 20 * s; }

            if (Time.time - bowl.lastHit < .3f && Flat.Dist(cat.transform.position, bowl.transform.position) > 4f && Mathf.Sin(Time.time * 10) > 0)
                Shadowed(new Rect(0, 52 * s, Screen.width, 30 * s), "A RAÇÃO ESTÁ SENDO ATACADA!", new Color(1f, .5f, .3f), (int)(20 * s));

            if (boss != null)
                Bar(new Rect(Screen.width / 2 - 250 * s, 48 * s, 500 * s, 16 * s), boss.hp / boss.st.hp, new Color(.6f, .2f, .15f), "Barata Gigante");

            if (state == State.Select) DrawSelect(s);
            else if (state == State.LevelUp && offer != null)
            {
                Dim();
                GUI.Label(new Rect(0, Screen.height * .18f, Screen.width, 50 * s), $"Nível {level}! Escolha uma melhoria (1, 2 ou 3)", sBig);
                float w = 300 * s, h = 160 * s, gap = 20 * s, x0 = Screen.width / 2 - (w * offer.Count + gap * (offer.Count - 1)) / 2;
                var opcoes = offer;
                for (int i = 0; i < opcoes.Count; i++)
                    if (GUI.Button(new Rect(x0 + i * (w + gap), Screen.height * .32f, w, h), $"[{i + 1}]  {opcoes[i].title}\n\n{opcoes[i].desc}", sBtn)) { Choose(i); break; }
            }
            else if (state == State.Paused) { Dim(); GUI.Label(new Rect(0, Screen.height * .4f, Screen.width, 60 * s), "Pausado — Esc volta · Tab troca de gato", sBig); }
            else if (state == State.GameOver || state == State.Victory)
            {
                Dim();
                GUI.Label(new Rect(Screen.width * .1f, Screen.height * .25f, Screen.width * .8f, 140 * s), (state == State.Victory ? "VITÓRIA!\n" : "FIM DE JOGO\n") + endMessage, sBig);
                GUI.Label(new Rect(0, Screen.height * .6f, Screen.width, 100 * s), $"Tempo: {(int)time / 60}:{(int)time % 60:00}   Insetos: {kills}   Nível: {level}\nAperte R para jogar de novo", sBig);
            }
        }

        void Shadowed(Rect r, string text, Color c, int size)
        {
            sNum.fontSize = Mathf.Max(8, size);
            sNum.normal.textColor = new Color(0, 0, 0, c.a * .8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, sNum);
            sNum.normal.textColor = c;
            GUI.Label(r, text, sNum);
        }

        static void Dim()
        {
            var c = GUI.color; GUI.color = new Color(0, 0, 0, .65f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = c;
        }

        void Bar(Rect r, float k, Color col, string label)
        {
            var c = GUI.color;
            GUI.color = new Color(0, 0, 0, .6f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = col; GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(k), r.height), Texture2D.whiteTexture);
            GUI.color = c;
            if (!string.IsNullOrEmpty(label)) GUI.Label(new Rect(r.x + 6, r.y - 3, r.width, r.height + 6), label, sSmall);
        }
    }

    /// <summary>Opções de level up.</summary>
    public class UpgradeOption
    {
        public string title, desc; public Action apply;

        public static List<UpgradeOption> Roll(GameManager gm, int n)
        {
            var cat = gm.cat; var pool = new List<UpgradeOption>();
            var todas = new Func<Weapon>[] { () => new Patada(), () => new BolaDePelo(), () => new Ronronar(), () => new DerrubarCopo(), () => new Miado(), () => new Almofadas() };
            foreach (var make in todas)
            {
                var proto = make();
                var tem = cat.weapons.Find(w => w.GetType() == proto.GetType());
                if (tem == null && cat.weapons.Count < 6) pool.Add(new UpgradeOption { title = $"NOVA: {proto.Name}", desc = proto.Describe(1), apply = () => cat.weapons.Add(proto) });
                else if (tem != null && tem.level < Weapon.MaxLevel) pool.Add(new UpgradeOption { title = $"{tem.Name} → nv {tem.level + 1}", desc = tem.Describe(tem.level + 1), apply = () => tem.level++ });
            }
            pool.Add(new UpgradeOption { title = "Unhas Afiadas", desc = "+15% de dano em tudo.", apply = () => cat.damageMult *= 1.15f });
            pool.Add(new UpgradeOption { title = "Patas Ligeiras", desc = "+10% de velocidade.", apply = () => cat.speed *= 1.1f });
            pool.Add(new UpgradeOption { title = "Sete Vidas", desc = "+20 de vida máxima e cura 30.", apply = () => { cat.maxHp += 20; cat.Heal(30); } });
            pool.Add(new UpgradeOption { title = "Faro Apurado", desc = "Atrai petiscos de mais longe.", apply = () => cat.pickupRadius += .6f });
            pool.Add(new UpgradeOption { title = "Agilidade Felina", desc = "Habilidades 8% mais frequentes.", apply = () => cat.cooldownMult *= .92f });
            pool.Add(new UpgradeOption { title = "Ração Premium", desc = "Repõe 40 de ração e +20 de capacidade do pote.", apply = () => { gm.bowl.maxHp += 20; gm.bowl.Repair(40); } });
            var result = new List<UpgradeOption>();
            while (result.Count < n && pool.Count > 0) { int i = Random.Range(0, pool.Count); result.Add(pool[i]); pool.RemoveAt(i); }
            return result;
        }
    }
}
