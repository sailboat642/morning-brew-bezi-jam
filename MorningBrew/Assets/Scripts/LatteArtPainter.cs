using UnityEngine;

public class LatteArtPainter : MonoBehaviour
{
    [Header("Canvas Setup")]
    public int canvasSize = 256;
    [Range(0.1f, 3f)]
    public float coffeeScale = 2.5f;
    [Range(-1f, 1f)]
    public float coffeeOffsetX = 0f;
    [Range(-1f, 1f)]
    public float coffeeOffsetY = 0.2f;
    [Range(0.1f, 2f)]
    public float colliderRadius = 1.6f;

    [Range(0.1f, 0.9f)]
    public float coffeeCircleSize = 0.4f;
    public Color coffeeColor = new Color(0.4f, 0.2f, 0.1f, 1f);

    [Header("Debug")]
    public bool showDebugLogs = true;

    private RenderTexture coffeeTexture;
    private GameObject coffeeSpriteGameObject; // Fixed naming conflict
    private SpriteRenderer coffeeSpriteRenderer;
    private CircleCollider2D coffeeCollider;

    void Start()
    {
        Debug.Log("=== Setting up Coffee Canvas ===");
        CreateCoffeeSprite();
        SetupCoffeeTexture();
        SetupCollider();

        Invoke("DebugCoordinateSystem", 0.2f);
        Invoke("DebugColliderAndBounds", 0.2f);
    }

    void CreateCoffeeSprite()
    {
        // Create a separate GameObject for the coffee surface
        coffeeSpriteGameObject = new GameObject("CoffeeSurface");
        coffeeSpriteGameObject.transform.SetParent(transform);

        // Position inside the cup
        coffeeSpriteGameObject.transform.localPosition = new Vector3(coffeeOffsetX, coffeeOffsetY, 0f);
        coffeeSpriteGameObject.transform.localScale = Vector3.one * coffeeScale;

        // Add SpriteRenderer
        coffeeSpriteRenderer = coffeeSpriteGameObject.AddComponent<SpriteRenderer>();

        // CRITICAL: Set proper sorting to ensure it shows in Game view
        coffeeSpriteRenderer.sortingLayerName = "Default";
        coffeeSpriteRenderer.sortingOrder = 1; // In front of cup (cup should be 0)

        // Create a simple circle texture for the coffee surface
        Texture2D circleTexture = CreateCircleTexture(canvasSize);

        // Create sprite from the circle texture
        Sprite coffeeSprite = Sprite.Create(
            circleTexture,
            new Rect(0, 0, canvasSize, canvasSize),
            new Vector2(0.5f, 0.5f),
            100f
        );

        coffeeSpriteRenderer.sprite = coffeeSprite;

        Debug.Log($"Coffee sprite created at: {coffeeSpriteGameObject.transform.localPosition}");
        Debug.Log($"Coffee sprite sorting order: {coffeeSpriteRenderer.sortingOrder}");
    }

    Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                if (distance <= radius)
                {
                    pixels[y * size + x] = coffeeColor;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    void SetupCoffeeTexture()
    {
        // Create render texture for painting
        coffeeTexture = new RenderTexture(canvasSize, canvasSize, 0, RenderTextureFormat.ARGB32);
        coffeeTexture.Create();

        // Start with a circular coffee base
        RenderTexture.active = coffeeTexture;
        GL.Clear(true, true, Color.clear);
        DrawCircleToRenderTexture(coffeeTexture, coffeeColor);
        RenderTexture.active = null;

        // Create sprite with proper scaling
        Sprite coffeeSprite = Sprite.Create(
            Texture2D.CreateExternalTexture(canvasSize, canvasSize, TextureFormat.ARGB32, false, false, coffeeTexture.GetNativeTexturePtr()),
            new Rect(0, 0, canvasSize, canvasSize),
            new Vector2(0.5f, 0.5f),
            100f // Use consistent pixelsPerUnit
        );

        coffeeSpriteRenderer.sprite = coffeeSprite;

        // Create material
        Material coffeeMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        coffeeMaterial.mainTexture = coffeeTexture;
        coffeeSpriteRenderer.material = coffeeMaterial;

        // Debug sprite size
        Debug.Log($"Coffee sprite bounds: {coffeeSpriteRenderer.bounds}");
        Debug.Log($"Coffee sprite size: {coffeeSpriteRenderer.bounds.size}");
    }



    void DrawCircleToRenderTexture(RenderTexture target, Color color)
    {
        RenderTexture.active = target;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, canvasSize, canvasSize, 0);

        Material mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.SetPass(0);

        GL.Begin(GL.TRIANGLES);
        GL.Color(color);

        // Draw circle using triangles - SMALLER CIRCLE
        Vector2 center = new Vector2(canvasSize * 0.5f, canvasSize * 0.5f);
        float radius = canvasSize * coffeeCircleSize; // Use adjustable size instead of fixed 0.45f

        int segments = 64; // Smooth circle
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2f;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;

            Vector2 point1 = center + new Vector2(Mathf.Cos(angle1), Mathf.Sin(angle1)) * radius;
            Vector2 point2 = center + new Vector2(Mathf.Cos(angle2), Mathf.Sin(angle2)) * radius;

            // Triangle: center, point1, point2
            GL.Vertex3(center.x, center.y, 0);
            GL.Vertex3(point1.x, point1.y, 0);
            GL.Vertex3(point2.x, point2.y, 0);
        }

