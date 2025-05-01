using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LightAngleAnimator : MonoBehaviour
{
    [SerializeField] float speed = 1f;                 // oscillations per second
    [SerializeField] float angleRange = Mathf.PI * 2;  // max ± range (radians)

    [SerializeField] TMP_Text textMeshPro;

    static readonly int LightAngleID = Shader.PropertyToID("_LightAngle");

    Material matInstance;
    float baseAngle;
    float halfRange;

    void Awake()
    {
        if (!textMeshPro) textMeshPro = GetComponent<TMP_Text>();

        // clone once -> no shared-material mutation
        matInstance = Instantiate(textMeshPro.fontMaterial);
        textMeshPro.fontMaterial = matInstance;

        baseAngle = matInstance.HasProperty(LightAngleID)
                  ? matInstance.GetFloat(LightAngleID)
                  : 0f;

        halfRange = angleRange * .5f;
    }

    void Update()
    {
        float ang = baseAngle + Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * halfRange;
        matInstance.SetFloat(LightAngleID, ang);
    }
}
