using UnityEngine;
using TMPro;

public class LightAngleAnimator : MonoBehaviour
{
    [SerializeField] private float speed = 1f;        // Speed of the light angle change
    [SerializeField] private float angleRange = Mathf.PI * 2f; // Range of the angle change (0 to 2π)

    [SerializeField] private TMP_Text textMeshPro;
    private Material textMaterial;
    private float initialLightAngle;

    void Start()
    {
        // Get the material used by the TextMeshPro component
        // We clone the material to avoid changing the shared material
        textMaterial = textMeshPro.fontMaterial;

        // Store the initial light angle
        if (textMaterial.HasProperty("_LightAngle"))
        {
            initialLightAngle = textMaterial.GetFloat("_LightAngle");
        }
        else
        {
            Debug.LogWarning("Material does not have a _LightAngle property.");
        }
    }

    void Update()
    {
        if (textMaterial != null && textMaterial.HasProperty("_LightAngle"))
        {
            // Calculate the new light angle value
            float lightAngle = initialLightAngle + Mathf.Sin(Time.time * speed) * angleRange * 0.5f;

            // Keep the angle within 0 to 2π
            lightAngle = Mathf.Repeat(lightAngle, Mathf.PI * 2f);

            // Set the "_LightAngle" property in the shader
            textMaterial.SetFloat("_LightAngle", lightAngle);
        }
    }
}
