using System.Collections.Generic;
using UnityEngine;

namespace MirioShoto
{
    /// <summary>Base das habilidades automáticas (5 níveis cada).</summary>
    public abstract class Weapon
    {
        public const int MaxLevel = 5;
        public const float CritChance = .1f;
        public int level = 1;
        protected float timer;
        public abstract string Name { get; }
        public abstract string Describe(int lvl);
        public abstract void Tick(Cat cat, float dt);

        protected bool Ready(Cat cat, float dt, float cd)
        {
            timer -= dt; if (timer > 0) return false;
            timer = cd * cat.cooldownMult; return true;
        }

        protected static Insect Nearest(Vector3 from, float maxDist)
        {
            Insect best = null; float bd = maxDist;
            foreach (var i in GameManager.I.insects) { float d = Flat.Dist(i.transform.position, from); if (d < bd) { bd = d; best = i; } }
            return best;
        }

        /// <summary>Dano em área com chance de crítico (dobro, número amarelo).</summary>
        protected static void Area(Vector3 c, float r, float dmg, float knock = .5f, float stun = 0)
        {
            var list = GameManager.I.insects;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (i >= list.Count) continue;
                var ins = list[i];
                if (Flat.Dist(c, ins.transform.position) < r + ins.Radius)
                {
                    bool crit = Random.value < CritChance;
                    ins.Hit(crit ? dmg * 2 : dmg, c, knock, stun, crit);
                }
            }
        }
    }

    public class Patada : Weapon
    {
        public override string Name => "Patada";
        float Cd => .85f - .07f * (level - 1);
        float Dmg => 4 + 2.5f * (level - 1);
        public override string Describe(int l) => l == 1 ? "Golpeia o inseto mais próximo, com respingo." : "Mais dano e mais rápida.";
        public override void Tick(Cat cat, float dt)
        {
            if (!Ready(cat, dt, Cd)) return;
            var t = Nearest(cat.transform.position, 1.8f + .1f * level);
            if (t == null) { timer = .08f; return; }
            Vector3 p = t.transform.position;
            Area(p, .7f + .08f * level, Dmg * cat.RollDmg(), .8f);
            Feedback.Ring(p, new Color(1f, .95f, .8f), .2f, .8f, .12f);
        }
    }

    public class BolaDePelo : Weapon
    {
        public override string Name => "Bola de Pelo";
        int Count => 1 + (level >= 3 ? 1 : 0) + (level >= 5 ? 1 : 0);
        public override string Describe(int l) => l == 1 ? "Cospe uma bola de pelo que atravessa insetos." : "Mais dano, atravessa mais, mais bolas.";
        public override void Tick(Cat cat, float dt)
        {
            if (!Ready(cat, dt, 1.25f)) return;
            Vector3 p = cat.transform.position;
            var t = Nearest(p, 10f);
            Vector3 dir = t != null ? Flat.XZ(t.transform.position - p).normalized : cat.facing;
            for (int i = 0; i < Count; i++)
                Projectile.Fire(p, Quaternion.Euler(0, (i - (Count - 1) / 2f) * 15f, 0) * dir, 10f, (5 + 2f * level) * cat.RollDmg(), 1 + level / 2);
        }
    }

    public class Projectile : MonoBehaviour
    {
        Vector3 vel; float dmg, life = 2f; int pierce; readonly HashSet<Insect> hits = new();
        public static void Fire(Vector3 pos, Vector3 dir, float speed, float dmg, int pierce)
        {
            var go = Art.Prim(PrimitiveType.Sphere, null, Flat.XZ(pos) + Vector3.up * .45f, Vector3.one * .25f, new Color(.95f, .8f, .6f));
            var p = go.AddComponent<Projectile>(); p.vel = dir * speed; p.dmg = dmg; p.pierce = pierce;
        }
        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.state != GameManager.State.Playing) return;
            float dt = Time.deltaTime;
            transform.position += vel * dt; transform.Rotate(720 * dt, 0, 0); life -= dt;
            var list = gm.insects;
            for (int i = list.Count - 1; i >= 0 && pierce > 0; i--)
            {
                if (i >= list.Count) continue;
                var ins = list[i]; if (hits.Contains(ins)) continue;
                if (Flat.Dist(transform.position, ins.transform.position) < .2f + ins.Radius)
                {
                    hits.Add(ins); pierce--;
                    bool crit = Random.value < Weapon.CritChance;
                    ins.Hit(crit ? dmg * 2 : dmg, transform.position - vel.normalized, .5f, 0, crit);
                }
            }
            if (pierce <= 0 || life <= 0) { Feedback.Burst(transform.position, new Color(.95f, .8f, .6f), 4, 2f, .05f); Destroy(gameObject); }
        }
    }

    public class Ronronar : Weapon
    {
        public override string Name => "Ronronar";
        float R => 1.3f + .25f * level;
        readonly List<Transform> dots = new();
        public override string Describe(int l) => l == 1 ? "Ondas de ronrono machucam insetos por perto." : "Ondas maiores e mais fortes.";
        public override void Tick(Cat cat, float dt)
        {
            if (dots.Count == 0)
                for (int i = 0; i < 14; i++) dots.Add(Art.Prim(PrimitiveType.Sphere, cat.transform, Vector3.zero, Vector3.one * .1f, new Color(1f, .85f, .35f)).transform);
            for (int i = 0; i < dots.Count; i++)
            {
                float a = Time.time * 1.5f + i * Mathf.PI * 2 / dots.Count;
                float r = R * (.9f + .1f * Mathf.Sin(Time.time * 8 + i));
                dots[i].position = cat.transform.position + new Vector3(Mathf.Cos(a) * r, .08f, Mathf.Sin(a) * r);
            }
            if (!Ready(cat, dt, .5f)) return;
            Area(cat.transform.position, R, (1 + .7f * level) * cat.RollDmg(), .15f);
        }
    }

    public class DerrubarCopo : Weapon
    {
        public override string Name => "Derrubar Copo";
        public override string Describe(int l) => l == 1 ? "Um copo d'água cai sobre os insetos (dano em área + atordoa)." : "Mais copos, respingo maior.";
        public override void Tick(Cat cat, float dt)
        {
            if (!Ready(cat, dt, 3.6f - .35f * level)) return;
            var list = GameManager.I.insects;
            if (list.Count == 0) { timer = .3f; return; }
            int n = level >= 4 ? 2 : 1;
            for (int i = 0; i < n; i++)
                FallingCup.Drop(list[Random.Range(0, list.Count)].transform.position, 1.3f + .15f * level, (10 + 4 * level) * cat.RollDmg());
        }
    }

    public class FallingCup : MonoBehaviour
    {
        float t, r, dmg; Transform cup, shadow;
        public static void Drop(Vector3 pos, float r, float dmg)
        {
            var go = new GameObject("CopoCaindo"); go.transform.position = Flat.XZ(pos);
            var c = go.AddComponent<FallingCup>(); c.r = r; c.dmg = dmg;
            c.shadow = Art.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, .02f, 0), new Vector3(.2f, .005f, .2f), new Color(.25f, .2f, .18f)).transform;
            c.cup = Art.Prim(PrimitiveType.Cylinder, go.transform, new Vector3(0, 4, 0), new Vector3(.35f, .3f, .35f), new Color(.75f, .9f, 1f)).transform;
        }
        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.state != GameManager.State.Playing) return;
            t += Time.deltaTime; float k = t / .55f;
            shadow.localScale = new Vector3(Mathf.Lerp(.2f, r * 2, k), .005f, Mathf.Lerp(.2f, r * 2, k));
            cup.localPosition = new Vector3(0, Mathf.Lerp(4f, .3f, k * k), 0);
            cup.Rotate(300 * Time.deltaTime, 0, 0);
            if (k >= 1)
            {
                Feedback.Ring(transform.position, new Color(.5f, .8f, 1f), .3f, r, .3f);
                Feedback.Burst(transform.position, new Color(.55f, .8f, 1f), 18, 4.5f, .08f);
                Feedback.Shake(.12f);
                var list = gm.insects;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (i >= list.Count) continue;
                    var ins = list[i];
                    if (Flat.Dist(transform.position, ins.transform.position) < r + ins.Radius) ins.Hit(dmg, transform.position, .9f, .6f);
                }
                Destroy(gameObject);
            }
        }
    }

    public class Miado : Weapon
    {
        public override string Name => "Miado Assustador";
        public override string Describe(int l) => l == 1 ? "MIAU! Empurra e atordoa todos os insetos por perto." : "Mais alcance e mais frequente.";
        public override void Tick(Cat cat, float dt)
        {
            if (!Ready(cat, dt, 6f - .6f * level)) return;
            float R = 2.4f + .3f * level;
            Vector3 p = cat.transform.position;
            Area(p, R, (2 + level) * cat.RollDmg(), 2.2f, .7f);
            Feedback.Ring(p, new Color(1f, 1f, .55f), .4f, R, .3f);
            Feedback.Text(p, "MIAU!", new Color(1f, 1f, .6f), 1.6f, .6f);
            Feedback.Shake(.08f);
        }
    }

    /// <summary>Almofadas: barricadas automáticas nas entradas mais movimentadas (e no pote, a partir do nv 3).</summary>
    public class Almofadas : Weapon
    {
        public override string Name => "Barricada de Pelúcias";
        float Hp => 40 + 20 * level;
        public override string Describe(int l) => l == 1 ? "Posiciona uma pelúcia na entrada mais movimentada. Insetos precisam roê-la para passar." : l == 3 ? "Também protege um lado do pote de ração." : "Pelúcias mais resistentes e frequentes.";
        public override void Tick(Cat cat, float dt)
        {
            if (!Ready(cat, dt, 13f - level)) return;
            var gm = GameManager.I;
            if (gm.barricades.Count >= 2 + level) return;
            var e = gm.BusiestEntrance();
            Barricade.Place(e.inside, Hp, .9f);
            if (level >= 3)
            {
                Vector3 bowl = gm.bowl.transform.position;
                Vector3 dir = Flat.XZ(e.inside - bowl).normalized;
                Barricade.Place(bowl + dir * 1.6f, Hp * .7f, .6f);
            }
        }
    }
}
