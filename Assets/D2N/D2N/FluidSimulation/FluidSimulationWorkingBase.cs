using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections;

public class FluidSimulationWorkingBase : MonoBehaviour
{
    [Header("Simulation Settings")]
    public int resolution = 100;
    public float simHeight = 1.1f;
    public float density = 1000.0f;
    public float dt = 1.0f / 60.0f;
    public int numIters = 40;
    public float overRelaxation = 1.9f;
    public float gravity = 0.0f;
    
    [Header("Visualization")]
    public bool showPressure = false;
    public bool showSmoke = true;
    public bool showVelocities = false;
    public bool showStreamlines = false;
    //public bool showObstacle = true;
    
    // [Header("Obstacle")]
    // public float obstacleRadius = 0.15f;
    // public Vector2 obstaclePos = new Vector2(0.4f, 0.5f);
    
    [Header("Scene Type")]
    public SceneType sceneType = SceneType.WindTunnel;
    
    // Fluid fields
    private float[] u, v, newU, newV, p, s, m, newM;
    private int numX, numY, numCells;
    private float h;
    private float simWidth;
    
    // Rendering
    private Texture2D fluidTexture;
    private Color[] pixels;
    private Renderer quadRenderer;
    
    // UI
    private bool mouseDown = false;
    private Camera cam;

    public bool IsInitialized { get; private set; }

    [Header("Debug Rendering")]
    public Material debugLineMaterial;
    private Transform quadTransform;

    [Header("Aerodynamic Proxies")]
    private float liftProxy;
    private float circulation;
    private float dragProxy; // from Part 3

    [Header("Averaged Aerodynamic Metrics")]
    private float avgLiftProxy;
    private float avgCirculation;

    float liftSum;
    float circulationSum;
    int sampleCount;

    [Header("Sampling Control")]
    public int settleFrames = 50;   // frames to ignore
    public int sampleFrames = 60;   // frames to average over

    int frameCounter;

    [Header("Snapshot UI")]
    public Font uiFont;   // assign any Unity font in inspector


////////////////////// UI Elements ///////////////////////

    public void ToggleSmoke()
    {
        if (showSmoke == true)
        {
            showSmoke = false;
        }
        else
        {
            showSmoke = true;
        }
    }

    public void TogglePressure()
    {
        if (showPressure == true)
        {
            showPressure = false;
        }
        else
        {
            showPressure = true;
        }
    }

    public void ToggleVelocities()
    {
        if (showVelocities == true)
        {
            showVelocities = false;
        }
        else
        {
            showVelocities = true;
        }
    }

    public void ToggleStreamlines()
    {
        if (showStreamlines == true)
        {
            showStreamlines = false;
        }
        else
        {
            showStreamlines = true;
        }
    }
///////////////////////////////////////////////////////////

    public enum SceneType
    {
        Tank = 0,
        WindTunnel = 1,
        Paint = 2,
        HiresTunnel = 3
    }

    void Start()
    {
        cam = Camera.main;
        SetupSimulation();
        SetupVisualization();
    }

