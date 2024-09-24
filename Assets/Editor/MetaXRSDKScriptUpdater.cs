using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Editor
{
	public class MetaXRSDKScriptUpdater : EditorWindow
	{
		private readonly Dictionary<string, List<string>> _database = new() //file path, original line, modified line, and optional previous line for precision
	    {
			{"OVRManager 1",
				new()
				{
					"Packages/com.meta.xr.sdk.core/Scripts/OVRManager.cs", "private void InitOVRManager()", "private void InitOVRManager(bool update = false)"
				}
			},

			{"OVRManager 2",
				new()
				{
					"Packages/com.meta.xr.sdk.core/Scripts/OVRManager.cs", "if (instance != null)", "if (instance != null && !update)"
				}
			},

			{"OVRManager 3",
				new()
				{
					"Packages/com.meta.xr.sdk.core/Scripts/OVRManager.cs", "InitOVRManager();", "InitOVRManager(true);" , ", we can init OVRManager"
				}
			},

			{ "OVRHands 1",
				new()
				{
					"Packages/com.meta.xr.sdk.core/Scripts/Util/OVRHand.cs",
					"PointerPose.localPosition = _handState.PointerPose.Position.FromFlippedZVector3f();",
					"PointerPose.localPosition = _handState.PointerPose.Position.FromFlippedZVector3f() + new Vector3(0f, 0.035f, 0.016f);"
				}
			},

			{
				"MetaXrFeatureEnabler 1",
				new()
				{
					"Packages/com.meta.xr.sdk.core/Editor/OpenXRFeatures/MetaXRFeatureEnabler.cs", "#if USING_XR_SDK_OPENXR" , "/*"
				}
			},

			{
				"MetaXrFeatureEnabler 2",
				new()
				{
					"Packages/com.meta.xr.sdk.core/Editor/OpenXRFeatures/MetaXRFeatureEnabler.cs", "#endif" , "*/" , "#endif"
				}
			}
		};

		private const string META_XR_ALL_IN_ONE = "com.meta.xr.sdk.all";
		private const string META_XR_CORE = "com.meta.xr.sdk.core";

		private AddRequest _addRequest;
		private ListRequest _listRequest;
		private string _updatedPackageName = string.Empty;

		private bool _hasRestoredBeforeUpdate = false;
		private bool _hasUpdated = false;
		private bool _hasMovedAfterUpdate = false;

		[MenuItem("Tools/MetaXR SDK Script Updater")]
		public static void ShowWindow()
		{
			GetWindow<MetaXRSDKScriptUpdater>("MetaXR SDK Updater");
		}

		private void OnGUI()
		{
#if UNITY_2019_3_OR_NEWER
			GUILayout.Label("Packages Updater", EditorStyles.boldLabel);

			EditorGUI.BeginDisabledGroup(!(_hasUpdated == false && _hasRestoredBeforeUpdate == false));
			if (GUILayout.Button("Restore MetaXR Core package before updating"))
			{
				MovePackageToPackageCache();
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!(_hasUpdated == false && _hasRestoredBeforeUpdate));
			if (GUILayout.Button("Update MetaXR All-in-one package"))
			{
				UpdatePackage();
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup(!(_hasUpdated && _hasRestoredBeforeUpdate));
			if (GUILayout.Button("Make MetaXR Core package mutable"))
			{
				MoveUpdatedToPackages();
			}
			EditorGUI.EndDisabledGroup();
#endif

			GUILayout.Label("Script Updater", EditorStyles.boldLabel);

			if (GUILayout.Button("Update MetaXR Core Scripts"))
			{
				ModifyScripts();
			}
			if (GUILayout.Button("Restore MetaXR Core Scripts"))
			{
				RestoreScripts();
			}
		}

		private void ModifyScripts()
		{
			foreach (var script in _database)
			{
				Debug.Log($"Modifying script {Path.GetFileName(script.Value[0])}...");
				ModifyScriptAtPath(script.Key, script.Value[0], script.Value[1], script.Value[2]);
			}

			Debug.Log("Script update done.");
		}

		private void RestoreScripts()
		{
			foreach (var script in _database)
			{
				Debug.Log($"Restoring script {Path.GetFileName(script.Value[0])}...");
				ModifyScriptAtPath(script.Key, script.Value[0], script.Value[2], script.Value[1]);
			}

			Debug.Log("Script restoration done.");
		}

		private void ModifyScriptAtPath(string key, string path, string originalText, string modifiedText)
		{
			if (!System.IO.File.Exists(path))
			{
				Debug.LogError($"Script not found: {path}");
				return;
			}

			string[] scriptContent = System.IO.File.ReadAllLines(path);

			for (int i = 0; i < scriptContent.Length; i++)
			{
				string line = scriptContent[i];
				if (line.Contains(originalText) && line.EndsWith(originalText))
				{
					_database.TryGetValue(key, out var value);
					if (value is { Count: > 3 }) //We need to check previous line to replace the right one
					{
						if (scriptContent[i - 1].EndsWith(value[3]))
						{
							scriptContent[i] = line.Replace(originalText, modifiedText);
						}
					}
					else
					{
						scriptContent[i] = line.Replace(originalText, modifiedText);
					}
				}
			}

			System.IO.File.WriteAllLines(path, scriptContent);

			AssetDatabase.Refresh();
			Debug.Log($"Script modified successfully: {path}");
		}

		private void MovePackageToPackageCache()
		{
			_hasUpdated = false;
			_hasMovedAfterUpdate = false;

			Debug.Log("Moving old CoreSDK from mutable Packages folder to PackageCache...");
			_hasRestoredBeforeUpdate = MoveOldFolder(META_XR_CORE);

			if (_hasRestoredBeforeUpdate)
			{
				Debug.Log("Package successfully restored to cache. Unity will reload.");
			}
			else
			{
				Debug.LogError("Restoration failed : Please close unity and manually delete 'com.meta.xr.sdk.core@65.0.0\\Plugins\\Win64OpenXR\\OVRPlugin.dll'." +
						  "\r\n Update stopped.");
			}
		}

		private void UpdatePackage()
		{
			_listRequest = Client.List(true);
			EditorApplication.update += CheckListRequestCompletion;
		}

		private void CheckListRequestCompletion()
		{
			if (_listRequest.IsCompleted)
			{
				if (_listRequest.Status == StatusCode.Success)
				{
					foreach (var package in _listRequest.Result)
					{
						if (package.name == META_XR_ALL_IN_ONE)
						{
							Debug.Log($"{META_XR_ALL_IN_ONE} Current version: {package.version}");
						}
					}

					Debug.Log("Please wait until packages are all updated...");

					_addRequest = Client.Add(META_XR_ALL_IN_ONE);
					EditorApplication.update += CheckAddRequestCompletion;
				}
				else if (_listRequest.Status >= StatusCode.Failure)
				{
					Debug.LogError($"Failed to list {META_XR_ALL_IN_ONE} packages: {_listRequest.Error.message}");
				}

				EditorApplication.update -= CheckListRequestCompletion;
			}
		}

		private void CheckAddRequestCompletion()
		{
			if (_addRequest.IsCompleted)
			{
				if (_addRequest.Status == StatusCode.Success)
				{
					_hasUpdated = true;
					Debug.Log($"Successfully updated package {_addRequest.Result.packageId} to version {_addRequest.Result.version}. Wait for Unity reload.");
				}
				else if (_addRequest.Status >= StatusCode.Failure)
				{
					_hasUpdated = false;
					Debug.LogError($"Failed to add package: {_addRequest.Error.message}");
				}

				EditorApplication.update -= CheckAddRequestCompletion;
			}
		}

		private void MoveUpdatedToPackages()
		{
			Debug.Log($"Moving Core sdk to mutable Packages folder...");

			_hasMovedAfterUpdate = MovePackage(META_XR_CORE);

			if (_hasMovedAfterUpdate)
			{
				Debug.Log($"Update is done and successful. Please wait for Unity Reload.");
			}
			else
			{
				Debug.LogError($"Update has failed.");
			}

			_hasUpdated = false;
			_hasRestoredBeforeUpdate = false;
		}

		private bool MovePackage(string packageName)
		{
			try
			{
				var projectDirectory = Directory.GetParent(Application.dataPath);
				if (projectDirectory != null)
				{
					string projectPath = projectDirectory.FullName;

					// Paths to PackageCache and Packages directories
					string packageCachePath = Path.Combine(projectPath, "Library/PackageCache");
					string packagesPath = Path.Combine(projectPath, "Packages");

					foreach (var directory in Directory.GetDirectories(packageCachePath))
					{
						var dirName = Path.GetFileName(directory);
						if (!string.IsNullOrWhiteSpace(dirName) && dirName.StartsWith(packageName))
						{
							packageCachePath = Path.Combine(packageCachePath, dirName);
							packagesPath = Path.Combine(packagesPath, dirName);
							_updatedPackageName = dirName;
							break;
						}
					}

					if (!Directory.Exists(packageCachePath))
					{
						Debug.LogError($"Package not found in PackageCache: {packageCachePath}");
						return false;
					}

					if (Directory.Exists(packagesPath))
					{
						Debug.LogWarning($"Package already exists in Packages folder: {packagesPath}. Deleting from PackageCache folder...");
						Directory.Delete(packageCachePath, true);
						return false;
					}

					// Move the package folder
					Directory.Move(packageCachePath, packagesPath);
					Debug.Log($"Package moved from {packageCachePath} to {packagesPath}");

					// Refresh the AssetDatabase
					AssetDatabase.Refresh();

					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error moving package: {ex.Message}");
				return false;
			}
		}

		private bool MoveOldFolder(string packageName)
		{
			try
			{
				var projectDirectory = Directory.GetParent(Application.dataPath);
				if (projectDirectory != null)
				{
					string projectPath = projectDirectory.FullName;

					// Paths to PackageCache and Packages directories
					string packageCachePath = Path.Combine(projectPath, "Library/PackageCache");
					string packagesPath = Path.Combine(projectPath, "Packages");

					bool hasFoundPackage = false;

					foreach (var directory in Directory.GetDirectories(packagesPath))
					{
						var dirName = Path.GetFileName(directory);
						if (!string.IsNullOrWhiteSpace(dirName) && dirName.StartsWith(packageName))
						{
							packageCachePath = Path.Combine(packageCachePath, dirName);
							packagesPath = Path.Combine(packagesPath, dirName);

							hasFoundPackage = true;
							Debug.Log($"Package found in {packagesPath}");
							break;
						}
					}

					if (hasFoundPackage)
					{
						if (!Directory.Exists(packagesPath))
						{
							Debug.Log($"Package not found in Packages: {packagesPath}");
							return true;
						}

						if (Directory.Exists(packageCachePath))
						{
							Debug.LogWarning($"Package already exists in PackageCache folder: {packageCachePath}. Deleting from Packages folder...");
							Directory.Delete(packagesPath, true);
							return true;
						}

						// Move the package folder
						Directory.Move(packagesPath, packageCachePath);
						Debug.Log($"Package moved from {packagesPath} to {packageCachePath}");

						return true;
					}
					else
					{
						Debug.Log($"Package not found in Packages: {packagesPath}");
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Error moving package: {ex.Message}");
				return false;
			}
		}
	}
}
