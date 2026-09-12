using System;
using UnityEngine;

/// <summary>
/// Multi-layer vertical parallax background system with zero-gap leapfrog wrapping.
/// Employs differential layer speeds to create deep atmospheric perspective in 2D orthographic space.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    public const float DefaultFarSpeed = 0.8f;
    public const float DefaultMidSpeed = 1.4f;
    public const float DefaultNearSpeed = 2.6f;
    public const float DefaultHeight = 10.24f;

    [Serializable]
    public class LayerConfig
    {
        public string name = "Layer";
        public Sprite sprite;
        public float scrollSpeed = 1.0f;
        public int sortingOrder = -10;
        public Color tintColor = Color.white;
        public Vector2 scale = new Vector2(1.5f, 1.0f);
        public float height = DefaultHeight;

        [HideInInspector] public Transform transformA;
        [HideInInspector] public Transform transformB;
    }

    [Header("Parallax Configuration")]
    [SerializeField] private LayerConfig[] layers;
    [SerializeField] private bool autoInitialize = true;

    public LayerConfig[] Layers => layers;

    private void Awake()
    {
        if (layers == null || layers.Length == 0)
        {
            SetupDefaultLayers();
        }

        if (autoInitialize)
        {
            InitializeLayers();
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        AdjustLayerScales();
        UpdateLayers(dt);
    }

    /// <summary>
    /// Dynamically expands layer width to eliminate black side borders across any resolution or aspect ratio.
    /// </summary>
    public void AdjustLayerScales()
    {
        if (layers == null) return;

        Camera cam = Camera.main;
        float screenHalfWidth = 14f;
        if (cam != null && cam.orthographic)
        {
            screenHalfWidth = cam.orthographicSize * cam.aspect;
        }

        float desiredWidth = Mathf.Max(screenHalfWidth * 2f + 6f, 36f);

        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            float spriteWidth = (layer.sprite != null && layer.sprite.pixelsPerUnit > 0f)
                ? (layer.sprite.rect.width / layer.sprite.pixelsPerUnit)
                : 5.12f;

            float targetScaleX = Mathf.Max(layer.scale.x, desiredWidth / Mathf.Max(0.1f, spriteWidth));

            if (layer.transformA != null && Mathf.Abs(layer.transformA.localScale.x - targetScaleX) > 0.01f)
            {
                Vector3 s = layer.transformA.localScale;
                s.x = targetScaleX;
                layer.transformA.localScale = s;
            }
            if (layer.transformB != null && Mathf.Abs(layer.transformB.localScale.x - targetScaleX) > 0.01f)
            {
                Vector3 s = layer.transformB.localScale;
                s.x = targetScaleX;
                layer.transformB.localScale = s;
            }
        }
    }

    /// <summary>
    /// Updates positions for all configured parallax layers by delta time.
    /// </summary>
    public void UpdateLayers(float dt)
    {
        if (layers == null || dt <= 0f) return;

        for (int i = 0; i < layers.Length; i++)
        {
            UpdateLayer(layers[i], dt);
        }
    }

    /// <summary>
    /// Executes leapfrog wrapping translation for an individual layer with strict gap invariant.
    /// </summary>
    public void UpdateLayer(LayerConfig layer, float dt)
    {
        if (layer == null || dt <= 0f || layer.scrollSpeed == 0f) return;
        if (layer.transformA == null || layer.transformB == null) return;

        float delta = layer.scrollSpeed * dt;
        Vector3 posA = layer.transformA.position;
        Vector3 posB = layer.transformB.position;

        posA.y -= delta;
        posB.y -= delta;

        // Zero-gap leapfrog wrapping: reposition relative to paired partner
        float h = Mathf.Max(0.1f, layer.height);
        if (posA.y <= -h)
        {
            posA.y = posB.y + h;
        }
        if (posB.y <= -h)
        {
            posB.y = posA.y + h;
        }

        layer.transformA.position = posA;
        layer.transformB.position = posB;
    }

    /// <summary>
    /// Creates the 3 standard architectural layers: Distant Sky, Mid Clouds, Near Mist.
    /// </summary>
    public void SetupDefaultLayers()
    {
        layers = new LayerConfig[]
        {
            new LayerConfig
            {
                name = "DistantSky",
                scrollSpeed = DefaultFarSpeed,
                sortingOrder = -10,
                scale = new Vector2(1.5f, 1.0f),
                tintColor = Color.white,
                height = DefaultHeight
            },
            new LayerConfig
            {
                name = "MidClouds",
                scrollSpeed = DefaultMidSpeed,
                sortingOrder = -5,
                scale = new Vector2(1.5f, 1.0f),
                tintColor = new Color(1f, 1f, 1f, 0.45f),
                height = DefaultHeight
            },
            new LayerConfig
            {
                name = "NearMist",
                scrollSpeed = DefaultNearSpeed,
                sortingOrder = -1,
                scale = new Vector2(1.8f, 1.0f),
                tintColor = new Color(0.85f, 0.95f, 1f, 0.70f),
                height = DefaultHeight
            }
        };
    }

    /// <summary>
    /// Spawns or binds paired child SpriteRenderers for each layer.
    /// </summary>
    public void InitializeLayers()
    {
        if (layers == null) return;

        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.sprite != null && layer.sprite.pixelsPerUnit > 0f)
            {
                layer.height = (layer.sprite.rect.height / layer.sprite.pixelsPerUnit) * Mathf.Abs(layer.scale.y);
            }
            else if (layer.height <= 0f)
            {
                layer.height = DefaultHeight * Mathf.Abs(layer.scale.y);
            }

            if (layer.transformA == null)
            {
                GameObject objA = new GameObject($"{layer.name}_A");
                objA.transform.SetParent(transform);
                objA.transform.localScale = new Vector3(layer.scale.x, layer.scale.y, 1f);
                objA.transform.position = new Vector3(0f, 0f, 0f);
                var srA = objA.AddComponent<SpriteRenderer>();
                srA.sprite = layer.sprite;
                srA.sortingOrder = layer.sortingOrder;
                srA.color = layer.tintColor;
                layer.transformA = objA.transform;
            }

            if (layer.transformB == null)
            {
                GameObject objB = new GameObject($"{layer.name}_B");
                objB.transform.SetParent(transform);
                objB.transform.localScale = new Vector3(layer.scale.x, layer.scale.y, 1f);
                objB.transform.position = new Vector3(0f, layer.height, 0f);
                var srB = objB.AddComponent<SpriteRenderer>();
                srB.sprite = layer.sprite;
                srB.sortingOrder = layer.sortingOrder;
                srB.color = layer.tintColor;
                layer.transformB = objB.transform;
            }
        }

        AdjustLayerScales();
    }
}
