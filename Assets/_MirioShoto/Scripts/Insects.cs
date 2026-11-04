using System.Collections.Generic;
using UnityEngine;

namespace MirioShoto
{
    public enum InsectType { Formiga, Saudva, Pernilongo, Mosca, Varejeira, Barata, BarataGigante }

    /// <summary>Tabela de inimigos — balancear o jogo = editar números aqui.</summary>
    public struct InsectStats
    {
        public string nome; public float hp, speed, damage, size, sprite; public int xp;
        public bool targetsBowl, flies, horn; public Color color; public float knockResist;

        public static InsectStats Of(InsectType t) => t switch
        {
            InsectType.Formiga => new InsectStats { nome = "Formiguinha", hp = 2, speed = 1.0f, damage = 3, size = .09f, sprite = .26f, xp = 1, targetsBowl = true, color = new Color(.25f, .13f, .1f) },
            InsectType.Saudva => new InsectStats { nome = "Saúva", hp = 8, speed = .9f, damage = 5, size = .15f, sprite = .38f, xp = 2, targetsBowl = true, color = new Color(.66f, .26f, .11f), knockResist = .2f },
            InsectType.Pernilongo => new InsectStats { nome = "Mosquito", hp = 2, speed = 1.7f, damage = 3, size = .15f, sprite = .36f, xp = 1, flies = true, color = new Color(.6f, .55f, .5f) },
            InsectType.Mosca => new InsectStats { nome = "Mosca", hp = 3, speed = 1.4f, damage = 4, size = .17f, sprite = .42f, xp = 2, flies = true, color = new Color(.2f, .2f, .22f) },
            InsectType.Varejeira => new InsectStats { nome = "Varejeira", hp = 9, speed = 1.0f, damage = 6, size = .21f, sprite = .52f, xp = 3, flies = true, color = new Color(.1f, .45f, .5f), knockResist = .3f },
            InsectType.Barata => new InsectStats { nome = "Barata", hp = 12, speed = 1.5f, damage = 6, size = .26f, sprite = .64f, xp = 3, targetsBowl = true, color = new Color(.48f, .2f, .09f), knockResist = .3f },
            _ => new InsectStats { nome = "Barata Gigante", hp = 420, speed = .7f, damage = 14, size = .5f, sprite = 1.25f, xp = 60, horn = true, color = new Color(.5f, .15f, .1f), knockResist = .97f },
        };
    }

    public class Insect : MonoBehaviour
    {
        public InsectType type; public InsectStats st; public float hp;
        float stun, flash, hopT, chargeT, wobble, animT;
        Vector3 knock, chargeDir; bool charging;
        Transform model; List<Renderer> rends; SpriteActor sa; SpriteRenderer spriteR; float lift; readonly List<Color> baseColors = new();

        static string SpriteName(InsectType t) => t switch
        {
            InsectType.Formiga => "formiga", InsectType.Saudva => "sauva", InsectType.Pernilongo => "mosquito",
            InsectType.Mosca => "mosca", InsectType.Varejeira => "varejeira", _ => "barata",
        };

        public float Radius => Mathf.Max(.07f, st.size * .8f);
        public bool IsBoss => type == InsectType.BarataGigante;

        public void Init(InsectType t, float hpMult)
        {
            type = t; st = InsectStats.Of(t); hp = st.hp * hpMult; st.hp = hp;
            sa = SpriteActor.Create(transform, "Insects/" + SpriteName(t), st.sprite);
            if (sa != null) { model = sa.transform; spriteR = sa.sr; rends = new List<Renderer>(); lift = st.flies ? .4f : 0f; if (t == InsectType.BarataGigante) spriteR.color = new Color(1f, .65f, .6f); }
            else { model = Art.BuildInsect(transform, st, out rends); foreach (var r in rends) baseColors.Add(r.sharedMaterial.GetColor("_BaseColor")); }
            wobble = Random.value * 10f; hopT = Random.Range(.3f, 1.2f); chargeT = 3f;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.state != GameManager.State.Playing || hp <= 0) return;
            float dt = Time.deltaTime;
            Vector3 pos = transform.position;
            Vector3 catPos = gm.cat.transform.position, bowlPos = gm.bowl.transform.position;
            Vector3 target = st.targetsBowl ? bowlPos : catPos;
            Vector3 dir = Flat.XZ(target - pos).normalized;
            float spd = st.speed, hopY = 0;

