using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Project.Core
{
    public static class SaveSlotScreenshotUtility
    {
        public const int ThumbnailWidth = 160;
        public const int ThumbnailHeight = 90;

        private const string ScreenshotFileNameFormat = "savegame_slot{0}_preview.png";

        public static string GetScreenshotPath(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, GameSaveSystem.SlotCount - 1);
            return Path.Combine(Application.persistentDataPath, string.Format(ScreenshotFileNameFormat, slotIndex));
        }

        public static bool HasScreenshot(int slotIndex)
        {
            return File.Exists(GetScreenshotPath(slotIndex));
        }

        public static void SaveScreenshot(int slotIndex, Texture2D screenshot)
        {
            if (screenshot == null)
                return;

            slotIndex = Mathf.Clamp(slotIndex, 0, GameSaveSystem.SlotCount - 1);
            Texture2D thumbnail = ScaleTexture(screenshot, ThumbnailWidth, ThumbnailHeight);
            try
            {
                File.WriteAllBytes(GetScreenshotPath(slotIndex), thumbnail.EncodeToPNG());
            }
            finally
            {
                if (thumbnail != screenshot)
                    UnityEngine.Object.Destroy(thumbnail);
            }
        }

        public static Texture2D LoadScreenshot(int slotIndex)
        {
            string path = GetScreenshotPath(slotIndex);
            if (!File.Exists(path))
                return null;

            try
            {
                byte[] pngData = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(pngData))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.name = $"SaveSlotPreview_{slotIndex}";
                return texture;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"SaveSlotScreenshotUtility: Failed to load slot {slotIndex} preview. {exception.Message}");
                return null;
            }
        }

        public static Texture2D CaptureGameplayScreenshot()
        {
            Texture2D capture = ScreenCapture.CaptureScreenshotAsTexture(ScreenCapture.StereoScreenCaptureMode.LeftEye);
            if (capture == null)
                return null;

            Texture2D thumbnail = ScaleTexture(capture, ThumbnailWidth, ThumbnailHeight);
            UnityEngine.Object.Destroy(capture);
            return thumbnail;
        }

        public static IEnumerator CaptureAfterFrame(Action<Texture2D> onCaptured)
        {
            yield return new WaitForEndOfFrame();
            onCaptured?.Invoke(CaptureGameplayScreenshot());
        }

        private static Texture2D ScaleTexture(Texture2D source, int width, int height)
        {
            if (source == null)
                return null;

            if (source.width == width && source.height == height)
                return source;

            RenderTexture renderTarget = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(source, renderTarget);
            RenderTexture.active = renderTarget;

            Texture2D scaled = new Texture2D(width, height, TextureFormat.RGBA32, false);
            scaled.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            scaled.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTarget);
            return scaled;
        }
    }
}
