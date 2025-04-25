using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMirror : MonoBehaviour
{
    public Transform objectToFollow;
    public Transform mirror;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 objectToFollowLocal = mirror.InverseTransformPoint(objectToFollow.position);
        transform.position = mirror.TransformPoint(new Vector3(objectToFollowLocal.x, objectToFollowLocal.y, -objectToFollowLocal.z));

        transform.localRotation = objectToFollow.localRotation;
    }
}
