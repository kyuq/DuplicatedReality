using Kinect4Azure;
using UnityEngine;

public class KinectController_WithDuplicatedReality : KinectController
{

    [Header("Duplicated Reality")]
    public bool EnableDuplication = true;
    public Transform ROI;
    public Transform Duplicate;

    public override void OnSetPointcloudProperties(Material pointcloudMat)
    {
        if(EnableDuplication) pointcloudMat.EnableKeyword("_DUPLICATE_ON");
        else pointcloudMat.DisableKeyword("_DUPLICATE_ON");

        pointcloudMat.SetFloat("_ROIScale", ROI.lossyScale.x);
        pointcloudMat.SetFloat("_DuplScale", Duplicate.lossyScale.x);
        pointcloudMat.SetVector("_CenterOfROI", ROI.position);
        pointcloudMat.SetMatrix("_ROI2Dupl", Matrix4x4.TRS(Duplicate.position - ROI.position, Quaternion.Inverse(ROI.rotation) * Duplicate.rotation, Vector3.one));
        pointcloudMat.SetMatrix("_DuplInverseTransform", Duplicate.worldToLocalMatrix);
    }
}
