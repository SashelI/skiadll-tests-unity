using UnityEngine;
using SkiaSharp;

public class SkiaSharpTest : MonoBehaviour
{
	private Texture2D texture;
	private SKCanvas canvas;
	private SKBitmap bitmap;
	private SKSurface surface;
	private int width = 512;
	private int height = 512;

	void Start()
	{
		texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
		surface = SKSurface.Create(bitmap.Info);
		canvas = surface.Canvas;

		DrawRectangle();

		UpdateTextureFromBitmap();
	}

	void DrawRectangle()
	{
		canvas.Clear(SKColors.White);

		using (var paint = new SKPaint())
		{
			paint.Color = SKColors.Blue;
			paint.IsAntialias = true;

			SKRect rect = new SKRect(50, 50, 400, 400);
			canvas.DrawRect(rect, paint);
		}

		canvas.Flush();
	}

	void UpdateTextureFromBitmap()
	{
		var pixelData = bitmap.Pixels;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				var pixel = pixelData[y * width + x];
				texture.SetPixel(x, height - y - 1, new Color32(pixel.Red, pixel.Green, pixel.Blue, pixel.Alpha));
			}
		}

		texture.Apply();

		GetComponent<Renderer>().material.mainTexture = texture;
	}

	void OnDestroy()
	{
		surface?.Dispose();
		bitmap?.Dispose();
	}
}
