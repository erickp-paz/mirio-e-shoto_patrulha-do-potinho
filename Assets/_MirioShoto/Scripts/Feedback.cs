using System.Collections.Generic;
using UnityEngine;

namespace MirioShoto
{
    /// <summary>
    /// "Game feel": tudo que faz o golpe parecer um golpe. Números de dano, pedacinhos voando,
    /// tremida de câmera e uma micro-pausa (hitstop) em golpes fortes.
    /// </summary>
    public static class Feedback
    {
        public struct Floating { public Vector3 pos; public string text; public Color color; public float t, life, size; }
        public static readonly List<Floating> numbers = new();
        public static float shake;
        public static float hitstop;

        public static void Number(Vector3 pos, float value, Color color, float size = 1f)
        {
            if (numbers.Count > 120) numbers.RemoveAt(0);
            numbers.Add(new Floating { pos = pos + Vector3.up * .8f + new Vector3(Random.Range(-.25f, .25f), 0, Random.Range(-.25f, .25f)), text = Mathf.CeilToInt(value).ToString(), color = color, life = .7f, size = size });
        }

        public static void Text(Vector3 pos, string text, Color color, float size = 1f, float life = 1f)
        {
            numbers.Add(new Floating { pos = pos + Vector3.up, text = text, color = color, life = life, size = size });
        }

        public static void Shake(float amount) => shake = Mathf.Max(shake, amount);
        public static void Hitstop(float seconds) => hitstop = Mathf.Max(hitstop, seconds);

        /// <summary>Pedacinhos que voam e caem (morte de inseto, respingo, migalhas de ração).</summary>
        public static void Burst(Vector3 pos, Color color, int count, float speed = 3f, float size = .08f)
        {
            for (int i = 0; i < count; i++)
            {
                var go = Art.Prim(PrimitiveType.Cube, null, pos + Vector3.up * .2f, Vector3.one * size, color);
                go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var d = go.AddComponent<Debris>();
                var dir = Random.insideUnitSphere; dir.y = Mathf.Abs(dir.y) + .6f;
                d.vel = dir.normalized * speed * Random.Range(.5f, 1.2f);
            }
        }

        /// <summary>Anel achatado que cresce e some (área de golpe, miado, splash).</summary>
        public static void Ring(Vector3 pos, Color color, float r0, float r1, float dur)
        {
            var go = Art.Prim(PrimitiveType.Cylinder, null, new Vector3(pos.x, .03f, pos.z), Vector3.one, color, default, true);
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var r = go.AddComponent<RingFx>(); r.r0 = r0; r.r1 = r1; r.dur = dur; r.color = color;
        }
    }

    public class Debris : MonoBehaviour
    {
        public Vector3 vel; float t;
        void Update()
        {
            float dt = Time.deltaTime; t += dt;
            vel += Vector3.down * 14f * dt;
            transform.position += vel * dt;
            transform.Rotate(400 * dt, 300 * dt, 0);
            if (transform.position.y < .03f) { var p = transform.position; p.y = .03f; transform.position = p; vel *= .4f; vel.y = -vel.y * .3f; }
            if (t > .6f) transform.localScale *= 1 - dt * 6;
            if (t > .9f) Destroy(gameObject);
        }
    }

    public class RingFx : MonoBehaviour
    {
        public float r0, r1, dur; public Color color; float t; Material m;
        void Start() { m = GetComponent<Renderer>().material; }
        void Update()
        {
            t += Time.deltaTime; float k = t / dur;
            float r = Mathf.Lerp(r0, r1, k) * 2;
            transform.localScale = new Vector3(r, .01f, r);
            var c = color; c.a = 1 - k;
            m.SetColor("_BaseColor", Color.Lerp(color, Color.white, k * .5f));
            if (k >= 1) Destroy(gameObject);
        }
    }
}
