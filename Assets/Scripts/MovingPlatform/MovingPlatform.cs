using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector3 AccumulatedPos { get; private set; }
    public Quaternion AccumulatedRot { get; private set; } = Quaternion.identity;
    public Vector3 Velocity { get; private set; }  // optional, for debug

    Vector3 _prevPos;
    Quaternion _prevRot;

    void Awake()
    {
        _prevPos = transform.position;
        _prevRot = transform.rotation;
        AccumulatedPos = Vector3.zero;
        AccumulatedRot = Quaternion.identity;
    }

    void LateUpdate() // after Spline Animate moves
    {
        var pos = transform.position;
        var rot = transform.rotation;

        var dPos = pos - _prevPos;
        var dRot = rot * Quaternion.Inverse(_prevRot);

        // accumulate for the next physics step
        AccumulatedPos += dPos;
        AccumulatedRot = dRot * AccumulatedRot;

        Velocity = dPos / Mathf.Max(Time.deltaTime, 1e-6f);

        _prevPos = pos;
        _prevRot = rot;
    }

    // called by player in FixedUpdate
    public void ConsumeDeltas(out Vector3 dPos, out Quaternion dRot)
    {
        dPos = AccumulatedPos;
        dRot = AccumulatedRot;
        AccumulatedPos = Vector3.zero;
        AccumulatedRot = Quaternion.identity;
    }
}