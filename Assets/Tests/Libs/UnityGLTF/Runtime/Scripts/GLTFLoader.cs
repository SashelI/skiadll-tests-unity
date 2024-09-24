using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;

public class GLTFLoader
{
	private string _path;
	private bool AppendStreamingAssets = true;

	public GLTFLoader(string path, bool appendStreamingAssets = true)
	{
		this._path = path;
		this.AppendStreamingAssets = appendStreamingAssets;
	}

	public async Task<GameObject> Load()
	{
		var gameObject = new GameObject();
		var importOptions = new ImportOptions
		{
			AsyncCoroutineHelper = gameObject.GetComponent<AsyncCoroutineHelper>() ?? gameObject.AddComponent<AsyncCoroutineHelper>()
		};

		GLTFSceneImporter sceneImporter = null;
		try
		{
			var Factory = ScriptableObject.CreateInstance<DefaultImporterFactory>();

			string fullPath;
			if (AppendStreamingAssets)
			{
				// Path.Combine treats paths that start with the separator character
				// as absolute paths, ignoring the first path passed in. This removes
				// that character to properly handle a filename written with it.
				fullPath = Path.Combine(Application.streamingAssetsPath, _path.TrimStart(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }));
			}
			else
			{
				fullPath = _path;
			}
			string directoryPath = URIHelper.GetDirectoryName(fullPath);
			importOptions.DataLoader = new FileLoader(directoryPath);
			sceneImporter = Factory.CreateSceneImporter(
				Path.GetFileName(_path),
				importOptions
				);

			sceneImporter.SceneParent = gameObject.transform;
			sceneImporter.Collider = GLTFSceneImporter.ColliderType.Mesh;
			sceneImporter.MaximumLod = 300;
			sceneImporter.Timeout = 8;
			sceneImporter.IsMultithreaded = true;
			sceneImporter.CustomShaderName = "Mixed Reality Toolkit/Standard";

			await sceneImporter.LoadSceneAsync();

			var shaderOverride = Shader.Find(sceneImporter.CustomShaderName);

			// Override the shaders on all materials if a shader is provided
			if (shaderOverride != null)
			{
				Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
				foreach (Renderer renderer in renderers)
				{
					//renderer.sharedMaterial.shader = shaderOverride;
				}
			}


			var Animations = sceneImporter.LastLoadedScene.GetComponents<Animation>();

			if (Animations.Any())
			{
				Animations.FirstOrDefault().Play();
			}
		}
		finally
		{
			if (importOptions.DataLoader != null)
			{
				sceneImporter?.Dispose();
				sceneImporter = null;
				importOptions.DataLoader = null;
			}
		}

		return gameObject;
	}

	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}

	public void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
	{
		switch (blendMode)
		{
			case BlendMode.Opaque:
				material.SetOverrideTag("RenderType", "");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = -1;
				break;

			case BlendMode.Cutout:
				material.SetOverrideTag("RenderType", "TransparentCutout");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
				material.SetInt("_ZWrite", 1);
				material.EnableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
				break;

			case BlendMode.Fade:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.EnableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				break;

			case BlendMode.Transparent:
				material.SetOverrideTag("RenderType", "Transparent");
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
				break;
		}
	}
}