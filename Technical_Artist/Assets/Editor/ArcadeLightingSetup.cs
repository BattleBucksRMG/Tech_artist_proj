using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ArcadeLightingSetup : Editor
{
    [MenuItem("Tools/Apply Arcade Lighting Settings")]
    public static void ApplySettings()
    {
        Light sun = RenderSettings.sun;
        if (sun == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional) { sun = l; break; }
            }
        }

        if (sun != null)
        {
            Undo.RecordObject(sun, "Apply Arcade Lighting");
            
            Color warmTone;
            if (!ColorUtility.TryParseHtmlString("#FFF0D4", out warmTone)) warmTone = new Color(1f, 0.94f, 0.83f);
            
            sun.color = warmTone;
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.85f, 0.9f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.7f, 0.8f, 0.9f); 
        RenderSettings.ambientGroundColor = new Color(0.5f, 0.6f, 0.5f);
        RenderSettings.ambientIntensity = 1.2f;
        Volume volume = Object.FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            GameObject volumeGo = new GameObject("Global Post-Processing");
            volume = volumeGo.AddComponent<Volume>();
        }
        volume.isGlobal = true;

        VolumeProfile profile = volume.sharedProfile;
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, "Assets/ArcadePostProcessing.asset");
            volume.sharedProfile = profile;
        }
        Bloom bloom;
        if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(1.5f);
        bloom.threshold.Override(1.0f);
        bloom.scatter.Override(0.6f);
        ColorAdjustments colorAdj;
        if (!profile.TryGet(out colorAdj)) colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.saturation.Override(20f);
        colorAdj.contrast.Override(15f);
        Tonemapping tonemap;
        if (!profile.TryGet(out tonemap)) tonemap = profile.Add<Tonemapping>();
        tonemap.active = true;
        tonemap.mode.Override(TonemappingMode.ACES);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
    }
}



