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

        pointcloudMat.SetMatrix("_Roi2Dupl", Duplicate.localToWorldMatrix * ROI.worldToLocalMatrix);
        pointcloudMat.SetMatrix("_ROI_Inversed", ROI.worldToLocalMatrix);
        pointcloudMat.SetMatrix("_Dupl_Inversed", Duplicate.worldToLocalMatrix);
    }
}
