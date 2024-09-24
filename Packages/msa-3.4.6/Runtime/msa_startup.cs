using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

public class msa_startup
{
    [Preserve]
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Startup()
    {
        MagicLeap.Soundfield.Plugin.Startup();
    }
}