            if (stun > 0) { stun -= dt; spd = 0; }
            else switch (type)
            {
                case InsectType.Pernilongo:
                    dir = (dir + Vector3.Cross(dir, Vector3.up) * Mathf.Sin(Time.time * 6 + wobble) * .8f).normalized; break;
                case InsectType.Mosca:   // voo errático e rápido
                    dir = (dir + Vector3.Cross(dir, Vector3.up) * Mathf.Sin(Time.time * 11 + wobble) * 1.2f).normalized; break;
                case InsectType.Varejeira:   // voo pesado, quase em linha reta
                    dir = (dir + Vector3.Cross(dir, Vector3.up) * Mathf.Sin(Time.time * 2 + wobble) * .35f).normalized; break;
                case InsectType.BarataGigante:
                    chargeT -= dt;
                    if (!charging && chargeT < 0) { charging = true; chargeDir = dir; chargeT = 1.1f; Feedback.Ring(pos, new Color(1, .3f, .2f), .5f, 1.6f, .35f); Feedback.Shake(.2f); }
                    if (charging) { dir = chargeDir; spd *= 3f; if (chargeT < 0) { charging = false; chargeT = Random.Range(3f, 4.5f); } }
                    break;
            }

            // separação entre insetos
            Vector3 sep = Vector3.zero;
            var list = gm.insects;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i]; if (o == this) continue;
                Vector3 d = Flat.XZ(pos - o.transform.position);
                float min = Radius + o.Radius, sq = d.sqrMagnitude;
                if (sq < min * min && sq > .0001f) sep += d.normalized * (min - Mathf.Sqrt(sq));
            }

            Vector3 vel = dir * spd + knock * 8f;
            knock = Vector3.MoveTowards(knock, Vector3.zero, dt * 3f);
            Vector3 next = pos + vel * dt + sep * .5f;

            // barricadas: bloqueiam e são roídas
            foreach (var b in gm.barricades)
            {
                Vector3 d = Flat.XZ(next - b.transform.position);
                float min = b.radius + Radius;
                if (d.sqrMagnitude < min * min)
                {
                    next = b.transform.position + d.normalized * min; next.y = 0;
                    b.Damage(st.damage * .5f * dt);
                    break;
                }
            }

            next = gm.ClampToRoom(next, Radius, allowOutside: true);
            transform.position = next;
            if (vel.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(Flat.XZ(vel));

            // animação: insetos "trotam" e voadores flutuam
            animT += dt * 20f;
            model.localPosition = new Vector3(0, (spriteR != null ? lift : 0f) + (st.flies ? Mathf.Sin(animT * .4f) * .1f : hopY + Mathf.Abs(Mathf.Sin(animT)) * .02f), 0);
            if (sa != null) { sa.Face(vel); sa.roll = vel.sqrMagnitude > .01f ? Mathf.Sin(animT * .5f) * 5f : 0f; }

            if (st.targetsBowl && Flat.Dist(next, bowlPos) < Radius + FoodBowl.Radius)
                gm.bowl.Damage(st.damage * .15f * dt);
            else if (!st.targetsBowl && Flat.Dist(next, catPos) < Radius + Cat.Radius)
                gm.cat.TakeDamage(st.damage);

            if (flash > 0)
            {
                flash -= dt;
                if (spriteR != null) spriteR.color = Color.Lerp(Color.white, new Color(1f, .35f, .35f), Mathf.Clamp01(flash / .1f));
                for (int i = 0; i < rends.Count; i++) rends[i].material.SetColor("_BaseColor", Color.Lerp(baseColors[i], Color.white, flash / .1f));
            }
        }

        public void Hit(float dmg, Vector3 from, float knockback = .5f, float stunTime = 0, bool crit = false)
        {
            if (hp <= 0) return;
            hp -= dmg; flash = .1f;
            Feedback.Number(transform.position, dmg, crit ? new Color(1f, .85f, .2f) : Color.white, crit ? 1.4f : 1f);
            knock += Flat.XZ(transform.position - from).normalized * knockback * (1 - st.knockResist);
            if (stunTime > 0 && !IsBoss) stun = Mathf.Max(stun, stunTime);
            if (IsBoss) Feedback.Shake(.05f);
            if (hp <= 0) GameManager.I.Kill(this);
        }
    }
}
