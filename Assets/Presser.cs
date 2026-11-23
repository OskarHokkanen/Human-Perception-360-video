using UnityEngine;

// Add to each controller anchor of the XR rig (e.g. LeftControllerAnchor and
// RightControllerAnchor, or LeftHandAnchor and RightHandAnchor).
// Creates a small touch tip in front of the controller that presses buttons.
public class Presser : MonoBehaviour
{
    [Tooltip("Where the tip sits relative to the controller anchor.")]
    [SerializeField] Vector3 tipOffset = new Vector3(0f, 0f, 0.06f);
    [SerializeField] float tipRadius = 0.012f;
    [SerializeField] bool showTip = true;
    [SerializeField] Color tipColor = new Color(1f, 0.85f, 0.2f);

    void Awake()
    {
        var tip = new GameObject("PressTip");
        tip.transform.SetParent(transform, false);
        tip.transform.localPosition = tipOffset;

        var col = tip.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = tipRadius;

        var rb = tip.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (showTip)
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "TipVisual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(tip.transform, false);
            visual.transform.localScale = Vector3.one * tipRadius * 2f;
            var r = visual.GetComponent<Renderer>();
            r.sharedMaterial = StudyMaterials.Create(tipColor);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}