using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Small rock chunk that flies from a mined node toward the mining-tool muzzle, scaling up.
    /// </summary>
    public class DMIMiningChunkVfx : MonoBehaviour
    {
        private Transform target;
        private Vector3 startPos;
        private float duration = 0.45f;
        private float elapsed;
        private float startScale = 0.12f;
        private float endScale = 0.55f;

        public static DMIMiningChunkVfx Spawn(Vector3 from, Transform muzzle, GameObject prefabOverride = null)
        {
            GameObject go;
            if (prefabOverride != null)
            {
                go = Instantiate(prefabOverride, from, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "MiningChunk";
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                    Destroy(col);
                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (shader != null)
                    {
                        rend.material = new Material(shader)
                        {
                            color = new Color(0.55f, 0.42f, 0.28f, 1f)
                        };
                    }
                }
            }

            DMIMiningChunkVfx vfx = go.GetComponent<DMIMiningChunkVfx>();
            if (vfx == null)
                vfx = go.AddComponent<DMIMiningChunkVfx>();

            vfx.Begin(from, muzzle);
            return vfx;
        }

        public void Begin(Vector3 from, Transform muzzle)
        {
            startPos = from;
            target = muzzle;
            transform.position = from;
            transform.localScale = Vector3.one * startScale;
            elapsed = 0f;
            enabled = true;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
            Vector3 end = target != null ? target.position : startPos + Vector3.up;
            transform.position = Vector3.Lerp(startPos, end, t * t);
            float scale = Mathf.Lerp(startScale, endScale, t);
            transform.localScale = Vector3.one * scale;
            transform.Rotate(120f * Time.deltaTime, 90f * Time.deltaTime, 40f * Time.deltaTime, Space.Self);

            if (t >= 1f)
                Destroy(gameObject);
        }
    }
}
