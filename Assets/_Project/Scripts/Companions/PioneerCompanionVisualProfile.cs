using Project.Pioneers;
using Project.UI;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Applies per-pioneer tinting to the shared ProjectUnityCharacter body mesh.
    /// </summary>
    public class PioneerCompanionVisualProfile : MonoBehaviour
    {
        [SerializeField] private Renderer[] bodyRenderers;

        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (bodyRenderers == null || bodyRenderers.Length == 0)
                CacheBodyRenderers();
        }

        public void Apply(SkilledPioneerRecord record)
        {
            if (record == null)
                return;

            if (bodyRenderers == null || bodyRenderers.Length == 0)
                CacheBodyRenderers();

            Color tint = ResolveTint(record);
            propertyBlock ??= new MaterialPropertyBlock();

            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Renderer renderer = bodyRenderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, tint);
                propertyBlock.SetColor(ColorId, tint);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheBodyRenderers()
        {
            Transform body = transform.Find("ProjectUnityCharacter/Body");
            if (body == null)
                body = transform.Find("Body");

            if (body != null)
            {
                bodyRenderers = body.GetComponentsInChildren<Renderer>(true);
                return;
            }

            bodyRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        private static Color ResolveTint(SkilledPioneerRecord record) => GetClassTint(record);

        public static Color GetClassTint(SkilledPioneerRecord record)
        {
            if (record == null)
                return SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.8f);

            Color baseTint = record.pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.92f),
                SkilledPioneerClass.ScienceSpecialist => SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.88f),
                SkilledPioneerClass.CombatTactician => SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.9f),
                SkilledPioneerClass.InfiltratorScout => SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.95f),
                SkilledPioneerClass.IoHybrid => SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.WarmOffWhite, 0.94f),
                _ => SurvivalPioneerUiPalette.BodyText
            };

            if (record.Kind == PioneerKind.RescuedEcho)
                baseTint = Color.Lerp(baseTint, SurvivalPioneerUiPalette.RichFuchsia, 0.35f);

            return baseTint;
        }
    }
}
