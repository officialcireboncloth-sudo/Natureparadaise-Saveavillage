using UnityEngine;

/// <summary>Displays the forge's authored 36-frame sheet on its single flame card.</summary>
[RequireComponent(typeof(Renderer))]
public sealed class ShopForgeFlame : MonoBehaviour
{
    static readonly int BaseMapTransform = Shader.PropertyToID("_BaseMap_ST");
    const int Columns = 6;
    const int Rows = 6;
    const float FrameWidth = 288f;
    const float FrameHeight = 240f;
    const float Padding = 2f;
    const float FramesPerSecond = 1f / .06f;

    Renderer card;
    MaterialPropertyBlock properties;
    int lastFrame = -1;

    void Awake()
    {
        card = GetComponent<Renderer>();
        properties = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        int frame = Mathf.FloorToInt(Time.time * FramesPerSecond) % (Columns * Rows);
        if (frame == lastFrame) return;
        lastFrame = frame;
        float strideX = FrameWidth + 2f * Padding;
        float strideY = FrameHeight + 2f * Padding;
        float width = strideX * Columns;
        float height = strideY * Rows;
        int rowFromBottom = Rows - 1 - frame / Columns;
        card.GetPropertyBlock(properties);
        properties.SetVector(BaseMapTransform, new Vector4(
            FrameWidth / width, FrameHeight / height,
            ((frame % Columns) * strideX + Padding) / width,
            (rowFromBottom * strideY + Padding) / height));
        card.SetPropertyBlock(properties);
    }
}
