using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK level-up / require-level / achievement toasts. Gameplay still calls the old static
    /// Show methods; those forward here when the Toolkit HUD is driving.
    /// </summary>
    [DefaultExecutionOrder(-380)]
    [DisallowMultipleComponent]
    public class DMUiToolkitLevelUp : MonoBehaviour
    {
        private static DMUiToolkitLevelUp instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement levelUpCard;
        private Label levelUpHeadline;
        private Label levelUpLevel;
        private Label levelUpSubtitle;
        private VisualElement requireCard;
        private Label requireValue;
        private VisualElement achievementCard;
        private Label achievementTitle;
        private Label achievementDesc;
        private Label achievementXp;
        private Coroutine levelRoutine;
        private Coroutine requireRoutine;
        private Coroutine achievementRoutine;
        private readonly Queue<AchievementPending> achievementQueue = new Queue<AchievementPending>();
        private bool bound;

        private struct AchievementPending
        {
            public string Title;
            public string Description;
            public int XpReward;
        }

        public static DMUiToolkitLevelUp Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitLevelUp EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.LevelUpName,
                DMUiToolkitOverlayDocument.LevelUpUxml,
                DMUiToolkitOverlayDocument.LevelUpUss,
                DMUiToolkitOverlayDocument.LevelUpSort);
            if (doc == null)
                return null;

            DMUiToolkitLevelUp host = doc.GetComponent<DMUiToolkitLevelUp>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitLevelUp>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShowLevelUp(int newLevel, int levelsGained)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitLevelUp host = EnsureHost();
            if (host == null)
                return false;

            host.PresentLevelUp(newLevel, Mathf.Max(1, levelsGained));
            return true;
        }

        public static bool TryShowRequireLevel(int requiredLevel)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitLevelUp host = EnsureHost();
            if (host == null)
                return false;

            host.PresentRequireLevel(requiredLevel);
            return true;
        }

        public static bool TryShowAchievement(string title, string description, int xpReward)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitLevelUp host = EnsureHost();
            if (host == null)
                return false;

            host.EnqueueAchievement(title, description, xpReward);
            return true;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDisable()
        {
            bound = false;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            bool want = DMUiToolkitOverlayDocument.GameplayHudWanted();
            if (root != null && !want)
            {
                if (levelUpCard != null)
                    levelUpCard.style.opacity = 0f;
                if (requireCard != null)
                    requireCard.style.opacity = 0f;
                if (achievementCard != null)
                    achievementCard.style.opacity = 0f;
            }

            HideUguiToasts();
            DMUiToolkitHud.HideHudAuthoredOverlayHosts();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("levelup-root") ?? tree;
            levelUpCard = tree.Q<VisualElement>("level-up");
            levelUpHeadline = tree.Q<Label>("level-up-headline");
            levelUpLevel = tree.Q<Label>("level-up-level");
            levelUpSubtitle = tree.Q<Label>("level-up-subtitle");
            requireCard = tree.Q<VisualElement>("require-level");
            requireValue = tree.Q<Label>("require-level-value");
            achievementCard = tree.Q<VisualElement>("achievement");
            achievementTitle = tree.Q<Label>("achievement-title");
            achievementDesc = tree.Q<Label>("achievement-desc");
            achievementXp = tree.Q<Label>("achievement-xp");

            HidePreview();
            bound = root != null;
        }

        private void HidePreview()
        {
            DMUiToolkitOverlayDocument.SetShown(levelUpCard, false);
            DMUiToolkitOverlayDocument.SetShown(requireCard, false);
            DMUiToolkitOverlayDocument.SetShown(achievementCard, false);
            if (levelUpCard != null)
                levelUpCard.style.opacity = 0f;
            if (requireCard != null)
                requireCard.style.opacity = 0f;
            if (achievementCard != null)
                achievementCard.style.opacity = 0f;
        }

        private void PresentLevelUp(int newLevel, int levelsGained)
        {
            BindTree();
            if (levelUpCard == null)
                return;

            if (levelUpHeadline != null)
                levelUpHeadline.text = "You Leveled Up!";

            if (levelUpLevel != null)
            {
                levelUpLevel.text = levelsGained > 1
                    ? $"Level {newLevel}  (+{levelsGained})"
                    : $"Level {newLevel}";
            }

            int skillPoints = SumSkillPointsGained(newLevel, levelsGained);
            if (levelUpSubtitle != null)
            {
                bool show = skillPoints > 0;
                DMUiToolkitOverlayDocument.SetShown(levelUpSubtitle, show);
                if (show)
                    levelUpSubtitle.text = $"+{skillPoints} skill point{(skillPoints == 1 ? string.Empty : "s")}";
            }

            GameAudioManager.Instance?.PlayLevelUp();

            if (levelRoutine != null)
                StopCoroutine(levelRoutine);
            levelRoutine = StartCoroutine(AnimateCard(levelUpCard, 0.34f, 2.5f, 0.35f, 36f, 22f));
        }

        private void PresentRequireLevel(int requiredLevel)
        {
            BindTree();
            if (requireCard == null)
                return;

            if (requireValue != null)
                requireValue.text = requiredLevel.ToString();

            GameAudioManager.Instance?.PlayButtonClick();

            if (requireRoutine != null)
                StopCoroutine(requireRoutine);
            requireRoutine = StartCoroutine(AnimateCard(requireCard, 0.28f, 1.8f, 0.3f, 28f, 18f));
        }

        private void EnqueueAchievement(string title, string description, int xpReward)
        {
            achievementQueue.Enqueue(new AchievementPending
            {
                Title = title,
                Description = description,
                XpReward = xpReward
            });

            if (achievementRoutine == null && isActiveAndEnabled)
                achievementRoutine = StartCoroutine(RunAchievements());
        }

        private IEnumerator RunAchievements()
        {
            while (achievementQueue.Count > 0)
            {
                AchievementPending pending = achievementQueue.Dequeue();
                BindTree();
                if (achievementCard == null)
                    break;

                if (achievementTitle != null)
                    achievementTitle.text = pending.Title ?? string.Empty;

                bool hasDescription = !string.IsNullOrWhiteSpace(pending.Description);
                if (achievementDesc != null)
                {
                    DMUiToolkitOverlayDocument.SetShown(achievementDesc, hasDescription);
                    if (hasDescription)
                        achievementDesc.text = pending.Description;
                }

                bool hasXp = pending.XpReward > 0;
                if (achievementXp != null)
                {
                    DMUiToolkitOverlayDocument.SetShown(achievementXp, hasXp);
                    if (hasXp)
                        achievementXp.text = $"+{pending.XpReward} XP";
                }

                GameAudioManager.Instance?.PlayAchievementUnlock();
                yield return AnimateCard(achievementCard, 0.38f, 3.8f, 0.36f, 48f, 22f);
            }

            achievementRoutine = null;
        }

        private IEnumerator AnimateCard(
            VisualElement card,
            float slideIn,
            float hold,
            float fadeOut,
            float slideInPx,
            float slideOutPx)
        {
            if (card == null)
                yield break;

            DMUiToolkitOverlayDocument.SetShown(card, true);
            card.style.opacity = 0f;
            card.style.translate = new Translate(0, slideInPx);

            float elapsed = 0f;
            while (elapsed < slideIn)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideIn);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                card.style.opacity = eased;
                card.style.translate = new Translate(0, Mathf.Lerp(slideInPx, 0f, eased));
                yield return null;
            }

            card.style.opacity = 1f;
            card.style.translate = new Translate(0, 0);
            yield return new WaitForSecondsRealtime(hold);

            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOut);
                card.style.opacity = 1f - t;
                card.style.translate = new Translate(0, -slideOutPx * t);
                yield return null;
            }

            card.style.opacity = 0f;
            DMUiToolkitOverlayDocument.SetShown(card, false);
        }

        private static int SumSkillPointsGained(int newLevel, int levelsGained)
        {
            int total = 0;
            int firstLevel = newLevel - levelsGained + 1;
            for (int level = firstLevel; level <= newLevel; level++)
                total += PlayerProgressionManager.GetSkillPointsForLevel(level);

            return total;
        }

        private static void HideUguiToasts()
        {
            if (!DMUiToolkitHud.IsDriving)
                return;

            DMILevelUpPopupUI level = FindAnyObjectByType<DMILevelUpPopupUI>(FindObjectsInactive.Include);
            DMUiToolkitOverlayDocument.HideGameObject(level != null ? level.gameObject : null);

            DMIRequireLevelPopupUI require = FindAnyObjectByType<DMIRequireLevelPopupUI>(FindObjectsInactive.Include);
            DMUiToolkitOverlayDocument.HideGameObject(require != null ? require.gameObject : null);

            AchievementUnlockPopupUI achievement = FindAnyObjectByType<AchievementUnlockPopupUI>(FindObjectsInactive.Include);
            DMUiToolkitOverlayDocument.HideGameObject(achievement != null ? achievement.gameObject : null);
        }
    }
}
