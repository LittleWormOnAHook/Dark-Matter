using System.Collections.Generic;
using Project.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Toolkit hex skill graph (nodes, prereq paths, hover popup, click-to-spend).
    /// Stamp: DMUiToolkit 0901-finish
    /// </summary>
    public partial class DMUiToolkitMenus
    {
        private const float HexSize = 92f;
        private const float HexColSpacing = 118f;
        private const float HexRowSpacing = 104f;
        private const float HexPadX = 56f;
        private const float HexPadY = 48f;

        private static readonly Color HexRankFilledBlue = new Color(0.55f, 0.78f, 0.95f, 1f);
        private static readonly Color HexRankEmptyGray = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.85f);
        private static readonly Color HexPathLocked = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.35f);
        private static readonly Color HexPathReady = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SoftBeigeGray, 0.55f);
        private static readonly Color HexPathOwned = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.72f);

        private ScrollView skillsHexScroll;
        private VisualElement skillsHexHost;
        private VisualElement skillsHexPaths;
        private VisualElement skillsHexNodes;
        private readonly Dictionary<string, ToolkitHexNode> hexNodes = new Dictionary<string, ToolkitHexNode>();
        private string hoveredSkillId;

        private sealed class ToolkitHexNode
        {
            public SkillDefinition Skill;
            public VisualElement Root;
            public VisualElement Fill;
            public VisualElement Outline;
            public VisualElement Glow;
            public Label Label;
            public VisualElement[] RankDots;
            public Vector2 Pos;
        }

        private void BindSkillsHex(VisualElement tree)
        {
            if (tree == null)
                return;

            skillsHexScroll = tree.Q<ScrollView>("skills-hex-scroll");
            skillsHexHost = tree.Q<VisualElement>("skills-hex-host");
            if (skillsHexHost == null && skillsHexScroll != null)
                skillsHexHost = skillsHexScroll.Q<VisualElement>("skills-hex-host");

            if (skillsHexHost == null)
                return;

            skillsHexHost.pickingMode = PickingMode.Position;
            if (skillsHexPaths == null)
            {
                skillsHexPaths = new VisualElement { name = "skills-hex-paths", pickingMode = PickingMode.Ignore };
                skillsHexPaths.AddToClassList("dmg-hex-layer");
                skillsHexHost.Add(skillsHexPaths);
            }

            if (skillsHexNodes == null)
            {
                skillsHexNodes = new VisualElement { name = "skills-hex-nodes", pickingMode = PickingMode.Position };
                skillsHexNodes.AddToClassList("dmg-hex-layer");
                skillsHexHost.Add(skillsHexNodes);
            }
        }

        private void RebuildSkillsHex()
        {
            boundProgression ??= PlayerProgressionManager.EnsureExists();
            if (skillsList != null)
                DMUiToolkitOverlayDocument.SetShown(skillsList, false);

            if (skillsHexScroll != null)
                DMUiToolkitOverlayDocument.SetShown(skillsHexScroll, true);

            if (skillsHexHost == null)
            {
                if (root != null)
                    BindSkillsHex(root);
                if (skillsHexHost == null)
                    return;
            }

            hexNodes.Clear();
            skillsHexPaths?.Clear();
            skillsHexNodes?.Clear();

            List<SkillDefinition> skills = SkillRegistry.GetSkillsByCategory(skillsCategory);
            if (skills == null || skills.Count == 0)
            {
                skillsHexHost.style.width = 420f;
                skillsHexHost.style.height = 280f;
                if (skillsHexNodes != null)
                    skillsHexNodes.Add(MakeEmpty("No skills in this tree yet."));
                return;
            }

            int maxRow = 0;
            int maxCol = 2;
            for (int i = 0; i < skills.Count; i++)
            {
                maxRow = Mathf.Max(maxRow, skills[i].treeRow);
                maxCol = Mathf.Max(maxCol, skills[i].treeColumn);
            }

            float width = HexPadX * 2f + (maxCol + 1) * HexColSpacing;
            float height = HexPadY * 2f + (maxRow + 1) * HexRowSpacing + HexSize * 0.35f;
            skillsHexHost.style.width = Mathf.Max(width, 420f);
            skillsHexHost.style.height = Mathf.Max(height, 280f);

            Dictionary<string, ToolkitHexNode> byId = new Dictionary<string, ToolkitHexNode>();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                    continue;
                ToolkitHexNode view = CreateToolkitHexNode(skill);
                byId[skill.ResolvedId] = view;
                hexNodes[skill.ResolvedId] = view;
            }

            for (int i = 0; i < skills.Count; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null || skill.prerequisiteSkillIds == null)
                    continue;
                for (int p = 0; p < skill.prerequisiteSkillIds.Length; p++)
                {
                    string prereqId = skill.prerequisiteSkillIds[p];
                    if (string.IsNullOrEmpty(prereqId))
                        continue;
                    if (!byId.TryGetValue(prereqId, out ToolkitHexNode from) || !byId.TryGetValue(skill.ResolvedId, out ToolkitHexNode to))
                        continue;
                    CreateHexPath(from, to);
                }
            }

            if (string.IsNullOrEmpty(selectedSkillId) && skills.Count > 0)
                selectedSkillId = skills[0].ResolvedId;

            foreach (KeyValuePair<string, ToolkitHexNode> pair in hexNodes)
                ApplyHexHoverVisual(pair.Value, pair.Key == hoveredSkillId || pair.Key == selectedSkillId);
        }

        private ToolkitHexNode CreateToolkitHexNode(SkillDefinition skill)
        {
            int rank = boundProgression != null ? boundProgression.GetSkillRank(skill.ResolvedId) : 0;
            int maxRank = skill.ClampedMaxRank;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(skill, boundProgression, out _);
            bool isMaxed = rank >= maxRank;
            bool unlocked = rank > 0 || canAllocate || AreHexPrerequisitesMet(skill);
            Color accent = GetHexCategoryAccent(skillsCategory);
            Vector2 pos = HexGridToPos(skill.treeColumn, skill.treeRow);

            VisualElement node = new VisualElement { name = "hex-" + skill.ResolvedId, pickingMode = PickingMode.Position };
            node.AddToClassList("dmg-hex-node");
            node.style.left = pos.x - HexSize * 0.5f;
            node.style.top = pos.y - HexSize * 0.5f;
            node.style.width = HexSize;
            node.style.height = HexSize;
            node.userData = skill.ResolvedId;

            VisualElement glow = MakeHexSprite("hex-glow", DmHexUiSprites.SoftGlow, Color.clear, -10f);
            glow.pickingMode = PickingMode.Ignore;
            node.Add(glow);

            Color fillColor = unlocked
                ? DarkMatterGenesisUiPalette.WithAlpha(rank > 0 ? accent : DarkMatterGenesisUiPalette.CharcoalGray, rank > 0 ? 0.88f : 0.82f)
                : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.55f);
            VisualElement fill = MakeHexSprite("hex-fill", DmHexUiSprites.FilledHex, fillColor, 0f);
            fill.pickingMode = PickingMode.Ignore;
            node.Add(fill);

            Color outlineColor = isMaxed
                ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.95f)
                : canAllocate
                    ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.95f)
                    : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.9f);
            VisualElement outline = MakeHexSprite("hex-outline", DmHexUiSprites.OutlineHex, outlineColor, 0f);
            outline.pickingMode = PickingMode.Ignore;
            node.Add(outline);

            Label label = new Label(skill.displayName);
            label.AddToClassList("dmg-hex-label");
            label.pickingMode = PickingMode.Ignore;
            node.Add(label);

            VisualElement dotsRow = new VisualElement();
            dotsRow.AddToClassList("dmg-hex-dots");
            dotsRow.pickingMode = PickingMode.Ignore;
            VisualElement[] dots = new VisualElement[SkillDefinition.DisplayMaxRank];
            for (int d = 0; d < SkillDefinition.DisplayMaxRank; d++)
            {
                VisualElement dot = new VisualElement();
                dot.AddToClassList("dmg-hex-dot");
                dot.pickingMode = PickingMode.Ignore;
                if (DmHexUiSprites.RankDot != null)
                    dot.style.backgroundImage = new StyleBackground(Background.FromSprite(DmHexUiSprites.RankDot));
                bool usedSlot = d < maxRank;
                if (!usedSlot)
                    dot.style.unityBackgroundImageTintColor = Color.clear;
                else
                    dot.style.unityBackgroundImageTintColor = d < rank ? HexRankFilledBlue : HexRankEmptyGray;
                dotsRow.Add(dot);
                dots[d] = dot;
            }

            node.Add(dotsRow);
            skillsHexNodes.Add(node);

            SkillDefinition captured = skill;
            node.RegisterCallback<PointerEnterEvent>(_ => OnToolkitHexHover(captured, true));
            node.RegisterCallback<PointerLeaveEvent>(_ => OnToolkitHexHover(captured, false));
            node.RegisterCallback<ClickEvent>(_ => OnToolkitHexClicked(captured));

            return new ToolkitHexNode
            {
                Skill = skill,
                Root = node,
                Fill = fill,
                Outline = outline,
                Glow = glow,
                Label = label,
                RankDots = dots,
                Pos = pos
            };
        }

        private static VisualElement MakeHexSprite(string name, Sprite sprite, Color tint, float outset)
        {
            VisualElement element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = outset;
            element.style.top = outset;
            element.style.right = outset;
            element.style.bottom = outset;
            if (sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(Background.FromSprite(sprite));
                element.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            }

            element.style.unityBackgroundImageTintColor = tint;
            element.style.backgroundColor = Color.clear;
            return element;
        }

        private void CreateHexPath(ToolkitHexNode from, ToolkitHexNode to)
        {
            if (skillsHexPaths == null || from == null || to == null)
                return;

            Vector2 a = from.Pos;
            Vector2 b = to.Pos;
            Vector2 delta = b - a;
            float length = Mathf.Max(8f, delta.magnitude - HexSize * 0.55f);
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Vector2 mid = (a + b) * 0.5f;

            VisualElement line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("dmg-hex-path");
            line.style.left = mid.x - length * 0.5f;
            line.style.top = mid.y - 2f;
            line.style.width = length;
            line.style.height = 4f;
            SetElementRotate(line, angle);

            int fromRank = boundProgression != null ? boundProgression.GetSkillRank(from.Skill.ResolvedId) : 0;
            int toRank = boundProgression != null ? boundProgression.GetSkillRank(to.Skill.ResolvedId) : 0;
            if (toRank > 0 && fromRank > 0)
                line.style.backgroundColor = HexPathOwned;
            else if (fromRank > 0)
                line.style.backgroundColor = HexPathReady;
            else
                line.style.backgroundColor = HexPathLocked;

            skillsHexPaths.Add(line);
        }

        private void OnToolkitHexHover(SkillDefinition skill, bool entering)
        {
            if (skill == null)
                return;

            if (entering)
            {
                hoveredSkillId = skill.ResolvedId;
                selectedSkillId = skill.ResolvedId;
            }
            else if (hoveredSkillId == skill.ResolvedId)
            {
                hoveredSkillId = null;
            }

            foreach (KeyValuePair<string, ToolkitHexNode> pair in hexNodes)
                ApplyHexHoverVisual(pair.Value, pair.Key == hoveredSkillId || pair.Key == selectedSkillId);

            ApplySkillDetail();
            ApplyHexPopupOverride();
        }

        private void OnToolkitHexClicked(SkillDefinition skill)
        {
            if (skill == null)
                return;

            selectedSkillId = skill.ResolvedId;
            hoveredSkillId = skill.ResolvedId;
            if (PlayerSkillAllocator.TryAllocate(skill, out string error))
            {
                RefreshSkills();
                RefreshCharacter();
                return;
            }

            ApplySkillDetail();
            ApplyHexPopupOverride();
            if (!string.IsNullOrEmpty(error)
                && skill.requiredPlayerLevel > 1
                && !LevelUnlockUtility.CanAccess(boundProgression, skill.requiredPlayerLevel))
            {
                LevelUnlockUtility.ShowRequireLevelPopup(skill.requiredPlayerLevel);
            }
            else if (!string.IsNullOrEmpty(error) && error != "Max rank reached.")
            {
                PickupToastUI.Show(error);
            }

            foreach (KeyValuePair<string, ToolkitHexNode> pair in hexNodes)
                ApplyHexHoverVisual(pair.Value, pair.Key == selectedSkillId);
        }

        private void ApplyHexHoverVisual(ToolkitHexNode view, bool highlighted)
        {
            if (view?.Glow == null || view.Skill == null)
                return;

            int rank = boundProgression != null ? boundProgression.GetSkillRank(view.Skill.ResolvedId) : 0;
            int maxRank = view.Skill.ClampedMaxRank;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(view.Skill, boundProgression, out _);
            bool isMaxed = rank >= maxRank;

            if (highlighted)
            {
                bool selected = selectedSkillId == view.Skill.ResolvedId;
                Color glow = selected ? DarkMatterGenesisUiPalette.Gold : DarkMatterGenesisUiPalette.RichFuchsia;
                view.Glow.style.unityBackgroundImageTintColor = DarkMatterGenesisUiPalette.WithAlpha(glow, selected ? 0.5f : 0.55f);
                if (view.Outline != null)
                    view.Outline.style.unityBackgroundImageTintColor = DarkMatterGenesisUiPalette.WithAlpha(glow, 1f);
            }
            else
            {
                view.Glow.style.unityBackgroundImageTintColor = Color.clear;
                if (view.Outline != null)
                {
                    view.Outline.style.unityBackgroundImageTintColor = isMaxed
                        ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.Gold, 0.95f)
                        : canAllocate
                            ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.95f)
                            : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SlateGray, 0.9f);
                }
            }
        }

        private void ApplyHexPopupOverride()
        {
            string id = !string.IsNullOrEmpty(hoveredSkillId) ? hoveredSkillId : selectedSkillId;
            if (string.IsNullOrEmpty(id) || !hexNodes.TryGetValue(id, out ToolkitHexNode view) || view.Skill == null)
            {
                if (skillsDetailTitle != null)
                    skillsDetailTitle.text = "Hover a hex";
                if (skillsDetailBody != null)
                    skillsDetailBody.text = "Hover a skill for details. Click to spend skill points and raise its rank.";
                return;
            }

            SkillDefinition skill = view.Skill;
            int rank = boundProgression != null ? boundProgression.GetSkillRank(skill.ResolvedId) : 0;
            int maxRank = skill.ClampedMaxRank;
            int nextCost = rank < maxRank ? skill.GetCostForNextRank(rank) : 0;
            bool canAllocate = PlayerSkillAllocator.CanAllocate(skill, boundProgression, out string error);
            bool isMaxed = rank >= maxRank;
            string prereqLine = FormatHexPrerequisites(skill);
            string status = isMaxed
                ? "MAX RANK"
                : canAllocate
                    ? "Click to upgrade · Cost " + nextCost + " SP"
                    : error ?? "Locked";

            if (skillsDetailTitle != null)
                skillsDetailTitle.text = skill.displayName;
            if (skillsDetailBody != null)
            {
                skillsDetailBody.text =
                    (skill.description ?? string.Empty) + "\n\n" +
                    "Rank " + rank + "/" + maxRank + "\n" +
                    "Requires player level " + skill.requiredPlayerLevel + "\n" +
                    (string.IsNullOrEmpty(prereqLine) ? string.Empty : prereqLine + "\n") +
                    "\n" + status;
            }
        }

        private static string FormatHexPrerequisites(SkillDefinition skill)
        {
            if (skill.prerequisiteSkillIds == null || skill.prerequisiteSkillIds.Length == 0)
                return string.Empty;

            List<string> names = new List<string>();
            for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
            {
                SkillDefinition prereq = SkillRegistry.Resolve(skill.prerequisiteSkillIds[i]);
                if (prereq != null)
                    names.Add(prereq.displayName);
            }

            return names.Count == 0 ? string.Empty : "Requires: " + string.Join(", ", names);
        }

        private bool AreHexPrerequisitesMet(SkillDefinition skill)
        {
            if (skill.prerequisiteSkillIds == null || skill.prerequisiteSkillIds.Length == 0)
                return true;
            if (boundProgression == null)
                return false;

            for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
            {
                string id = skill.prerequisiteSkillIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (boundProgression.GetSkillRank(id) <= 0)
                    return false;
            }

            return true;
        }

        private static Vector2 HexGridToPos(int column, int row)
        {
            float x = HexPadX + column * HexColSpacing + HexSize * 0.5f;
            float y = HexPadY + row * HexRowSpacing + HexSize * 0.5f;
            if ((column & 1) == 1)
                y += HexRowSpacing * 0.18f;
            return new Vector2(x, y);
        }

        private static Color GetHexCategoryAccent(SkillTreeCategory category)
        {
            switch (category)
            {
                case SkillTreeCategory.Melee: return DarkMatterGenesisUiPalette.DeepMagenta;
                case SkillTreeCategory.Pistols: return DarkMatterGenesisUiPalette.RichFuchsia;
                case SkillTreeCategory.Rifles: return DarkMatterGenesisUiPalette.Gold;
                case SkillTreeCategory.Survival: return DarkMatterGenesisUiPalette.SoftBeigeGray;
                case SkillTreeCategory.Player: return DarkMatterGenesisUiPalette.CharcoalGray;
                default: return DarkMatterGenesisUiPalette.SlateGray;
            }
        }

        internal static void SetElementRotate(VisualElement element, float degrees)
        {
            if (element == null)
                return;

            element.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(Angle.Degrees(degrees)));
        }
    }
}
