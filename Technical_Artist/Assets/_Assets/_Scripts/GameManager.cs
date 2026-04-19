using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// GameManager acts as the central brain of the game.
/// It controls the global speed, manages boost states, triggers visual effects
/// (camera FOV, speed lines), tracks score, and handles the Game Over state.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Global Speed Settings")]
    public float baseSpeed = 40f;
    public float boostMultiplier = 2.0f;
    public float acceleration = 5f;
    public float speedIncreasePerSecond = 0.5f;
    public float planeLength = 10f;

    [Header("Crash Recovery")]
    public float crashSpeedMultiplier = 0.3f;
    public float crashRecoveryTime = 2.0f;

    [Header("Shader Links")]
    public string shaderSpeedProperty = "_GlobalScrollVector";

    [Header("Effect References")]
    public ArcadeCameraController cameraController;
    public SpeedLinesController speedLines;

    [Header("Obstacle Management (Scenery)")]
    public Transform[] obstacles;
    public float spawnZ = 800f;
    public float despawnZ = -400f;

    [Header("Traffic Cars (Prefabs)")]
    public GameObject[] trafficPrefabs;
    public int trafficPoolSize = 10;
    public float trafficSpeedMultiplier = 0.5f;
    public float[] lanePositionsX = { -5.5f, 0f, 5.5f };
    public Transform trafficSpawnerPoint;
    public float trafficSpawnZ = 800f;
    public float trafficDespawnZ = -200f;
    public float trafficSafeDistance = 30f;

    [Header("Lighting Settings")]
    public bool autoConfigureLighting = true;
    public Color ambientSkyColor = new Color(0.6f, 0.85f, 1.0f, 1f);
    public Color ambientEquatorColor = new Color(0.5f, 0.75f, 0.4f, 1f);
    public Color ambientGroundColor = new Color(0.3f, 0.5f, 0.2f, 1f);
    [Range(0f, 3f)]
    public float ambientIntensity = 1.5f;

    [Header("Optimization & Debugging")]
    public int targetFPS = 60;
    public bool showFPS = true;
    public bool IsBoosting { get; private set; }
    public float CurrentSpeed { get; private set; }
    public float CurrentSpeedMultiplier { get; private set; }
    public bool IsGameOver { get; private set; }
    public float Score { get; private set; }

    private bool _isRecovering = false;
    private float _recoveryTimer = 0f;

    private float accumulatedUVOffset = 0f;
    private float fpsDeltaTime = 0.0f;
    private GUIStyle fpsStyle;
    private Transform[] _activeTrafficCars;
    public Transform[] ActiveTrafficCars => _activeTrafficCars;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CurrentSpeed = baseSpeed;
        CurrentSpeedMultiplier = 1f;
        UpdateShaderSpeed();
        if (trafficPrefabs != null && trafficPrefabs.Length > 0 && trafficPoolSize > 0 && lanePositionsX.Length > 0)
        {
            _activeTrafficCars = new Transform[trafficPoolSize];
            float startSpawnZ = trafficSpawnerPoint != null ? trafficSpawnerPoint.position.z : trafficSpawnZ;

            for (int i = 0; i < trafficPoolSize; i++)
            {
                GameObject prefab = trafficPrefabs[Random.Range(0, trafficPrefabs.Length)];
                GameObject clone = Instantiate(prefab, new Vector3(0, prefab.transform.position.y, 0), Quaternion.Euler(-90f, 0f, 0f));
                clone.transform.SetParent(this.transform);
                
                _activeTrafficCars[i] = clone.transform;
                RespawnTrafficCar(clone.transform, startSpawnZ + (i * trafficSafeDistance));
            }
        }
    }

    private void Update()
    {
        if (showFPS)
        {
            fpsDeltaTime += (Time.unscaledDeltaTime - fpsDeltaTime) * 0.1f;
        }

        if (IsGameOver) return;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                SetBoostState(true);
            }
            else if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            {
                SetBoostState(false);
            }
        }
        if (!_isRecovering)
        {
            baseSpeed += speedIncreasePerSecond * Time.deltaTime;
        }
        else
        {
            _recoveryTimer -= Time.deltaTime;
            if (_recoveryTimer <= 0) _isRecovering = false;
        }
        float targetSpeed = IsBoosting ? baseSpeed * boostMultiplier : baseSpeed;
        if (_isRecovering) targetSpeed *= crashSpeedMultiplier;

        float targetMultiplier = IsBoosting ? boostMultiplier : 1f;
        if (_isRecovering) targetMultiplier *= crashSpeedMultiplier;
        CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, Time.deltaTime * acceleration);
        CurrentSpeedMultiplier = Mathf.Lerp(CurrentSpeedMultiplier, targetMultiplier, Time.deltaTime * acceleration);
        UpdateShaderSpeed();
        Score += CurrentSpeed * Time.deltaTime;
        MoveAndLoopArray(obstacles, CurrentSpeed * 2f, despawnZ, spawnZ);
        float trafficWorldSpeed = CurrentSpeed * trafficSpeedMultiplier;
        if (_activeTrafficCars != null)
        {
            foreach (Transform car in _activeTrafficCars)
            {
                if (car == null) continue;
                car.position -= Vector3.forward * trafficWorldSpeed * Time.deltaTime;
                if (car.position.z <= trafficDespawnZ)
                {
                    float currentSpawnZ = trafficSpawnerPoint != null ? trafficSpawnerPoint.position.z : trafficSpawnZ;
                    RespawnTrafficCar(car, currentSpawnZ);
                }
            }
        }
    }

    /// <summary>
    /// Respawns a traffic car in a random lane, checking for overlaps.
    /// If a lane is occupied, it pushes the car further back until it finds a safe spot.
    /// </summary>
    private void RespawnTrafficCar(Transform carToRespawn, float baseSpawnZ)
    {
        if (lanePositionsX.Length == 0) return;

        bool foundValidPos = false;
        float testZ = baseSpawnZ;
        for (int attempts = 0; attempts < 20 && !foundValidPos; attempts++)
        {
            int startLaneIdx = Random.Range(0, lanePositionsX.Length);
            for (int i = 0; i < lanePositionsX.Length; i++)
            {
                int laneIdx = (startLaneIdx + i) % lanePositionsX.Length;
                float testX = lanePositionsX[laneIdx];
                bool overlap = false;

                foreach (Transform otherCar in _activeTrafficCars)
                {
                    if (otherCar == carToRespawn || otherCar == null) continue;
                    if (Mathf.Abs(otherCar.position.x - testX) < 1.0f)
                    {
                        if (Mathf.Abs(otherCar.position.z - testZ) < trafficSafeDistance)
                        {
                            overlap = true;
                            break;
                        }
                    }
                }

                if (!overlap)
                {
                    carToRespawn.position = new Vector3(testX, carToRespawn.position.y, testZ);
                    TrafficCarBehavior behavior = carToRespawn.GetComponent<TrafficCarBehavior>();
                    if (behavior != null) behavior.ResetState();
                    
                    foundValidPos = true;
                    break;
                }
            }
            if (!foundValidPos)
            {
                testZ += trafficSafeDistance;
            }
        }
    }

    /// <summary>
    /// Moves an array of transforms backward and loops them when they pass the despawn point.
    /// </summary>
    private void MoveAndLoopArray(Transform[] arr, float speed, float despawn, float spawn)
    {
        if (arr == null) return;
        foreach (Transform obj in arr)
        {
            if (obj == null) continue;
            obj.position -= Vector3.forward * speed * Time.deltaTime;
            if (obj.position.z <= despawn)
            {
                float loopDistance = spawn - despawn;
                obj.position += new Vector3(0f, 0f, loopDistance);
            }
        }
    }

    /// <summary>
    /// Toggles the boost state, activating camera FOV kicks and speed lines.
    /// </summary>
    public void SetBoostState(bool boost)
    {
        if (IsGameOver) return;

        IsBoosting = boost;
        if (cameraController != null) cameraController.SetSpeedState(boost);
        if (speedLines != null) speedLines.SetBoosting(boost);
    }

    /// <summary>
    /// Slams the brakes and triggers a temporary slow-down rest period.
    /// </summary>
    public void TriggerCrash()
    {
        if (IsGameOver) return;
        _isRecovering = true;
        _recoveryTimer = crashRecoveryTime;
        SetBoostState(false);
    }

    /// <summary>
    /// Ends the game, stops the road, and kills movement.
    /// </summary>
    public void TriggerGameOver()
    {
        IsGameOver = true;
        CurrentSpeed = 0f;
        CurrentSpeedMultiplier = 0f;
        UpdateShaderSpeed();
        SetBoostState(false);
    }

    /// <summary>
    /// Updates the road shader speed.
    /// </summary>
    private void UpdateShaderSpeed()
    {
        float uvSpeed = CurrentSpeed / planeLength;
        accumulatedUVOffset += uvSpeed * Time.deltaTime;
        Vector4 scrollVector = new Vector4(0f, accumulatedUVOffset, 0f, 0f);
        Shader.SetGlobalVector("_GlobalScrollVector", scrollVector);
    }

    /// <summary>
    /// Configures Unity's lighting for a bright, vibrant, arcade-style look.
    /// </summary>
    private void ConfigureArcadeLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor * ambientIntensity;
        RenderSettings.ambientEquatorColor = ambientEquatorColor * ambientIntensity;
        RenderSettings.ambientGroundColor = ambientGroundColor * ambientIntensity;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.6f, 0.85f, 1.0f, 1f);
        RenderSettings.fogStartDistance = 200f;
        RenderSettings.fogEndDistance = 800f;
        Light sun = FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.color = new Color(1.0f, 0.95f, 0.8f, 1f);
            sun.intensity = 1.8f;
        }
    }

    /// <summary>
    /// Renders a highly optimized, allocation-free FPS counter to the screen if enabled.
    /// </summary>
    private void OnGUI()
    {
        if (!showFPS) return;

        if (fpsStyle == null)
        {
            fpsStyle = new GUIStyle();
            fpsStyle.alignment = TextAnchor.UpperLeft;
            fpsStyle.fontSize = Screen.height * 2 / 50;
            fpsStyle.normal.textColor = Color.yellow;
        }

        Rect rect = new Rect(20, 20, Screen.width, Screen.height * 2 / 100);
        float msec = fpsDeltaTime * 1000.0f;
        float fps = 1.0f / fpsDeltaTime;
        
#if UNITY_EDITOR
        int drawCalls = UnityEditor.UnityStats.drawCalls;
        int batches = UnityEditor.UnityStats.batches;
        int triangles = UnityEditor.UnityStats.triangles;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)\nBatches: {2}\nDraw Calls: {3}\nTriangles: {4}", msec, fps, batches, drawCalls, triangles);
#else
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
#endif
        
        GUI.Label(rect, text, fpsStyle);
    }
}



