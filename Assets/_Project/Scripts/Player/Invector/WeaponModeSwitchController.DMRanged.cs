using Project.Combat.DMRanged;
using Project.Data;
using UnityEngine;

namespace Project.Player.Invector
{
    // Round6: aim laser beam + dot on the single morphing DM Ranged weapon.
    // Pistol forms (id/name contains "pistol") use the PISTOL menu prefs; SMR / rifle forms use the RIFLE prefs.
    // One runtime-only stack ("Laser" LineRenderer + "laserSight" sprite) is built once per live DM Ranged
    // visual (settings copied from an existing player laser stack; nothing saved to prefabs/assets), parented to
    // the active form's muzzle, and re-parented + re-coloured only on bind / form change.
    public partial class WeaponModeSwitchController
    {
        [Header("DM Ranged morph laser colours (index = DM_RangedMorphSettings.forms order)")]
        [Tooltip("Default order: Rifle, Sub-Machine Rifle, Pistol 3, Pistol 2, Pistol 1. Applied via LineRenderer/SpriteRenderer colour (no material instances).")]
        [SerializeField]
        private Color[] morphStageLaserColors =
        {
            new Color(1f, 0.15f, 0.2f, 1f),   // Rifle - red
            new Color(1f, 0.55f, 0.1f, 1f),   // SMR - orange
            new Color(1f, 0.92f, 0.2f, 1f),   // Pistol 3 - yellow
            new Color(0.3f, 1f, 0.35f, 1f),   // Pistol 2 - green
            new Color(0.2f, 0.9f, 1f, 1f),    // Pistol 1 - cyan
        };

        private DMRangedMorphDriver _dmrDriver;
        private Transform _dmrRoot;
        private LineRenderer _dmrLine;
        private SpriteRenderer _dmrSprite;
        private Transform _dmrMuzzle;
        private static Material s_dmrFallbackLineMat;

        private void HandleMorphFormChanged()
        {
            ApplyLaserModes();
        }

        /// <summary>True when the drawn item is DM Ranged with a live driver; pistol = pistol-pref category.</summary>
        private bool TryGetDMRangedCategory(ItemData drawn, out bool pistol, out int formIndex)
        {
            pistol = false;
            formIndex = -1;
            if (drawn == null || !DMRangedMorphRuntime.IsDMRanged(drawn))
                return false;
            DMRangedMorphDriver d = DMRangedMorphRuntime.Active;
            if (d == null || !d.isActiveAndEnabled || d.IsHolsterInstance)
                return false;
            formIndex = d.CurrentFormIndex;
            DM_RangedMorphSettings s = DMRangedMorphRuntime.Settings;
            if (s != null && formIndex >= 0 && formIndex < s.FormCount && s.forms[formIndex] != null)
            {
                DMRangedForm f = s.forms[formIndex];
                pistol = Contains(f.id, "pistol") || Contains(f.displayName, "pistol");
            }
            return true;
        }

