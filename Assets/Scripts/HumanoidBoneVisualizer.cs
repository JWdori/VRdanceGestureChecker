using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class HumanoidBoneVisualizer : MonoBehaviour
{
    [Header("Assign Animator")]
    public Animator animator;

    [Header("Gizmo Settings")]
    [Tooltip("씬 뷰에 표시할 구체 반지름")]
    private float gizmoSphereSize = 0.02f;

    [Header("Bone Rotations (World Quaternions)")]
    [SerializeField] private Quaternion leftShoulderRotation;
    [SerializeField] private Quaternion rightShoulderRotation;
    [SerializeField] private Quaternion leftElbowRotation;
    [SerializeField] private Quaternion rightElbowRotation;
    [SerializeField] private Quaternion leftWristRotation;
    [SerializeField] private Quaternion rightWristRotation;

    private void Update()
    {
        if (animator != null)
            UpdateBoneRotations();
    }

    private void OnValidate()
    {
        if (animator != null)
            UpdateBoneRotations();
    }

    private void UpdateBoneRotations()
    {
        leftShoulderRotation  = GetBoneRotation(HumanBodyBones.LeftUpperArm);
        rightShoulderRotation = GetBoneRotation(HumanBodyBones.RightUpperArm);
        leftElbowRotation     = GetBoneRotation(HumanBodyBones.LeftLowerArm);
        rightElbowRotation    = GetBoneRotation(HumanBodyBones.RightLowerArm);
        leftWristRotation     = GetBoneRotation(HumanBodyBones.LeftHand);
        rightWristRotation    = GetBoneRotation(HumanBodyBones.RightHand);
    }

    private Quaternion GetBoneRotation(HumanBodyBones bone)
    {
        var t = animator.GetBoneTransform(bone);
        return t != null ? t.rotation : Quaternion.identity;
    }

    private void OnDrawGizmos()
    {
        if (animator == null) return;

        DrawBoneVisual(HumanBodyBones.LeftUpperArm, Color.red,   "L-S");
        DrawBoneVisual(HumanBodyBones.RightUpperArm, Color.red,  "R-S");
        DrawBoneVisual(HumanBodyBones.LeftLowerArm, Color.green, "L-E");
        DrawBoneVisual(HumanBodyBones.RightLowerArm, Color.green,"R-E");
        DrawBoneVisual(HumanBodyBones.LeftHand, Color.blue,      "L-W");
        DrawBoneVisual(HumanBodyBones.RightHand, Color.blue,     "R-W");

        Gizmos.color = Color.white;
        DrawLine(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm);
        DrawLine(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand);
        DrawLine(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm);
        DrawLine(HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand);
    }

    private void DrawBoneVisual(HumanBodyBones bone, Color color, string label)
    {
        var t = animator.GetBoneTransform(bone);
        if (t == null) return;

        Gizmos.color = color;
        Gizmos.DrawSphere(t.position, gizmoSphereSize);

        #if UNITY_EDITOR
        Handles.color = color;
        Handles.Label(t.position + Vector3.up * gizmoSphereSize, label);
        #endif
    }

    private void DrawLine(HumanBodyBones aBone, HumanBodyBones bBone)
    {
        var a = animator.GetBoneTransform(aBone);
        var b = animator.GetBoneTransform(bBone);
        if (a != null && b != null)
            Gizmos.DrawLine(a.position, b.position);
    }
}
