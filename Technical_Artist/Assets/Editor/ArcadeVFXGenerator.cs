using UnityEngine;
using UnityEditor;

public class ArcadeVFXGenerator : Editor
{
    private static string vfxDir = "Assets/_Assets/_Prefabs/VFX";

    [MenuItem("Tools/Generate Arcade VFX Prefabs")]
    public static void GenerateVFX()
    {
        EnsureDirectory();

        CreateDustPuffVFX();
        CreateSparkVFX();
        CreateSpeedLinesVFX();

        AssetDatabase.SaveAssets();
    }

    private static void EnsureDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Assets")) AssetDatabase.CreateFolder("Assets", "_Assets");
        if (!AssetDatabase.IsValidFolder("Assets/_Assets/_Prefabs")) AssetDatabase.CreateFolder("Assets/_Assets", "_Prefabs");
        if (!AssetDatabase.IsValidFolder(vfxDir)) AssetDatabase.CreateFolder("Assets/_Assets/_Prefabs", "VFX");
    }

    private static Material GetParticleMaterial(string name, bool additive)
    {
        string matPath = vfxDir + "/" + name + "_Mat.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            
            mat = new Material(shader);

            if (additive)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 1);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
            }
            else
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
            }
            Texture2D defaultTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
            if (defaultTex == null) defaultTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-ParticleSystem.psd");
            if (defaultTex == null) defaultTex = Resources.GetBuiltinResource<Texture2D>("Default-Particle.psd");
            
            if (defaultTex != null)
            {
                mat.SetTexture("_BaseMap", defaultTex);
                mat.SetTexture("_MainTex", defaultTex);
            }

            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
    }

    private static void CreateDustPuffVFX()
    {
        string name = "VFX_DustPuff";
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.6f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        col.color = grad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = GetParticleMaterial(name, false);

        string localPath = vfxDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, localPath);
        DestroyImmediate(go);
    }

    private static void CreateSpeedLinesVFX()
    {
        string name = "VFX_SpeedLines";
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(50f, 80f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(1f, 1f, 1f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 30;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = 12f;
        shape.radiusThickness = 0.1f;
        shape.position = new Vector3(0, 0, 80f);
        shape.rotation = new Vector3(0, 180f, 0);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0f), new Keyframe(0.2f, 1f), new Keyframe(1, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 4.0f;
        renderer.velocityScale = 0.1f;
        
        renderer.sharedMaterial = GetParticleMaterial(name, true);

        string localPath = vfxDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, localPath);
        DestroyImmediate(go);
    }

    private static void CreateSparkVFX()
    {
        string name = "VFX_CollisionSparks";
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(15f, 30f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startColor = new Color(1f, 0.8f, 0.1f, 1f); 
        main.gravityModifier = 1.5f; 
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20, 30) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(new Color(1f, 0.3f, 0f), 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        col.color = grad;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.0f;
        renderer.velocityScale = 0.05f;
        
        renderer.sharedMaterial = GetParticleMaterial(name, true);

        string localPath = vfxDir + "/" + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, localPath);
        DestroyImmediate(go);
    }
}



