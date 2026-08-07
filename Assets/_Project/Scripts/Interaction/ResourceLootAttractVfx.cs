using Project.Audio;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Loot visual that flies from a mined/harvested node to the player, then commits
    /// the looted ItemData into inventory (+ pickup SFX / complete VFX / optional XP).
    /// </summary>
    public class ResourceLootAttractVfx : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _startPos;
        private Vector3 _startScaleVec = Vector3.one * 0.32f;
        private float _duration = 0.7f;
        private float _elapsed;
        private float _endScaleFactor = 0.15f;
        private ResourceGatherer _gatherer;
        private ItemData _item;
        private int _amount;
        private bool _committed;
        private AudioClip _grantClip;
        private float _grantVolume = 0.95f;

        /// <param name="prefabOverride">
        /// Optional fly model. Prefer node.lootAttractPrefab, else item.worldPrefab.
        /// Colliders / ItemPickup / ResourceNode are stripped so it is visual-only.
        /// </param>
        /// <param name="grantClipOverride">
        /// Optional inventory-grant SFX when loot arrives. Empty uses MineHarvestItemData.lootGrantClip
        /// or GameAudioManager.PlayItemPickup().
        /// </param>
        public static ResourceLootAttractVfx Spawn(
            Vector3 from,
            Transform playerCenter,
            ResourceGatherer gatherer,
            ItemData item,
            int amount,
            GameObject prefabOverride = null,
            Color? tint = null,
            AudioClip grantClipOverride = null,
            float grantVolume = 0.95f)
        {
            if (playerCenter == null || gatherer == null || item == null || amount <= 0)
                return null;

            GameObject go;
            if (prefabOverride != null)
            {
                go = Object.Instantiate(prefabOverride, from, Quaternion.identity);
                go.name = $"LootAttract_{item.itemName}";
                StripGameplayComponents(go);
                NormalizeFlyScale(go);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"LootAttract_{item.itemName}";
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

                go.transform.localScale = Vector3.one * 0.32f;
            }

            ResourceLootAttractVfx vfx = go.GetComponent<ResourceLootAttractVfx>();
            if (vfx == null)
                vfx = go.AddComponent<ResourceLootAttractVfx>();

            vfx.Begin(from, playerCenter, gatherer, item, amount, grantClipOverride, grantVolume);
            return vfx;
        }

        private static void StripGameplayComponents(GameObject root)
        {
            if (root == null)
                return;

            ResourceNode[] nodes = root.GetComponentsInChildren<ResourceNode>(true);
            for (int i = 0; i < nodes.Length; i++)
                Object.Destroy(nodes[i]);

            ItemPickup[] pickups = root.GetComponentsInChildren<ItemPickup>(true);
            for (int i = 0; i < pickups.Length; i++)
                Object.Destroy(pickups[i]);

            Collider[] cols = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
                Object.Destroy(cols[i]);

            Rigidbody[] bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
                Object.Destroy(bodies[i]);
        }

        private static void NormalizeFlyScale(GameObject root)
        {
            if (root == null)
                return;

            Renderer rend = root.GetComponentInChildren<Renderer>();
            if (rend == null)
                return;

            Bounds b = rend.bounds;
            float maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxDim < 0.01f)
                return;

            const float target = 0.35f;
            float scale = target / maxDim;
            root.transform.localScale *= scale;
        }

        public void Begin(
            Vector3 from,
            Transform playerCenter,
            ResourceGatherer gatherer,
            ItemData item,
            int amount,
            AudioClip grantClipOverride = null,
            float grantVolume = 0.95f)
        {
            _startPos = from;
            _target = playerCenter;
            _gatherer = gatherer;
            _item = item;
            _amount = amount;
            _grantClip = grantClipOverride;
            _grantVolume = Mathf.Clamp01(grantVolume);
            if (_grantClip == null && item is MineHarvestItemData lean && lean.lootGrantClip != null)
            {
                _grantClip = lean.lootGrantClip;
                _grantVolume = lean.lootGrantVolume;
            }

            _elapsed = 0f;
            _committed = false;
            transform.position = from;
            _startScaleVec = transform.localScale;
            if (_startScaleVec.sqrMagnitude < 0.0001f)
                _startScaleVec = Vector3.one * 0.32f;
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
            transform.localScale = Vector3.Lerp(_startScaleVec, _startScaleVec * _endScaleFactor, eased);
            transform.Rotate(90f * Time.deltaTime, 140f * Time.deltaTime, 40f * Time.deltaTime, Space.Self);

            if (t >= 1f)
                CommitAndDestroy(true);
        }

        private void CommitAndDestroy(bool playFeedback)
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

            Vector3 grantPos = _target != null
                ? _target.position + Vector3.up * 0.85f
                : transform.position;

            if (playFeedback)
            {
                if (granted)
                {
                    if (_grantClip != null)
                        AudioSource.PlayClipAtPoint(_grantClip, grantPos, _grantVolume);
                    else
                        GameAudioManager.Instance?.PlayItemPickup();

                    SpawnLootCompleteVfx(grantPos);
                    TryGrantGatherXp();
                }
                else
                {
                    GameAudioManager.Instance?.PlayInventoryItemClick();
                }
            }

            Destroy(gameObject);
        }

        private void SpawnLootCompleteVfx(Vector3 at)
        {
            GameObject prefab = null;
            if (_item is MineHarvestItemData lean)
                prefab = lean.lootCompleteVfxPrefab;
            if (prefab == null)
                return;

            GameObject instance = Instantiate(prefab, at, Quaternion.identity);
            instance.name = $"LootComplete_{_item.itemName}";
            CombatVfxUtility.PlayParticleSystemsRecursive(instance);
            Destroy(instance, ResolveVfxLifetime(instance));
        }

        private static float ResolveVfxLifetime(GameObject root)
        {
            float duration = 2f;
            if (root == null)
                return duration;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;
                float life = ps.main.duration + ps.main.startLifetime.constantMax;
                if (life > duration)
                    duration = life;
            }

            return Mathf.Clamp(duration + 0.25f, 1f, 8f);
        }

        private void TryGrantGatherXp()
        {
            _item?.TryGrantConfiguredXp();
        }
    }
}
