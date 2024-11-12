using UnityEngine;

public class Hovering : MonoBehaviour
{
    private float originalY;
    private float amplitude = 10f;
    private float frequency = 1f;
    private float upwardSpeed = 0f;

    public void SetHoverParameters(float amplitude, float frequency, float upwardSpeed)
    {
        this.amplitude = amplitude;
        this.frequency = frequency;
        this.upwardSpeed = upwardSpeed;
    }

    private void Start()
    {
        originalY = transform.position.y;
    }

    private void Update()
    {
        Vector3 position = transform.position;
        position.y = originalY + Mathf.Sin(Time.time * frequency) * amplitude + upwardSpeed * Time.time;
        transform.position = position;
    }
}
