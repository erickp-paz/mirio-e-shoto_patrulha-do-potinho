using System.Collections.Generic;
using UnityEngine;

namespace MirioShoto
{
    public enum InsectType { Formiga, Pernilongo, Grilo, Barata, Besouro, BesouroRinoceronte }

    /// <summary>Tabela de inimigos — balancear o jogo = editar números aqui.</summary>
    public struct InsectStats
    {
        public string nome; public float hp, speed, damage, size; public int xp;
        public bool targetsBowl, flies, horn; public Color color; public float knockResist;

        public static InsectStats Of(InsectType t) => t switch
        {
            InsectType.Formiga => new InsectStats { nome = "Formiga", hp = 3, speed = 1.7f, damage = 4, size = .32f, xp = 1, targetsBowl = true, color = new Color(.6f, .14f, .1f) },
            InsectType.Pernilongo => new InsectStats { nome = "Pernilongo", hp = 2, speed = 3.0f, damage = 4, size = .3f, xp = 1, flies = true, color = new Color(.4f, .42f, .48f) },
            InsectType.Grilo => new InsectStats { nome = "Grilo", hp = 7, speed = 1.4f, damage = 7, size = .5f, xp = 2, color = new Color(.35f, .6f, .2f) },
            InsectType.Barata => new InsectStats { nome = "Barata", hp = 12, speed = 2.5f, damage = 6, size = .55f, xp = 3, targetsBowl = true, color = new Color(.45f, .25f, .1f), knockResist = .3f },
            InsectType.Besouro => new InsectStats { nome = "Besouro", hp = 32, speed = 1.15f, damage = 12, size = .7f, xp = 6, color = new Color(.15f, .25f, .45f), knockResist = .8f },
            _ => new InsectStats { nome = "Besouro-Rinoceronte", hp = 650, speed = 1.0f, damage = 25, size = 1.6f, xp = 60, horn = true, color = new Color(.32f, .17f, .1f), knockResist = .97f },
        };
    }

    public class Insect : MonoBehaviour
    {
        public InsectType type; public InsectStats st; public float hp;
        float stun, flash, hopT, chargeT, wobble, animT;
        Vector3 knock, chargeDir; bool charging;
        Transform model; List<Renderer> rends; readonly List<Color> baseColors = new();

        public float Radius => st.size * .45f;
        public bool IsBoss => type == InsectType.BesouroRinoceronte;

        public void Init(InsectType t, float hpMult)
        {
            type = t; st = InsectStats.Of(t); hp = st.hp * hpMult; st.hp = hp;
            model = Art.BuildInsect(transform, st, out rends);
            foreach (var r in rends) baseColors.Add(r.sharedMaterial.GetColor("_BaseColor"));
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
                case InsectType.Grilo:
                    hopT -= dt;
                    if (hopT < 0) { spd *= 6; hopY = Mathf.Sin(Mathf.Clamp01(-hopT / .2f) * Mathf.PI) * .5f; if (hopT < -.2f) hopT = Random.Range(.9f, 1.5f); }
                    break;
                case InsectType.BesouroRinoceronte:
                    chargeT -= dt;
                    if (!charging && chargeT < 0) { charging = true; chargeDir = dir; chargeT = 1.1f; Feedback.Ring(pos, new Color(1, .3f, .2f), .5f, 1.6f, .35f); Feedback.Shake(.2f); }
                    if (charging) { dir = chargeDir; spd *= 5f; if (chargeT < 0) { charging = false; chargeT = Random.Range(3f, 4.5f); } }
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
            model.localPosition = new Vector3(0, st.flies ? Mathf.Sin(animT * .4f) * .1f : hopY + Mathf.Abs(Mathf.Sin(animT)) * .02f, 0);

            if (st.targetsBowl && Flat.Dist(next, bowlPos) < Radius + FoodBowl.Radius)
                gm.bowl.Damage(st.damage * .15f * dt);
            else if (!st.targetsBowl && Flat.Dist(next, catPos) < Radius + Cat.Radius)
                gm.cat.TakeDamage(st.damage);

            if (flash > 0)
            {
                flash -= dt;
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
