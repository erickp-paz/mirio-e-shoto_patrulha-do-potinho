using System.Collections.Generic;
using UnityEngine;

namespace MirioShoto
{
    /// <summary>
    /// Constrói os modelos provisórios com primitivas 3D (low-poly).
    /// Por que: valida câmera isométrica, escala e leitura visual antes de investir em modelos finais.
    /// </summary>
    public static class Art
    {
        static Material baseMat, emissiveBase;
        static readonly Dictionary<Color, Material> shared = new();

        /// <summary>
        /// Materiais-base em Assets/_MirioShoto/Resources, usando o shader URP Lit.
        /// Por que: o Unity só coloca no build os shaders referenciados por algum asset.
        /// Criar material só por código (Shader.Find) deixa o URP Lit fora do build e o jogo fica rosa.
        /// Esses dois .mat garantem o shader no build (o segundo garante também a variante com emissão).
        /// </summary>
        static Material Load(ref Material cache, string name)
        {
            if (cache != null) return cache;
            cache = Resources.Load<Material>(name);
            if (cache == null) cache = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            return cache;
        }
        static Material BaseMat() => Load(ref baseMat, "MirioShoto_BaseLit");
        static Material EmissiveBase() => Load(ref emissiveBase, "MirioShoto_BaseLitEmissive");

        public static Material Mat(Color c, bool instance = false)
        {
            if (!instance && shared.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(BaseMat());
            m.SetColor("_BaseColor", c); m.color = c;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .15f);
            if (!instance) shared[c] = m;
            return m;
        }

        public static Material Emissive(Color c, float intensity)
        {
            var m = new Material(EmissiveBase());
            m.SetColor("_BaseColor", c); m.color = c;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
            return m;
        }

        /// <summary>Cria uma primitiva sem colisor (a física é feita à mão, mais leve).</summary>
        public static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color c, Vector3 euler = default, bool instance = false)
        {
            var go = GameObject.CreatePrimitive(t);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(euler);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mat(c, instance);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>Gato low-poly. 'model' é o nó que balança/estica na animação.</summary>
        public static Transform BuildCat(Transform root, Color fur, Color dark, Color eyes, out List<Renderer> renderers)
        {
            var model = new GameObject("Modelo").transform;
            model.SetParent(root, false);
            var list = new List<Renderer>();
            renderers = list;
            void Add(GameObject g) => list.Add(g.GetComponent<Renderer>());

            Add(Prim(PrimitiveType.Capsule, model, new Vector3(0, .32f, -.05f), new Vector3(.42f, .36f, .42f), fur, new Vector3(90, 0, 0), true)); // corpo
            Add(Prim(PrimitiveType.Sphere, model, new Vector3(0, .55f, .38f), new Vector3(.42f, .38f, .38f), fur, default, true));               // cabeça
            Add(Prim(PrimitiveType.Cube, model, new Vector3(-.12f, .76f, .38f), new Vector3(.11f, .16f, .05f), dark, new Vector3(0, 0, 18), true)); // orelhas
            Add(Prim(PrimitiveType.Cube, model, new Vector3(.12f, .76f, .38f), new Vector3(.11f, .16f, .05f), dark, new Vector3(0, 0, -18), true));
            Add(Prim(PrimitiveType.Sphere, model, new Vector3(-.09f, .6f, .55f), new Vector3(.08f, .1f, .05f), eyes, default, true));             // olhos
            Add(Prim(PrimitiveType.Sphere, model, new Vector3(.09f, .6f, .55f), new Vector3(.08f, .1f, .05f), eyes, default, true));
            Add(Prim(PrimitiveType.Sphere, model, new Vector3(0, .52f, .57f), new Vector3(.05f, .04f, .03f), new Color(1f, .55f, .6f), default, true)); // nariz
            Add(Prim(PrimitiveType.Capsule, model, new Vector3(0, .42f, -.45f), new Vector3(.08f, .22f, .08f), dark, new Vector3(-50, 0, 0), true));  // rabo
            for (int i = 0; i < 4; i++) // patas
            {
                float x = i % 2 == 0 ? -.12f : .12f, z = i < 2 ? .18f : -.25f;
                Add(Prim(PrimitiveType.Cylinder, model, new Vector3(x, .1f, z), new Vector3(.1f, .1f, .1f), fur, default, true));
            }
            return model;
        }

        public static Transform BuildInsect(Transform root, InsectStats st, out List<Renderer> renderers)
        {
            var model = new GameObject("Modelo").transform;
            model.SetParent(root, false);
            renderers = new List<Renderer>();
            float s = st.size;
            var c = st.color; var dark = c * .6f; dark.a = 1;
            float y = st.flies ? .6f : s * .25f;
            renderers.Add(Prim(PrimitiveType.Sphere, model, new Vector3(0, y, -s * .25f), new Vector3(s * .55f, s * .4f, s * .75f), c, default, true).GetComponent<Renderer>());
            renderers.Add(Prim(PrimitiveType.Sphere, model, new Vector3(0, y, s * .2f), new Vector3(s * .35f, s * .32f, s * .35f), dark, default, true).GetComponent<Renderer>());
            renderers.Add(Prim(PrimitiveType.Sphere, model, new Vector3(0, y, s * .45f), new Vector3(s * .28f, s * .26f, s * .25f), dark, default, true).GetComponent<Renderer>());
            if (st.flies)
            {
                renderers.Add(Prim(PrimitiveType.Cube, model, new Vector3(-s * .35f, y + s * .15f, 0), new Vector3(s * .6f, .01f, s * .3f), new Color(.9f, .95f, 1f), new Vector3(0, 0, 20), true).GetComponent<Renderer>());
                renderers.Add(Prim(PrimitiveType.Cube, model, new Vector3(s * .35f, y + s * .15f, 0), new Vector3(s * .6f, .01f, s * .3f), new Color(.9f, .95f, 1f), new Vector3(0, 0, -20), true).GetComponent<Renderer>());
            }
            else
            {
                for (int i = -1; i <= 1; i++) // pernas
                {
                    renderers.Add(Prim(PrimitiveType.Cube, model, new Vector3(0, s * .08f, i * s * .2f), new Vector3(s * 1.1f, s * .05f, s * .05f), dark, default, true).GetComponent<Renderer>());
                }
            }
            if (st.horn) renderers.Add(Prim(PrimitiveType.Cube, model, new Vector3(0, y + s * .25f, s * .55f), new Vector3(s * .1f, s * .45f, s * .1f), dark, new Vector3(40, 0, 0), true).GetComponent<Renderer>());
            return model;
        }
    }
}
