using UnityEditor;

namespace FoundersPlugin.Modules
{
    public class WebAudioTemplateEditor : EditorWindow
    {
        [MenuItem("FoundersPluginTools/Audio/Setup WebGL Template")]
        public static void SetupWebGLTemplate()
        {
            WebAudioTemplate.InjectAudioScripts();
            WebAudioTemplate.CopyAudioFiles();
        }

        [MenuItem("FoundersPluginTools/Audio/Setup WebGL Template", true)]
        public static bool ValidateSetupWebGLTemplate()
        {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
        }

        [MenuItem("FoundersPluginTools/Audio/Remove WebGL Template")]
        public static void RemoveWebGLTemplate()
        {
            WebAudioTemplate.RemoveAudioScripts();
        }

        [MenuItem("FoundersPluginTools/Audio/Remove WebGL Template", true)]
        public static bool ValidateRemoveWebGLTemplate()
        {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL;
        }
    }
} 