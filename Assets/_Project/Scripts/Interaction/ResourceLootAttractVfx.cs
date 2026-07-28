using Project.Audio;
using Project.Data;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Small loot orb that flies from a node to the player center: slow start, accelerates and
    /// shrinks near arrival, then commits inventory + pickup SFX.
    /// </summary>
    public class ResourceLootAttractVfx : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _startPos;
        private float _duration = 0.7f;
        private float _elapsed;
        private float _startScale = 0.32f;
        private float _endScale = 0.05f;
        private ResourceGatherer _gatherer;
        private ItemData _item;
        private int _amount;
        private bool _committed;

        public static ResourceLootAttractVfx Spawn(
            Vector3 from,
            Transform playerCenter,
            ResourceGatherer gatherer,
            ItemData item,
            int amount,
            GameObject prefabOverride = null,
            Color? tint = null)
        {
            if (playerCenter == null || gatherer == null || item == null || amount <= 0)
                return null;

            GameObject go;
            if (prefabOverride != null)
            {
                go = Object.Instantiate(prefabOverride, from, Quaternion.identity);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "ResourceLootAttract";
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                    Object.Destroy(col);

                Renderer rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                    if (shader != null)
                    {
                        Color color = tint ?? new Color(0.82f, 0.72f, 0.35f, 1f);
                        rend.material = new Material(shader) { color = color };
                    }
                }
            }

            ResourceLootAttractVfx vfx = go.GetComponent<ResourceLootAttractVfx>();
            if (vfx == null)
                vfx = go.AddComponent<ResourceLootAttractVfx>();

            vfx.Begin(from, playerCenter, gatherer, item, amount);
            return vfx;
        }

        public void Begin(
            Vector3 from,
            Transform playerCenter,
            ResourceGatherer gatherer,
            ItemData item,
            int amount)
        {
            _startPos = from;
            _target = playerCenter;
            _gatherer = gatherer;
            _item = item;
            _amount = amount;
            _elapsed = 0f;
            _committed = false;
            transform.position = from;
            transform.localScale = Vector3.one * _startScale;
            enabled = true;
        }

        private void Update()
        {
            if (_target == null)
            {
                CommitAndDestroy(false);
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.05f, _duration));
            // Ease-in cubic: slow start, fast finish.
            float eased = t * t * t;
            Vector3 end = _target.position + Vector3.up * 0.85f;
            transform.position = Vector3.Lerp(_startPos, end, eased);
            float scale = Mathf.Lerp(_startScale, _endScale, eased);
            transform.localScale = Vector3.one * scale;
            transform.Rotate(90f * Time.deltaTime, 140f * Time.deltaTime, 40f * Time.deltaTime, Space.Self);

            if (t >= 1f)
                CommitAndDestroy(true);
        }

        private void CommitAndDestroy(bool playSound)
        {
            if (_committed)
            {
                Destroy(gameObject);
                return;
            }

            _committed = true;
            bool granted = false;
            if (_gatherer != null && _item != null && _amount > 0)
                granted = _gatherer.TryGather(_item, _amount);

            if (playSound)
            {
                if (granted)
                    GameAudioManager.Instance?.PlayItemPickup();
                else
                    GameAudioManager.Instance?.PlayInventoryItemClick();
            }

            Destroy(gameObject);
        }
    }
}
