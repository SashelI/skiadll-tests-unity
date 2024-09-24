using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace MagicLeap.Soundfield
{
    namespace Gizmos
    {
        [AddComponentMenu("Magic Leap/RuntimeGizmos/MLRuntimeGizmoManager")]
        [HelpURL("https://developer-docs.magicleap.cloud/docs/guides/unity/soundfield-audio/soundfield-plugin#runtime-gizmos")]
        public class MLRuntimeGizmoManager : MonoBehaviour
        {
            #if UNITY_EDITOR
            public MLPointSource selectedPointSource = null;
            #else
            private MLPointSource selectedPointSource = null;
            #endif
            public bool showMLPointSources = true;

            void Start()
            {
                RenderPipelineManager.endContextRendering += OnEndContextRendering;
            }

            void OnDestroy()
            {
                RenderPipelineManager.endContextRendering -= OnEndContextRendering;
            }

            public void SetSelectedPointSource(MLPointSource ps)
            {
                selectedPointSource = ps;
            }

            void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
            {
                if(selectedPointSource)
                {
                    selectedPointSource.DrawGizmo(showMLPointSources);
                }
                else
                {
                    MLPointSource[] sources = GameObject.FindObjectsOfType<MLPointSource>();
                    foreach (MLPointSource source in sources)
                    {
                        source.DrawGizmo(showMLPointSources);
                    }
                }
            }
        }
    }
}