using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(LineRenderer))]
public class SplineVisualizer : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    private LineRenderer lineRenderer;
    [SerializeField] private int sampleCount = 50;
    [SerializeField] private bool convertToWorldSpace = true;
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) Debug.LogError("LineRenderer missing");
    }

    void Update()
    {
        if (splineContainer == null) return;
        var spline = splineContainer.Spline;
        if (spline == null) return;

        sampleCount = Mathf.Clamp(sampleCount, 2, 20000);

        lineRenderer.positionCount = sampleCount;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (sampleCount - 1f);

            Vector3 localPos = spline.EvaluatePosition(t);

            Vector3 pos = convertToWorldSpace
                ? splineContainer.transform.TransformPoint(localPos)
                : localPos;

            lineRenderer.SetPosition(i, pos);
        }

        lineRenderer.loop = spline.Closed;
    }
}
