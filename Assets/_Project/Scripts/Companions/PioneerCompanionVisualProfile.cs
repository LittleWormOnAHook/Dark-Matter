using Project.Pioneers;
using Project.UI;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Applies per-pioneer tinting to companion body renderers (Invector mesh or child skinned meshes).
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
            Transform body = transform.Find("Body");
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
                return DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.8f);

            Color baseTint = record.pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.92f),
                SkilledPioneerClass.ScienceSpecialist => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.88f),
                SkilledPioneerClass.CombatTactician => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.9f),
                SkilledPioneerClass.InfiltratorScout => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.95f),
                SkilledPioneerClass.IoHybrid => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.WarmOffWhite, 0.94f),
                SkilledPioneerClass.MedTech => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.PositiveGreen, 0.9f),
                SkilledPioneerClass.LogisticsOfficer => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.88f),
                SkilledPioneerClass.SalvageEngineer => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SoftBeigeGray, 0.92f),
                SkilledPioneerClass.CommunicationsOfficer => DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.86f),
                _ => DarkMatterGenesisUiPalette.BodyText
            };

            if (record.Kind == PioneerKind.RescuedEcho)
                baseTint = Color.Lerp(baseTint, DarkMatterGenesisUiPalette.RichFuchsia, 0.35f);

            return baseTint;
        }
    }
}
