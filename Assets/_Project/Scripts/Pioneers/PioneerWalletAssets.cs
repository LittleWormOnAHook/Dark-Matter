using UnityEngine;

namespace Project.Pioneers
{
    public static class PioneerWalletAssets
    {
        private const string SkullIconAssetPath =
            "Assets/HONETi/TexturesThemeCommon/Icons/256x256px/Skull256x256.png";

        private static Sprite cachedSkullIcon;

        public static Sprite SkullIcon256
        {
            get
            {
                if (cachedSkullIcon != null)
                    return cachedSkullIcon;

#if UNITY_EDITOR
                cachedSkullIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(SkullIconAssetPath);
                if (cachedSkullIcon == null)
                {
                    Texture2D texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(SkullIconAssetPath);
                    if (texture != null)
                    {
                        cachedSkullIcon = Sprite.Create(
                            texture,
                            new Rect(0f, 0f, texture.width, texture.height),
                            new Vector2(0.5f, 0.5f),
                            100f);
                    }
                }
#endif
                return cachedSkullIcon;
            }
        }
    }
}