        private static bool Contains(string s, string v) =>
            !string.IsNullOrEmpty(s) && s.IndexOf(v, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private bool BindDMRangedLaser(bool beamEnabled, bool sightEnabled, int formIndex)
        {
            DMRangedMorphDriver d = DMRangedMorphRuntime.Active;
            if (d == null || (!beamEnabled && !sightEnabled))
                return false;
            if (!EnsureDMRangedStack(d))
                return false;

            Transform muzzle = d.CurrentMuzzle != null ? d.CurrentMuzzle : d.transform;
            if (_dmrMuzzle != muzzle || _dmrRoot.parent != muzzle)
            {
                _dmrMuzzle = muzzle;
                _dmrRoot.SetParent(muzzle, false);
                _dmrRoot.localPosition = Vector3.zero;
                _dmrRoot.localRotation = Quaternion.identity;
            }
            if (!_dmrRoot.gameObject.activeSelf)
                _dmrRoot.gameObject.SetActive(true);

            Color c = morphStageLaserColors != null && formIndex >= 0 && formIndex < morphStageLaserColors.Length
                ? morphStageLaserColors[formIndex]
                : Color.red;
            if (_dmrLine != null)
            {
                _dmrLine.startColor = c;
                _dmrLine.endColor = c;
            }
            if (_dmrSprite != null)
            {
                _dmrSprite.color = c;
                if (sightEnabled && !_dmrSprite.gameObject.activeSelf)
                    _dmrSprite.gameObject.SetActive(true);
            }

            activeAimLaserRoot = _dmrRoot;
            activeAimLaserLine = _dmrLine;
            activeAimLaserSight = _dmrSprite;
            activeAimSightRequested = sightEnabled;
            if (_dmrLine != null)
            {
                _dmrLine.useWorldSpace = true;
                _dmrLine.positionCount = 2;
                _dmrLine.enabled = beamEnabled;
            }
            if (_dmrSprite != null)
                _dmrSprite.enabled = sightEnabled;
            UpdateActiveAimLaserToReticle();
            return true;
        }

        private bool EnsureDMRangedStack(DMRangedMorphDriver d)
        {
            if (_dmrDriver == d && _dmrRoot != null && (_dmrLine != null || _dmrSprite != null))
                return true;

            _dmrDriver = d;
            _dmrMuzzle = null;
            FindLaserTemplates(out LineRenderer tLine, out SpriteRenderer tSprite);

            GameObject rootGo = new GameObject("Laser");
            rootGo.hideFlags = HideFlags.DontSave;
            _dmrRoot = rootGo.transform;
            _dmrRoot.SetParent(d.CurrentMuzzle != null ? d.CurrentMuzzle : d.transform, false);

            _dmrLine = rootGo.AddComponent<LineRenderer>();
            _dmrLine.useWorldSpace = true;
            _dmrLine.positionCount = 2;
            _dmrLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dmrLine.receiveShadows = false;
            if (tLine != null)
            {
                _dmrLine.sharedMaterials = tLine.sharedMaterials;
                _dmrLine.widthCurve = tLine.widthCurve;
                _dmrLine.widthMultiplier = tLine.widthMultiplier;
                _dmrLine.numCapVertices = tLine.numCapVertices;
                _dmrLine.alignment = tLine.alignment;
                _dmrLine.textureMode = tLine.textureMode;
                _dmrLine.sortingLayerID = tLine.sortingLayerID;
                _dmrLine.sortingOrder = tLine.sortingOrder;
            }
            else
            {
                if (s_dmrFallbackLineMat == null)
                {
                    Shader sh = Shader.Find("Sprites/Default");
                    if (sh != null)
                        s_dmrFallbackLineMat = new Material(sh) { hideFlags = HideFlags.DontSave };
                }
                if (s_dmrFallbackLineMat != null)
                    _dmrLine.sharedMaterial = s_dmrFallbackLineMat;
                _dmrLine.widthMultiplier = 0.008f;
            }
            _dmrLine.enabled = false;

            _dmrSprite = null;
            if (tSprite != null)
            {
                GameObject sightGo = new GameObject("laserSight");
                sightGo.hideFlags = HideFlags.DontSave;
                sightGo.transform.SetParent(_dmrRoot, false);
                sightGo.transform.localScale = tSprite.transform.lossyScale;
                _dmrSprite = sightGo.AddComponent<SpriteRenderer>();
                _dmrSprite.sprite = tSprite.sprite;
                _dmrSprite.sharedMaterial = tSprite.sharedMaterial;
                _dmrSprite.sortingLayerID = tSprite.sortingLayerID;
                _dmrSprite.sortingOrder = tSprite.sortingOrder;
                _dmrSprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _dmrSprite.enabled = false;
            }
            return true;
        }

        /// <summary>One-time (per DM Ranged visual) search of the player's existing laser stacks for a template.</summary>
        private void FindLaserTemplates(out LineRenderer line, out SpriteRenderer sprite)
        {
            line = null;
            sprite = null;
            LineRenderer[] lines = GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length && line == null; i++)
                if (lines[i] != null && lines[i] != _dmrLine && lines[i].name.Equals("Laser", System.StringComparison.OrdinalIgnoreCase))
                    line = lines[i];
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < sprites.Length && sprite == null; i++)
                if (sprites[i] != null && sprites[i] != _dmrSprite && sprites[i].sprite != null &&
                    sprites[i].name.Equals("laserSight", System.StringComparison.OrdinalIgnoreCase))
                    sprite = sprites[i];
        }
    }
}