        GL.End();
        GL.PopMatrix();

        RenderTexture.active = null;
    }




    void SetupCollider()
    {
        // Add collider to the coffee sprite
        coffeeCollider = coffeeSpriteGameObject.AddComponent<CircleCollider2D>();
        coffeeCollider.isTrigger = true;

        // Make collider match the visual coffee surface (smaller than the full sprite)
        coffeeCollider.radius = colliderRadius; // Reduced from 0.5f to better match coffee surface

        Debug.Log($"Coffee collider setup - actual radius: {coffeeCollider.radius * coffeeScale}");
    }


    void Update()
    {
        HandleInput();
        UpdateCoffeePosition();
    }

    void UpdateCoffeePosition()
    {
        if (coffeeSpriteGameObject != null)
        {
            coffeeSpriteGameObject.transform.localPosition = new Vector3(coffeeOffsetX, coffeeOffsetY, 0f);
            coffeeSpriteGameObject.transform.localScale = Vector3.one * coffeeScale;
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;

            // FIXED: Use camera distance properly for 2D
            Vector3 worldPos3D = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
            Vector2 worldPos = new Vector2(worldPos3D.x, worldPos3D.y);

            if (showDebugLogs)
            {
                Debug.Log($"Mouse screen pos: {mousePos}");
                Debug.Log($"Mouse world pos: {worldPos}");
                Debug.Log($"Camera position: {Camera.main.transform.position}");
            }

            bool hitCoffee = IsPointOnCoffee(worldPos);

            if (showDebugLogs)
                Debug.Log($"Hit coffee: {hitCoffee}");

            if (hitCoffee)
            {
                Vector2 textureCoords = GetTextureCoordinates(worldPos);

                if (showDebugLogs)
                {
                    Debug.Log($"=== HIT COFFEE ===");
                    Debug.Log($"Texture Coords: {textureCoords}");
                    Debug.Log($"Expected center coords: ({canvasSize / 2}, {canvasSize / 2})");
                }

                TestPaintDot(textureCoords);
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log("Click missed coffee surface");
            }
        }
    }



    bool IsPointOnCoffee(Vector2 worldPoint)
    {
        if (coffeeCollider == null) 
        {
            Debug.LogError("Coffee collider is null!");
            return false;
        }
        
        // Check bounds
        bool inBounds = coffeeCollider.bounds.Contains(worldPoint);
        
        // Also check distance from center for circular collision
        Vector2 center = coffeeCollider.bounds.center;
        float distance = Vector2.Distance(worldPoint, center);
        float effectiveRadius = coffeeCollider.radius * coffeeSpriteGameObject.transform.localScale.x;
        bool inCircle = distance <= effectiveRadius;
        
        if (showDebugLogs)
        {
            Debug.Log($"Collider bounds: {coffeeCollider.bounds}");
            Debug.Log($"Point: {worldPoint}, Center: {center}");
            Debug.Log($"Distance: {distance:F3}, Effective radius: {effectiveRadius:F3}");
            Debug.Log($"In bounds: {inBounds}, In circle: {inCircle}");
        }
        
        // Use the stricter check
        return inBounds && inCircle;
    }


    Vector2 GetTextureCoordinates(Vector2 worldPos)
    {
        // Get the coffee sprite's world bounds
        Bounds bounds = coffeeSpriteRenderer.bounds;
        
        if (showDebugLogs)
        {
            Debug.Log($"Sprite bounds: min={bounds.min}, max={bounds.max}, size={bounds.size}");
            Debug.Log($"World pos: {worldPos}");
        }
        
        // Calculate position relative to sprite bounds (0 = left/bottom, 1 = right/top)
        float relativeX = (worldPos.x - bounds.min.x) / bounds.size.x;
        float relativeY = (worldPos.y - bounds.min.y) / bounds.size.y;
        
        if (showDebugLogs)
        {
            Debug.Log($"Relative position: ({relativeX:F3}, {relativeY:F3})");
        }
        
        // Clamp to valid range (0-1)
        relativeX = Mathf.Clamp01(relativeX);
        relativeY = Mathf.Clamp01(relativeY);
        
        // For Unity textures, Y=0 is bottom, but for rendering Y=0 is top
        // So we need to flip Y
        relativeY = 1.0f - relativeY;
        
        // Convert to texture pixel coordinates
        Vector2 result = new Vector2(
            relativeX * (canvasSize - 1), // -1 to stay within bounds
            relativeY * (canvasSize - 1)
        );
        
        if (showDebugLogs)
        {
            Debug.Log($"Final texture coords: {result}");
            Debug.Log($"Should be center if clicked center: ({(canvasSize-1)/2f}, {(canvasSize-1)/2f})");
        }
        
        return result;
    }




    void TestPaintDot(Vector2 texturePos)
    {
        // Check if the paint position is within the coffee circle
        Vector2 center = new Vector2(canvasSize * 0.5f, canvasSize * 0.5f);
        float coffeeRadius = canvasSize * coffeeCircleSize;

        if (Vector2.Distance(texturePos, center) > coffeeRadius)
        {
            if (showDebugLogs)
                Debug.Log($"Paint position {texturePos} is outside coffee bounds");
            return; // Don't paint outside the coffee area
        }

        RenderTexture.active = coffeeTexture;

        GL.PushMatrix();
        GL.LoadPixelMatrix(0, canvasSize, canvasSize, 0);

        Material mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.SetPass(0);

        GL.Begin(GL.QUADS);
        GL.Color(Color.white);

        float size = 10f;
        GL.Vertex3(texturePos.x - size, texturePos.y - size, 0);
        GL.Vertex3(texturePos.x + size, texturePos.y - size, 0);
        GL.Vertex3(texturePos.x + size, texturePos.y + size, 0);
        GL.Vertex3(texturePos.x - size, texturePos.y + size, 0);

        GL.End();
        GL.PopMatrix();

        RenderTexture.active = null;

        if (showDebugLogs)
            Debug.Log($"Milk dot painted at: {texturePos}");
    }

    void OnDrawGizmos()
    {
        // Draw coffee bounds in Scene view
        if (coffeeSpriteGameObject != null && coffeeCollider != null)
        {
            // Green wireframe for collider bounds
            Gizmos.color = Color.green;
            Gizmos.matrix = coffeeSpriteGameObject.transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, coffeeCollider.radius);

            // Yellow wireframe for sprite bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            // Red dot at sprite center
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            if (coffeeSpriteRenderer != null)
            {
                Gizmos.DrawSphere(coffeeSpriteRenderer.bounds.center, 0.05f);
            }
        }
    }


    void OnDestroy()
    {
        // Clean up to prevent inspector errors
        if (coffeeTexture != null)
            coffeeTexture.Release();

        // Clean up dynamically created objects
        if (coffeeSpriteGameObject != null)
        {
            if (Application.isPlaying)
                Destroy(coffeeSpriteGameObject);
            else
                DestroyImmediate(coffeeSpriteGameObject);
        }
    }

    void DebugCoordinateSystem()
    {
        if (coffeeSpriteRenderer == null) return;

        Bounds bounds = coffeeSpriteRenderer.bounds;
        Debug.Log($"=== COORDINATE DEBUG ===");
        Debug.Log($"Sprite bounds: {bounds}");
        Debug.Log($"Sprite size: {bounds.size}");
        Debug.Log($"Sprite center: {bounds.center}");
        Debug.Log($"Canvas size: {canvasSize}");
        Debug.Log($"Expected texture center: ({canvasSize / 2}, {canvasSize / 2})");

        // Test center mapping
        Vector2 centerCoords = GetTextureCoordinates(bounds.center);
        Debug.Log($"Center world pos {bounds.center} maps to texture: {centerCoords}");

        // Test corners
        Vector2 bottomLeft = GetTextureCoordinates(new Vector2(bounds.min.x, bounds.min.y));
        Vector2 topRight = GetTextureCoordinates(new Vector2(bounds.max.x, bounds.max.y));
        Debug.Log($"Bottom-left maps to: {bottomLeft} (should be ~0,255)");
        Debug.Log($"Top-right maps to: {topRight} (should be ~255,0)");
    }
    
    void DebugColliderAndBounds()
    {
        if (coffeeSpriteRenderer == null || coffeeCollider == null) return;
        
        Debug.Log($"=== COLLIDER DEBUG ===");
        Debug.Log($"Coffee sprite bounds: {coffeeSpriteRenderer.bounds}");
        Debug.Log($"Coffee collider bounds: {coffeeCollider.bounds}");
        Debug.Log($"Collider radius: {coffeeCollider.radius}");
        Debug.Log($"Sprite transform scale: {coffeeSpriteGameObject.transform.localScale}");
        Debug.Log($"Effective collider radius: {coffeeCollider.radius * coffeeSpriteGameObject.transform.localScale.x}");
    }



}
