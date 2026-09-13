using UnityEngine;

namespace RbnVisualisation.GenomePath
{
    internal static class GenomePathUiCircleSprite
    {
        private static Sprite cachedCircleSprite;
        private static Sprite cachedSquareSprite;

        public static Sprite Get()
        {
            if (cachedCircleSprite != null)
            {
                return cachedCircleSprite;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            float center = (size - 1) * 0.5f;
            float radiusSquared = center * center;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distSquared = dx * dx + dy * dy;
                    byte alpha = distSquared <= radiusSquared ? (byte)255 : (byte)0;
                    pixels[(y * size) + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            cachedCircleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            cachedCircleSprite.hideFlags = HideFlags.HideAndDontSave;
            return cachedCircleSprite;
        }

        public static Sprite GetSquare()
        {
            if (cachedSquareSprite != null)
            {
                return cachedSquareSprite;
            }

            const int size = 4;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            cachedSquareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);
            cachedSquareSprite.hideFlags = HideFlags.HideAndDontSave;
            return cachedSquareSprite;
        }
    }
}