    bool PointInPolygon(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        int count = poly.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 vi = poly[i];
            Vector2 vj = poly[j];

            if (((vi.y > p.y) != (vj.y > p.y)) &&
                (p.x < (vj.x - vi.x) * (p.y - vi.y) / (vj.y - vi.y) + vi.x))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public void SetPolygonObstacle(List<Vector2> vertices)
    {
        SetupSimulation();
        int n = numY;

        // Reset interior to fluid (keep domain boundaries)
        for (int i = 1; i < numX - 1; i++)
            for (int j = 1; j < numY - 1; j++)
                s[i * n + j] = 1.0f;

        // Rasterize polygon
        for (int i = 1; i < numX - 1; i++)
        {
            for (int j = 1; j < numY - 1; j++)
            {
                Vector2 c = new Vector2(
                    (i + 0.5f) * h,
                    (j + 0.5f) * h
                );

                if (PointInPolygon(c, vertices))
                {
                    s[i * n + j] = 0.0f;

                    u[i * n + j] = 0;
                    u[(i + 1) * n + j] = 0;
                    v[i * n + j] = 0;
                    v[i * n + j + 1] = 0;
                }
            }
        }
    }


    public Vector2 WorldToSim(Vector3 world)
    {
        return new Vector2(
            world.x + simWidth * 0.5f,
            world.y + simHeight * 0.5f
        );
    }

    // public Vector2 WorldToSim(Vector3 world)
    // {
    //     // Convert world position into simulation quad local space
    //     Vector3 local = transform.InverseTransformPoint(world);
    //     Debug.LogError(local);

    //     // Solver domain size
    //     float halfW = simWidth * 0.5f;
    //     float halfH = simHeight * 0.5f;

    //     // Convert from centered local space → bottom-left solver space
    //     return new Vector2(
    //         local.x + halfW,
    //         local.y + halfH
    //     );
    // }

    // public Vector2 WorldToSim(Vector3 world)
    // {
    //     // Quad basis vectors in world space
    //     Vector3 origin = transform.position;
    //     Vector3 right  = transform.right;
    //     Vector3 up     = transform.up;

    //     // Vector from quad center to point
    //     Vector3 d = world - origin;

    //     // Coordinates in quad local basis (signed distances)
    //     float xLocal = Vector3.Dot(d, right);
    //     float yLocal = Vector3.Dot(d, up);

    //     // Quad half extents in world units
    //     float halfW = simWidth  * 0.5f;
    //     float halfH = simHeight * 0.5f;

    //     // Convert from centered quad space → solver space
    //     return new Vector2(
    //         xLocal + halfW,
    //         yLocal + halfH
    //     );
    // }


    public void SetupSimulation()
    {
        if (sceneType == SceneType.Tank)
            resolution = 50;
        else if (sceneType == SceneType.HiresTunnel)
            resolution = 200;
        
        simWidth = simHeight * ((float)Screen.width / Screen.height);
        h = simHeight / resolution;
        
        numX = Mathf.FloorToInt(simWidth / h) + 2;
        numY = resolution + 2;
        numCells = numX * numY;
        
        // Initialize arrays
        u = new float[numCells];
        v = new float[numCells];
        newU = new float[numCells];
        newV = new float[numCells];
        p = new float[numCells];
        s = new float[numCells];
        m = new float[numCells];
        newM = new float[numCells];
        
        // Fill with default values
        for (int i = 0; i < numCells; i++)
        {
            m[i] = 1.0f;
        }
        
        SetupScene();
        IsInitialized = true;

        // // APPLY POLYGON OBSTACLE (if any)
        // PolygonObstacle poly = FindObjectOfType<PolygonObstacle>();
        // if (poly != null)
        // {
        //     SetPolygonObstacle(poly.GetVertices(this));
        // }
    }

    void SetupScene()
    {
        int n = numY;
        
        if (sceneType == SceneType.Tank)
        {
            // Tank setup
            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    float solid = 1.0f; // fluid
                    if (i == 0 || i == numX - 1 || j == 0)
                        solid = 0.0f; // solid
                    s[i * n + j] = solid;
                }
            }
            gravity = -9.81f;
            showPressure = true;
            showSmoke = false;
        }
        else if (sceneType == SceneType.WindTunnel || sceneType == SceneType.HiresTunnel)
        {
            // Wind tunnel setup
            float inVel = 2.0f;
            for (int i = 0; i < numX; i++)
            {
                for (int j = 0; j < numY; j++)
                {
                    float solid = 1.0f; // fluid
                    if (i == 0 || j == 0 || j == numY - 1)
                        solid = 0.0f; // solid
                    s[i * n + j] = solid;
                    
                    if (i == 1)
                    {
                        u[i * n + j] = inVel;
                    }
                }
            }
            
            // Create inlet pipe
            int pipeH = Mathf.FloorToInt(0.1f * numY);
            int minJ = Mathf.FloorToInt(0.5f * numY - 0.5f * pipeH);
            int maxJ = Mathf.FloorToInt(0.5f * numY + 0.5f * pipeH);
            
            for (int j = minJ; j < maxJ; j++)
                m[j] = 0.0f;
                
            //SetObstacle(obstaclePos.x, obstaclePos.y, true);
            
            gravity = 0.0f;
            showPressure = false;
            showSmoke = true;
            
            if (sceneType == SceneType.HiresTunnel)
            {
                dt = 1.0f / 120.0f;
                numIters = 100;
                showPressure = true;
            }
        }
        else if (sceneType == SceneType.Paint)
        {
            gravity = 0.0f;
            overRelaxation = 1.0f;
            showPressure = false;
            showSmoke = true;
            //obstacleRadius = 0.1f;
        }
    }

    void SetupVisualization()
    {
        // Create quad for rendering
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.position = new Vector3(0, 0, 1);
        quad.transform.localScale = new Vector3(simWidth, simHeight, 1);
        quadTransform = quad.transform;
        
        quadRenderer = quad.GetComponent<Renderer>();
        
        // Create texture
        fluidTexture = new Texture2D(numX, numY, TextureFormat.RGBA32, false);
        fluidTexture.filterMode = FilterMode.Bilinear;
        pixels = new Color[numX * numY];
        
        quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        quadRenderer.material.mainTexture = fluidTexture;
    }

    void Update()
    {
        //HandleInput();
        Simulate();
        Render();
    }

    // void HandleInput()
    // {
    //     if (Input.GetMouseButtonDown(0))
    //     {
    //         mouseDown = true;
    //         Vector2 worldPos = GetWorldMousePosition();
    //         SetObstacle(worldPos.x, worldPos.y, true);
    //     }
    //     else if (Input.GetMouseButtonUp(0))
    //     {
    //         mouseDown = false;
    //     }
    //     else if (Input.GetMouseButton(0) && mouseDown)
    //     {
    //         Vector2 worldPos = GetWorldMousePosition();
    //         SetObstacle(worldPos.x, worldPos.y, false);
    //     }
    // }

    // Vector2 GetWorldMousePosition()
    // {
    //     Vector3 mousePos = Input.mousePosition;
    //     Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane + 1));
    //     return new Vector2(worldPos.x + simWidth * 0.5f, worldPos.y + simHeight * 0.5f);
    // }

    void Simulate()
    {
        Integrate(dt, gravity);
        
        // Reset pressure
        Array.Clear(p, 0, numCells);
        SolveIncompressibility(numIters, dt);
        
        Extrapolate();
        AdvectVel(dt);
        AdvectSmoke(dt);

        ComputeLiftProxy();
        ComputeCirculation();

        frameCounter++;

        if (frameCounter > settleFrames &&
            frameCounter <= settleFrames + sampleFrames)
        {
            liftSum += liftProxy;
            circulationSum += circulation;
            sampleCount++;
        }

        if (frameCounter == settleFrames + sampleFrames)
        {
            avgLiftProxy = liftSum / sampleCount;
            avgCirculation = circulationSum / sampleCount;
            Debug.LogError(avgLiftProxy);
            Debug.LogError(avgCirculation);
            CaptureComparisonSnapshot();
        }

    }

    public void ResetAveraging()
    {
        liftSum = 0f;
        circulationSum = 0f;
        sampleCount = 0;
        frameCounter = 0;

        avgLiftProxy = 0f;
        avgCirculation = 0f;
    }


    void Integrate(float dt, float gravity)
    {
        int n = numY;
        for (int i = 1; i < numX; i++)
        {
            for (int j = 1; j < numY - 1; j++)
            {
                if (s[i * n + j] != 0.0f && s[i * n + j - 1] != 0.0f)
                    v[i * n + j] += gravity * dt;
            }
        }
    }

    void SolveIncompressibility(int numIters, float dt)
    {
        int n = numY;
        float cp = density * h / dt;
        
        for (int iter = 0; iter < numIters; iter++)
        {
            for (int i = 1; i < numX - 1; i++)
            {
                for (int j = 1; j < numY - 1; j++)
                {
                    if (s[i * n + j] == 0.0f)
                        continue;
                        
                    float sx0 = s[(i - 1) * n + j];
                    float sx1 = s[(i + 1) * n + j];
                    float sy0 = s[i * n + j - 1];
                    float sy1 = s[i * n + j + 1];
                    float sSum = sx0 + sx1 + sy0 + sy1;
                    
                    if (sSum == 0.0f)
                        continue;
                        
                    float div = u[(i + 1) * n + j] - u[i * n + j] +
                              v[i * n + j + 1] - v[i * n + j];
                              
                    float pressure = -div / sSum;
                    pressure *= overRelaxation;
                    p[i * n + j] += cp * pressure;
                    
                    u[i * n + j] -= sx0 * pressure;
                    u[(i + 1) * n + j] += sx1 * pressure;
                    v[i * n + j] -= sy0 * pressure;
                    v[i * n + j + 1] += sy1 * pressure;
                }
            }
        }
    }

    void Extrapolate()
    {
        int n = numY;
        for (int i = 0; i < numX; i++)
        {
            u[i * n + 0] = u[i * n + 1];
            u[i * n + numY - 1] = u[i * n + numY - 2];
        }
        for (int j = 0; j < numY; j++)
        {
            v[0 * n + j] = v[1 * n + j];
            v[(numX - 1) * n + j] = v[(numX - 2) * n + j];
        }
    }

    float SampleField(float x, float y, int field)
    {
        int n = numY;
        float h1 = 1.0f / h;
        float h2 = 0.5f * h;
        
        x = Mathf.Clamp(x, h, numX * h);
        y = Mathf.Clamp(y, h, numY * h);
        
        float dx = 0.0f, dy = 0.0f;
        float[] f = null;
        
        switch (field)
        {
            case 0: // U_FIELD
                f = u; dy = h2; break;
            case 1: // V_FIELD
                f = v; dx = h2; break;
            case 2: // S_FIELD
                f = m; dx = h2; dy = h2; break;
        }
        
        int x0 = Mathf.Min(Mathf.FloorToInt((x - dx) * h1), numX - 1);
        float tx = ((x - dx) - x0 * h) * h1;
        int x1 = Mathf.Min(x0 + 1, numX - 1);
        
        int y0 = Mathf.Min(Mathf.FloorToInt((y - dy) * h1), numY - 1);
        float ty = ((y - dy) - y0 * h) * h1;
        int y1 = Mathf.Min(y0 + 1, numY - 1);
        
        float sx = 1.0f - tx;
        float sy = 1.0f - ty;
        
        float val = sx * sy * f[x0 * n + y0] +
                   tx * sy * f[x1 * n + y0] +
                   tx * ty * f[x1 * n + y1] +
                   sx * ty * f[x0 * n + y1];
                   
        return val;
    }

    float AvgU(int i, int j)
    {
        int n = numY;
        return (u[i * n + j - 1] + u[i * n + j] +
                u[(i + 1) * n + j - 1] + u[(i + 1) * n + j]) * 0.25f;
    }

    float AvgV(int i, int j)
    {
        int n = numY;
        return (v[(i - 1) * n + j] + v[i * n + j] +
                v[(i - 1) * n + j + 1] + v[i * n + j + 1]) * 0.25f;
    }

    void AdvectVel(float dt)
    {
        Array.Copy(u, newU, numCells);
        Array.Copy(v, newV, numCells);
        
        int n = numY;
        float h2 = 0.5f * h;
        
        for (int i = 1; i < numX; i++)
        {
            for (int j = 1; j < numY; j++)
            {
                // u component
                if (s[i * n + j] != 0.0f && s[(i - 1) * n + j] != 0.0f && j < numY - 1)
                {
                    float x = i * h;
                    float y = j * h + h2;
                    float uVal = u[i * n + j];
                    float vVal = AvgV(i, j);
                    x = x - dt * uVal;
                    y = y - dt * vVal;
                    uVal = SampleField(x, y, 0); // U_FIELD
                    newU[i * n + j] = uVal;
                }
                
                // v component
                if (s[i * n + j] != 0.0f && s[i * n + j - 1] != 0.0f && i < numX - 1)
                {
                    float x = i * h + h2;
                    float y = j * h;
                    float uVal = AvgU(i, j);
                    float vVal = v[i * n + j];
                    x = x - dt * uVal;
                    y = y - dt * vVal;
                    vVal = SampleField(x, y, 1); // V_FIELD
                    newV[i * n + j] = vVal;
                }
            }
        }
        
        Array.Copy(newU, u, numCells);
        Array.Copy(newV, v, numCells);
    }

    void AdvectSmoke(float dt)
    {
        Array.Copy(m, newM, numCells);
        
        int n = numY;
        float h2 = 0.5f * h;
        
        for (int i = 1; i < numX - 1; i++)
        {
            for (int j = 1; j < numY - 1; j++)
            {
                if (s[i * n + j] != 0.0f)
                {
                    float uVal = (u[i * n + j] + u[(i + 1) * n + j]) * 0.5f;
                    float vVal = (v[i * n + j] + v[i * n + j + 1]) * 0.5f;
                    float x = i * h + h2 - dt * uVal;
                    float y = j * h + h2 - dt * vVal;
                    
                    newM[i * n + j] = SampleField(x, y, 2); // S_FIELD
                }
            }
        }
        
        Array.Copy(newM, m, numCells);
    }

    // void SetObstacle(float x, float y, bool reset)
    // {
    //     float vx = 0.0f;
    //     float vy = 0.0f;
        
    //     if (!reset)
    //     {
    //         vx = (x - obstaclePos.x) / dt;
    //         vy = (y - obstaclePos.y) / dt;
    //     }
        
    //     obstaclePos = new Vector2(x, y);
    //     float r = obstacleRadius;
    //     int n = numY;
        
    //     for (int i = 1; i < numX - 2; i++)
    //     {
    //         for (int j = 1; j < numY - 2; j++)
    //         {
    //             s[i * n + j] = 1.0f;
                
    //             float dx = (i + 0.5f) * h - x;
    //             float dy = (j + 0.5f) * h - y;
                
    //             if (dx * dx + dy * dy < r * r)
    //             {
    //                 s[i * n + j] = 0.0f;
    //                 if (sceneType == SceneType.Paint)
    //                     m[i * n + j] = 0.5f + 0.5f * Mathf.Sin(0.1f * Time.frameCount);
    //                 else
    //                     m[i * n + j] = 1.0f;
    //                 u[i * n + j] = vx;
    //                 u[(i + 1) * n + j] = vx;
    //                 v[i * n + j] = vy;
    //                 v[i * n + j + 1] = vy;
    //             }
    //         }
    //     }
    // }

    Color GetSciColor(float val, float minVal, float maxVal)
    {
        val = Mathf.Clamp(val, minVal, maxVal - 0.0001f);
        float d = maxVal - minVal;
        val = d == 0.0f ? 0.5f : (val - minVal) / d;
        float m = 0.25f;
        int num = Mathf.FloorToInt(val / m);
        float s = (val - num * m) / m;
        float r, g, b;
        
        switch (num)
        {
            case 0: r = 0.0f; g = s; b = 1.0f; break;
            case 1: r = 0.0f; g = 1.0f; b = 1.0f - s; break;
            case 2: r = s; g = 1.0f; b = 0.0f; break;
            case 3: r = 1.0f; g = 1.0f - s; b = 0.0f; break;
            default: r = 1.0f; g = 0.0f; b = 0.0f; break;
        }
        
        return new Color(r, g, b, 1.0f);
    }

    void Render()
    {
        int n = numY;
        
        // Find pressure range
        float minP = p[0];
        float maxP = p[0];
        
        for (int i = 0; i < numCells; i++)
        {
            minP = Mathf.Min(minP, p[i]);
            maxP = Mathf.Max(maxP, p[i]);
        }
        
        for (int i = 0; i < numX; i++)
        {
            for (int j = 0; j < numY; j++)
            {
                Color color = Color.white;
                
                if (showPressure)
                {
                    float pressure = p[i * n + j];
                    float smoke = m[i * n + j];
                    color = GetSciColor(pressure, minP, maxP);
                    if (showSmoke)
                    {
                        color.r = Mathf.Max(0.0f, color.r - smoke);
                        color.g = Mathf.Max(0.0f, color.g - smoke);
                        color.b = Mathf.Max(0.0f, color.b - smoke);
                    }
                }
                else if (showSmoke)
                {
                    float smoke = m[i * n + j];
                    color = new Color(smoke, smoke, smoke, 1.0f);
                    if (sceneType == SceneType.Paint)
                        color = GetSciColor(smoke, 0.0f, 1.0f);
                }
                // else if (showVelocities)
                // {
                //     float maxVel = VelocityMagnitude(i, j);
                //     color = GetSciColor(maxVel, 0.0f, 3.0f); // tune max range
                //     color.r = Mathf.Max(0.0f, 0.5f + u[i * n + j] / maxVel);
                //     color.g = Mathf.Max(0.0f, 0.5f + v[i * n + j] / maxVel);
                //     color.b = Mathf.Max(0.0f, maxVel);
                // }
                else if (s[i * n + j] == 0.0f)
                {
                    color = Color.black;
                }
                
                pixels[j * numX + i] = color;
            }
        }
        
        fluidTexture.SetPixels(pixels);
        fluidTexture.Apply();
    }

    // float VelocityMagnitude(int i, int j)
    // {
    //     int n = numY;

    //     float uVal = 0.5f * (u[i * n + j]);// + u[(i + 1) * n + j]);
    //     float vVal = 0.5f * (v[i * n + j]);// + v[i * n + j + 1]);

    //     return Mathf.Sqrt(uVal * uVal + vVal * vVal);
    // }

    void DrawVelocitiesGL()
    {
        int n = numY;
        float scale = 0.02f;   // same as JS
        int step = 2;          // skip cells to reduce clutter

        GL.Begin(GL.LINES);
        GL.Color(Color.black);

        for (int i = 1; i < numX - 1; i += step)
        {
            for (int j = 1; j < numY - 1; j += step)
            {
                if (s[i * n + j] == 0.0f)
                    continue;

                float uVal = u[i * n + j];
                float vVal = v[i * n + j];

                Vector3 pU = SimToWorld(i * h, (j + 0.5f) * h);
                Vector3 pU2 = pU + new Vector3(uVal * scale, 0, 0);

                Vector3 pV = SimToWorld((i + 0.5f) * h, j * h);
                Vector3 pV2 = pV + new Vector3(0, vVal * scale, 0);

                GL.Vertex(pU);
                GL.Vertex(pU2);

                GL.Vertex(pV);
                GL.Vertex(pV2);
            }
        }

        GL.End();
    }

    void OnRenderObject()
    {
        if (!showVelocities && !showStreamlines)
            return;

        if (debugLineMaterial == null || quadTransform == null)
            return;

        debugLineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(quadTransform.localToWorldMatrix);

        if (showVelocities)
            DrawVelocitiesGL();

        if (showStreamlines)
            DrawStreamlinesGL();

        GL.PopMatrix();
    }

    void DrawStreamlinesGL()
    {
        int seedsStep = 5;
        int numSegs = 15;
        float stepSize = 0.01f;

        GL.Begin(GL.LINES);
        GL.Color(Color.black);

        for (int i = 1; i < numX - 1; i += seedsStep)
        {
            for (int j = 1; j < numY - 1; j += seedsStep)
            {
                float x = (i + 0.5f) * h;
                float y = (j + 0.5f) * h;

                Vector3 prev = SimToWorld(x, y);

                for (int k = 0; k < numSegs; k++)
                {
                    float uVal = SampleField(x, y, 0); // U_FIELD
                    float vVal = SampleField(x, y, 1); // V_FIELD

                    x += uVal * stepSize;
                    y += vVal * stepSize;

                    if (x < 0 || y < 0 || x > simWidth || y > simHeight)
                        break;

                    Vector3 curr = SimToWorld(x, y);

                    GL.Vertex(prev);
                    GL.Vertex(curr);

                    prev = curr;
                }
            }
        }

        GL.End();
    }

    Vector3 SimToWorld(float x, float y)
    {
        return new Vector3(
            (x / simWidth - 0.5f),
            (y / simHeight - 0.5f),
            0
        );
    }

    /////////////////////////////////////// Compute lift and circulation proxies for comparison /////////////////////

    public void ComputeLiftProxy()
    {
        int n = numY;
        liftProxy = 0f;

        for (int i = 1; i < numX - 1; i++)
        {
            for (int j = 1; j < numY - 1; j++)
            {
                if (s[i * n + j] != 0.0f)
                    continue; // inside solid only

                // Check neighboring fluid cells (surface detection)
                AccumulatePressureForce(i, j, -1,  0); // left
                AccumulatePressureForce(i, j,  1,  0); // right
                AccumulatePressureForce(i, j,  0, -1); // bottom
                AccumulatePressureForce(i, j,  0,  1); // top
            }
        }
    }

    void AccumulatePressureForce(int i, int j, int di, int dj)
    {
        int n = numY;
        int ni = i + di;
        int nj = j + dj;

        if (s[ni * n + nj] == 0.0f)
            return; // solid-solid

        float pressure = p[ni * n + nj];

        Vector2 normal = new Vector2(di, dj); // grid normal
        normal.Normalize();

        // Lift = vertical component of pressure force
        liftProxy += -pressure * normal.y * h;
    }

    public void ComputeCirculation()
    {
        int n = numY;
        circulation = 0f;

        // Define loop bounds (must enclose entire airfoil)
        int margin = 2;
        int iMin = margin;
        int iMax = numX - margin - 1;
        int jMin = margin;
        int jMax = numY - margin - 1;

        // Bottom edge
        for (int i = iMin; i < iMax; i++)
            circulation += u[i * n + jMin] * h;

        // Right edge
        for (int j = jMin; j < jMax; j++)
            circulation += v[iMax * n + j] * h;

        // Top edge
        for (int i = iMax; i > iMin; i--)
            circulation -= u[i * n + jMax] * h;

        // Left edge
        for (int j = jMax; j > jMin; j--)
            circulation -= v[iMin * n + j] * h;
    }

    ////////////////////////////// Capture screenshot ///////////////////////////////////////////////////////////
    public void CaptureComparisonSnapshot()
    {
        StartCoroutine(CaptureComparisonCoroutine());
    }


    IEnumerator CaptureComparisonCoroutine()
    {
        // Save current visualization state
        bool prevPressure = showPressure;
        bool prevStreamlines = showStreamlines;
        bool prevSmoke = showSmoke;

        // ---------- Capture pressure ----------
        showPressure = true;
        showSmoke = false;
        showStreamlines = false;

        yield return new WaitForEndOfFrame();
        Texture2D pressureTex = CaptureGameView();

        // ---------- Capture streamlines ----------
        showPressure = false;
        showSmoke = true;        // smoke helps context, optional
        showStreamlines = true;

        yield return new WaitForEndOfFrame();
        Texture2D streamlineTex = CaptureGameView();

        // Restore previous state
        showPressure = prevPressure;
        showStreamlines = prevStreamlines;
        showSmoke = prevSmoke;

        CreateComparisonCanvas(pressureTex, streamlineTex);
    }


    Texture2D CaptureGameView()
    {
        int w = Screen.width;
        int h = Screen.height;

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        return tex;
    }


    void CreateComparisonCanvas(Texture2D pressureTex, Texture2D streamlineTex)
    {
        GameObject canvasGO = new GameObject("FluidComparisonCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        CreateImage(canvasGO.transform, pressureTex, new Vector2(0.25f, 0.6f));
        CreateImage(canvasGO.transform, streamlineTex, new Vector2(0.75f, 0.6f));

        CreateText(canvasGO.transform,
            $"Avg Lift Proxy: {avgLiftProxy:F3}\nAvg Circulation: {avgCirculation:F3}",
            new Vector2(0.5f, 0.2f));
    }


    void CreateImage(Transform parent, Texture2D tex, Vector2 anchor)
    {
        GameObject imgGO = new GameObject("Image");
        imgGO.transform.SetParent(parent);

        Image img = imgGO.AddComponent<Image>();
        img.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));

        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(400, 300);
    }


    void CreateText(Transform parent, string content, Vector2 anchor)
    {
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(parent);

        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = content;
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;

        RectTransform rt = text.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = new Vector2(600, 100);
    }


}
