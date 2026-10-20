using Invector.Throw;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Tints throw arc LineRenderer red and sizes the landing projector as a large blast ring while aiming.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMIGrenadeThrowLandingRing : MonoBehaviour
    {
        [SerializeField] private vThrowManager throwManager;

        private vThrowRangeProjectorController[] _projectors;
        private LineRenderer _throwArcLine;
        private Material[] _projectorMaterials;

        private void Awake()
        {
            ResolveThrowManager();
            CacheVisuals();
            ApplyRingColor();
        }

        private void OnEnable()
        {
            ResolveThrowManager();
            CacheVisuals();
            ApplyRingColor();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !DMIGrenadeThrowBridge.GrenadesEnabled || throwManager == null)
                return;

            if (!ShouldDriveThrowVisuals())
                return;

            ApplyThrowArcColor();
            UpdateRingSize();
        }

        private void ResolveThrowManager()
        {
            if (throwManager == null)
                throwManager = GetComponent<vThrowManager>() ?? GetComponentInChildren<vThrowManager>(true);
        }

        private bool ShouldDriveThrowVisuals()
        {
            if (throwManager.ObjectToThrow == null)
                return false;

            if (_throwArcLine != null && _throwArcLine.gameObject.activeInHierarchy)
                return true;

            return throwManager.throwEnd != null && throwManager.throwEnd.activeSelf;
        }

        private void CacheVisuals()
        {
            if (throwManager == null)
                return;

            Transform throwRoot = throwManager.transform;
            _projectors = throwRoot.GetComponentsInChildren<vThrowRangeProjectorController>(true);
            _throwArcLine = throwManager.lineRenderer != null
                ? throwManager.lineRenderer
                : throwRoot.GetComponentInChildren<LineRenderer>(true);

            if (_projectors != null && _projectors.Length > 0)
            {
                _projectorMaterials = new Material[_projectors.Length];
                for (int i = 0; i < _projectors.Length; i++)
                {
                    Projector projector = _projectors[i] != null ? _projectors[i].GetComponent<Projector>() : null;
                    if (projector == null)
                        continue;

                    _projectorMaterials[i] = projector.material;
                }
            }
        }

        private void ApplyRingColor()
        {
            if (_projectorMaterials == null)
                return;

            for (int i = 0; i < _projectorMaterials.Length; i++)
            {
                TryTintMaterial(_projectorMaterials[i], ResolveLandingRingColor());
            }
        }

        private void ApplyThrowArcColor()
        {
            // Never call vThrowLineController.SetLineColor — its LineRenderer ref may be unset during parent Awake.
            if (_throwArcLine == null || !_throwArcLine.gameObject.activeInHierarchy)
                return;

            TryTintLineRenderer(_throwArcLine, ResolveThrowArcLineColor());
        }

        private void UpdateRingSize()
        {
            if (_projectors == null || _projectors.Length == 0)
                return;

            vThrowableObject throwable = throwManager.ObjectToThrow;
            if (throwable == null)
                return;

            DMIGrenadeExplosive explosive = throwable.GetComponent<DMIGrenadeExplosive>();
            explosive?.RefreshFromProfile();
            float radius = explosive != null ? explosive.EffectiveExplosionRadius : 4f;
            float ringScale = ResolveLandingRingSizeMultiplier();
            Texture2D ringTexture = ResolveRingIndicatorTexture();

            for (int i = 0; i < _projectors.Length; i++)
            {
                vThrowRangeProjectorController controller = _projectors[i];
                if (controller == null)
                    continue;

                controller.projectorSizeMultiplier = ringScale;
                Projector projector = controller.GetComponent<Projector>();
                if (projector == null || projector.material == null)
                    continue;

                controller.SetProjectorSize(radius);
                TryTintMaterial(projector.material, ResolveLandingRingColor());
                if (ringTexture != null && projector.material.HasProperty("_MainTex"))
                    projector.material.SetTexture("_MainTex", ringTexture);
            }
        }

        private static void TryTintLineRenderer(LineRenderer line, Color color)
        {
            if (line == null)
                return;

            Material mat = line.material;
            if (mat == null)
                return;

            TryTintMaterial(mat, color);
        }

        private static void TryTintMaterial(Material mat, Color color)
        {
            if (mat == null)
                return;

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
        }

        private static float ResolveLandingRingSizeMultiplier()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            return profile != null ? Mathf.Max(1f, profile.grenadeLandingRingSizeMultiplier) : 10f;
        }

        private static Texture2D ResolveRingIndicatorTexture()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            return profile != null ? profile.grenadeLandingRingIndicatorTexture : null;
        }

        private static Color ResolveThrowArcLineColor()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            return profile != null
                ? profile.grenadeThrowArcLineColor
                : new Color(1f, 0f, 0f, 0.88f);
        }

        private static Color ResolveLandingRingColor()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            return profile != null
                ? profile.grenadeLandingRingColor
                : new Color(1f, 0f, 0f, 0.42f);
        }
    }
}
