Skill tree connection line art (Journal → Skills tab)

Drop PNGs here as Sprites (Texture Type: Sprite, Single). Unity path = Resources/UI/Skills/Tree/<name>

  skill_path_core.png    — main horizontal line (design ~256×16)
  skill_path_glow.png    — gold glow behind line (~256×32, soft alpha)
  skill_path_locked.png  — optional locked-state strip
  skill_path_ready.png   — optional ready-state strip
  skill_path_owned.png   — optional owned-state strip

Or assign sprites on:
  Assets/_Project/Resources/UI/Skills/Tree/DMSkillTreeLineProfile.asset

Tune thickness and gold glow alphas on that profile asset.
Code: DMSkillTreeLineProfile.cs (profile + DMSkillTreeLineArt), CreateHexPath in DMUiToolkitMenus.SkillsHex.cs
