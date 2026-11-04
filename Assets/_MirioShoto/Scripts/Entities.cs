using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MirioShoto
{
    public static class Flat
    {
        public static Vector3 XZ(Vector3 v) => new Vector3(v.x, 0, v.z);
        public static float Dist(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }
    }

    /// <summary>O gato jogável (Mirio por enquanto; Shoto entra como segundo personagem).</summary>
    public class Cat : MonoBehaviour
    {
        public string catName = "Mirio";
        public float maxHp = 100, hp = 100, speed = 5f, pickupRadius = 1.8f;
        public float damageMult = 1f, cooldownMult = 1f, critChance = .05f, critMult = 1.5f;
        public Vector3 facing = Vector3.forward;
        public readonly List<Weapon> weapons = new();
        public const float Radius = .35f;
        public float lastHurt = -10;

        Transform model; List<Renderer> rends; readonly List<Color> baseColors = new();
        CatSprite catSprite; SpriteRenderer spriteR; // gato em pixel art (se os sprites existirem)
        Vector3 vel; float invuln, flash, animT;

        void Awake()
        {
            if (GameManager.I != null) catName = GameManager.Selected;
            if (catName == "Shoto") { maxHp = hp = 85; speed = 6.3f; damageMult = .95f; critChance = .22f; critMult = 2.2f; }
            else { maxHp = hp = 140; speed = 4.7f; damageMult = 1.2f; critChance = .05f; critMult = 1.5f; }
            if (CatSprite.Available(catName))
            {
                var go = new GameObject("Sprite"); go.transform.SetParent(transform, false);
                catSprite = go.AddComponent<CatSprite>(); catSprite.Init(catName);
                model = go.transform; spriteR = catSprite.sr; rends = new List<Renderer>();
            }
            else
            {
                model = Art.BuildCat(transform, new Color(1f, .6f, .22f), new Color(.85f, .42f, .1f), new Color(.15f, .4f, .2f), out rends);
                foreach (var r in rends) baseColors.Add(r.material.GetColor("_BaseColor"));
            }
        }

        /// <summary>Multiplicador de dano de um ataque, com chance de crítico.</summary>
        public float RollDmg()
        {
            if (Random.value >= critChance) return damageMult;
            Feedback.Text(transform.position + Vector3.up * 1.4f, "CRÍTICO!", new Color(1f, .85f, .2f), 1.1f, .7f);
            return damageMult * critMult;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.state != GameManager.State.Playing) return;
            float dt = Time.deltaTime;

            Vector2 input = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
            }
            if (Gamepad.current != null) input += Gamepad.current.leftStick.ReadValue();
            if (input.sqrMagnitude > 1) input.Normalize();

            // a câmera isométrica está girada 45°: "para cima" na tela = diagonal no mundo
            Vector3 wish = Quaternion.Euler(0, GameManager.CamYaw, 0) * new Vector3(input.x, 0, input.y) * speed;
            // aceleração/desaceleração: tira a sensação "travada" de parar e arrancar instantâneo
            vel = Vector3.MoveTowards(vel, wish, (wish.sqrMagnitude > .01f ? 40f : 30f) * dt);
            transform.position = gm.ClampToRoom(transform.position + vel * dt, Radius);

            float moving = Mathf.Clamp01(vel.magnitude / speed);
            if (vel.sqrMagnitude > .05f)
            {
                facing = vel.normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), 16f * dt);
            }
            // animação procedural: quica e estica levemente enquanto corre
            animT += dt * Mathf.Lerp(4f, 16f, moving);
            float bob = Mathf.Abs(Mathf.Sin(animT)) * .07f * moving;
            float squash = 1 + Mathf.Sin(animT * 2) * .05f * moving;
            if (catSprite != null)
            {
                // sprite 2D: a animação de passos já vem dos quadros, então só um quique leve
                model.localPosition = new Vector3(0, bob * .4f, 0);
                catSprite.Tick(vel, moving, dt);
            }
            else
            {
                model.localPosition = new Vector3(0, bob, 0);
                model.localScale = new Vector3(1 / squash, squash, 1);
            }

            foreach (var w in weapons) w.Tick(this, dt);

            if (invuln > 0) invuln -= dt;
            if (flash > 0) flash -= dt;
            if (spriteR != null)
            {
                spriteR.color = flash > 0 ? Color.Lerp(Color.white, new Color(1f, .3f, .3f), flash / .15f) : Color.white;
                spriteR.enabled = invuln <= 0 || Mathf.Sin(Time.time * 50) > -.3f;
            }
            for (int i = 0; i < rends.Count; i++)
            {
                var c = flash > 0 ? Color.Lerp(baseColors[i], new Color(1, .2f, .2f), flash / .15f) : baseColors[i];
                rends[i].material.SetColor("_BaseColor", c);
                rends[i].enabled = invuln <= 0 || Mathf.Sin(Time.time * 50) > -.3f;
            }
        }

        public void TakeDamage(float d)
        {
            if (invuln > 0) return;
            hp -= d; invuln = .5f; flash = .15f; lastHurt = Time.time;
            Feedback.Number(transform.position, d, new Color(1f, .35f, .35f), 1.1f);
            Feedback.Shake(.15f);
            if (hp <= 0) { hp = 0; GameManager.I.GameOver($"{catName} ficou exausto e foi dormir..."); }
        }

        public void Heal(float v)
        {
            hp = Mathf.Min(maxHp, hp + v);
            Feedback.Text(transform.position, "+" + Mathf.CeilToInt(v), new Color(.4f, 1f, .5f));
        }
    }

    /// <summary>Pote de ração: o monte de ração diminui/aumenta de verdade, pisca quando atacado.</summary>
    public class FoodBowl : MonoBehaviour
    {
        public float maxHp = 150, hp = 150;
        public const float Radius = .65f;
        public float lastHit = -10;
        Transform mound; Renderer bowlR; float pending, pendingT;
        static readonly Color bowlColor = new(.2f, .45f, .9f);

        void Awake()
        {
            var b = Art.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, .12f, 0), new Vector3(1.25f, .12f, 1.25f), bowlColor, default, true);
            bowlR = b.GetComponent<Renderer>();
            Art.Prim(PrimitiveType.Cylinder, transform, new Vector3(0, .2f, 0), new Vector3(1.05f, .05f, 1.05f), new Color(.12f, .25f, .55f));
            mound = Art.Prim(PrimitiveType.Sphere, transform, new Vector3(0, .25f, 0), Vector3.one, new Color(.62f, .38f, .18f)).transform;
            for (int i = 0; i < 7; i++) // grãos de ração por cima do monte
                Art.Prim(PrimitiveType.Sphere, mound, new Vector3(Random.Range(-.3f, .3f), .35f, Random.Range(-.3f, .3f)), Vector3.one * .18f, new Color(.5f, .28f, .12f));
        }

        void Update()
        {
            float k = Mathf.Clamp01(hp / maxHp);
            var target = new Vector3(Mathf.Lerp(.25f, .95f, k), Mathf.Lerp(.05f, .55f, k), Mathf.Lerp(.25f, .95f, k));
            mound.localScale = Vector3.Lerp(mound.localScale, target, Time.unscaledDeltaTime * 10f);
            bool atacado = Time.time - lastHit < .2f;
            bowlR.material.SetColor("_BaseColor", atacado && Mathf.Sin(Time.time * 30) > 0 ? new Color(1f, .3f, .25f) : bowlColor);

            // agrupa o dano contínuo em números legíveis a cada meio segundo
            pendingT += Time.deltaTime;
            if (pendingT > .5f && pending > 0)
            {
                Feedback.Text(transform.position + Vector3.up * .3f, "-" + Mathf.CeilToInt(pending), new Color(1f, .6f, .2f), 1.1f, .8f);
                pending = 0; pendingT = 0;
            }
        }

        public void Damage(float d)
        {
            hp -= d; pending += d; lastHit = Time.time;
            if (Random.value < .05f) Feedback.Burst(transform.position + Vector3.up * .3f, new Color(.55f, .32f, .15f), 1, 2f, .06f);
            if (hp <= 0) { hp = 0; GameManager.I.GameOver("Os insetos levaram toda a ração!"); }
        }

        public void Repair(float v)
        {
            hp = Mathf.Min(maxHp, hp + v);
            Feedback.Text(transform.position, "+" + Mathf.CeilToInt(v) + " ração", new Color(.5f, 1f, .5f), 1.2f, 1.2f);
            Feedback.Burst(transform.position + Vector3.up, new Color(.62f, .38f, .18f), 14, 3f, .07f);
        }
    }

    /// <summary>Petisco (experiência) que o inseto deixa cair.</summary>
    public class Pickup : MonoBehaviour
    {
        public float value = 1; bool magnet; float t;
        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.state != GameManager.State.Playing) return;
            t += Time.deltaTime;
            var cat = gm.cat.transform.position;
            float d = Flat.Dist(cat, transform.position);
            if (d < gm.cat.pickupRadius) magnet = true;
            var p = transform.position;
            if (magnet) p = Vector3.MoveTowards(p, cat, 12f * Time.deltaTime);
            p.y = .15f + Mathf.Sin(t * 5) * .05f;
            transform.position = p;
            transform.Rotate(0, 180 * Time.deltaTime, 0);
            if (d < .4f) { gm.AddXp(value); gm.pickups.Remove(this); Destroy(gameObject); }
        }
    }

    /// <summary>Barricada de pelúcia (bicho sorteado a cada partida): bloqueia insetos até ser roída.</summary>
    public class Barricade : MonoBehaviour
    {
        public float maxHp = 60, hp = 60, radius = .8f;
        Renderer r; SpriteRenderer spr; Color baseC = new(.75f, .35f, .55f); float flash;
        static readonly string[] Plushes = { "urso", "coelho", "dino", "pinguim", "polvo", "pato" };
        static int plushSeed = -1;
        public static void NewRun() { plushSeed = -1; }

        public static Barricade Place(Vector3 pos, float hp, float radius)
        {
            var go = new GameObject("Barricada");
            go.transform.position = Flat.XZ(pos);
            var b = go.AddComponent<Barricade>(); b.maxHp = b.hp = hp; b.radius = radius;
            if (plushSeed < 0) plushSeed = Random.Range(0, Plushes.Length);   // um bicho diferente por partida (e por barricada, em ciclo)
            var sa = SpriteActor.Create(go.transform, "Plush/pelucia_" + Plushes[(plushSeed + GameManager.I.barricades.Count) % Plushes.Length], radius * 1.5f);
            if (sa != null) b.spr = sa.sr;
            else { var cube = Art.Prim(PrimitiveType.Cube, go.transform, new Vector3(0, .25f, 0), new Vector3(radius * 1.8f, .5f, radius * 1.1f), b.baseC, new Vector3(0, Random.Range(0, 360), 0), true); b.r = cube.GetComponent<Renderer>(); }
            GameManager.I.barricades.Add(b);
            Feedback.Burst(go.transform.position + Vector3.up * .4f, new Color(1f, .9f, .95f), 8, 2.5f, .07f);
            return b;
        }

        void Update()
        {
            if (flash > 0) flash -= Time.deltaTime;
            if (spr != null) { float k = Mathf.Lerp(.55f, 1f, hp / maxHp); spr.color = Color.Lerp(new Color(k, k, k, 1), new Color(1f, .4f, .4f), Mathf.Clamp01(flash / .1f)); return; }
            r.material.SetColor("_BaseColor", Color.Lerp(baseC * Mathf.Lerp(.5f, 1f, hp / maxHp), Color.white, flash / .1f));
        }

        public void Damage(float d)
        {
            hp -= d; flash = .1f;
            if (hp <= 0)
            {
                Feedback.Burst(transform.position + Vector3.up * .4f, new Color(1f, .95f, .95f), 16, 3.5f, .09f);
                GameManager.I.barricades.Remove(this); Destroy(gameObject);
            }
        }
    }
}
