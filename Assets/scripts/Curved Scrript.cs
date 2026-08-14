using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class TMPTextBend3D : MonoBehaviour
{
    public enum Axis { Horizontal, Vertical }

    [Header("Curve Settings")]
    [Tooltip("Radius of the cylinder the text wraps around. Smaller = more curve.")]
    public float radius = 5f;

    [Tooltip("Which direction the text wraps along")]
    public Axis wrapAxis = Axis.Horizontal;

    [Tooltip("Flip curve direction (convex vs concave)")]
    public bool invert = false;

    [Tooltip("Prevents characters from wrapping past this angle and flipping backward. 89 = safe max.")]
    [Range(1f, 89f)]
    public float maxAnglePerSide = 80f;

    [Header("Orientation Adjustments")]
    [Tooltip("Extra rotation applied to every character AFTER the wrap. Use this to fix facing/mirroring without touching code.")]
    public Vector3 characterRotationOffset = Vector3.zero;

    [Tooltip("Extra rotation applied to the whole text object (equivalent to rotating the Transform, but keeps curve math centered).")]
    public Vector3 globalRotationOffset = Vector3.zero;

    [Tooltip("Mirror characters horizontally (fixes backward-facing text)")]
    public bool flipHorizontal = false;

    [Tooltip("Mirror characters vertically (fixes upside-down text)")]
    public bool flipVertical = false;

    [Header("Behavior")]
    public bool updateEveryFrame = false;

    private TMP_Text textComponent;
    private string lastText;

    void Awake() => textComponent = GetComponent<TMP_Text>();
    void OnEnable() { textComponent = GetComponent<TMP_Text>(); BendText(); }

    void LateUpdate()
    {
        if (updateEveryFrame || HasChanged())
            BendText();
    }

    // Track state to avoid rebending every frame unnecessarily
    private float _r, _max; private Axis _axis; private bool _inv, _fh, _fv;
    private Vector3 _cro, _gro;
    bool HasChanged()
    {
        bool changed = textComponent.text != lastText || radius != _r || wrapAxis != _axis ||
            invert != _inv || maxAnglePerSide != _max || flipHorizontal != _fh || flipVertical != _fv ||
            characterRotationOffset != _cro || globalRotationOffset != _gro;
        return changed;
    }
    void CacheState()
    {
        lastText = textComponent.text; _r = radius; _axis = wrapAxis; _inv = invert;
        _max = maxAnglePerSide; _fh = flipHorizontal; _fv = flipVertical;
        _cro = characterRotationOffset; _gro = globalRotationOffset;
    }

    [ContextMenu("Bend Text Now")]
    public void BendText()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();

        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;
        if (textInfo.characterCount == 0) return;

        Bounds bounds = textComponent.bounds;

        float minPos = wrapAxis == Axis.Horizontal ? bounds.min.x : bounds.min.y;
        float maxPos = wrapAxis == Axis.Horizontal ? bounds.max.x : bounds.max.y;
        float center = (minPos + maxPos) / 2f;

        float dir = invert ? -1f : 1f;
        float safeRadius = Mathf.Max(radius, 0.01f);
        float maxAngleRad = maxAnglePerSide * Mathf.Deg2Rad;

        Quaternion globalOffsetRot = Quaternion.Euler(globalRotationOffset);
        Quaternion charOffsetRot = Quaternion.Euler(characterRotationOffset);
        Vector3 mirrorScale = new Vector3(flipHorizontal ? -1f : 1f, flipVertical ? -1f : 1f, 1f);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 charMid = Vector3.zero;
            for (int k = 0; k < 4; k++) charMid += vertices[vertexIndex + k];
            charMid /= 4f;

            float posOnAxis = wrapAxis == Axis.Horizontal ? charMid.x : charMid.y;

            // Angle clamped so characters never wrap past maxAnglePerSide (prevents flip-through/mirroring)
            float angle = Mathf.Clamp((posOnAxis - center) / safeRadius, -maxAngleRad, maxAngleRad) * dir;

            Quaternion wrapRot = wrapAxis == Axis.Horizontal
                ? Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f)
                : Quaternion.Euler(-angle * Mathf.Rad2Deg, 0f, 0f);

            // Combine: wrap rotation -> user orientation offset -> global offset
            Quaternion finalRot = globalOffsetRot * charOffsetRot * wrapRot;

            Vector3 pivotOnCylinder;
            if (wrapAxis == Axis.Horizontal)
            {
                pivotOnCylinder = new Vector3(
                    center + Mathf.Sin(angle) * safeRadius,
                    charMid.y,
                    (Mathf.Cos(angle) - 1f) * safeRadius * -dir);
            }
            else
            {
                pivotOnCylinder = new Vector3(
                    charMid.x,
                    center + Mathf.Sin(angle) * safeRadius,
                    (Mathf.Cos(angle) - 1f) * safeRadius * -dir);
            }

            for (int j = 0; j < 4; j++)
            {
                Vector3 orig = vertices[vertexIndex + j];
                Vector3 local = orig - charMid;

                // Apply mirror fix first, then rotation
                local = Vector3.Scale(local, mirrorScale);
                Vector3 rotatedLocal = finalRot * local;

                vertices[vertexIndex + j] = pivotOnCylinder + rotatedLocal;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }

        CacheState();
    }
}