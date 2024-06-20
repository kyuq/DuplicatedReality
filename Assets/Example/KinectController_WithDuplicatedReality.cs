using Kinect4Azure;
using UnityEngine;

public class KinectController_WithDuplicatedReality : KinectController
{

    [Header("Duplicated Reality")]
    public bool EnableDuplication = true;
    public Transform RegionOfInterest;
    public Transform DuplicatedReality;

    public override void OnSetPointcloudProperties(Material pointcloudMat)
    {
        if(EnableDuplication) pointcloudMat.EnableKeyword("_DUPLICATE_ON");
        else pointcloudMat.DisableKeyword("_DUPLICATE_ON");

        pointcloudMat.SetMatrix("_Roi2Dupl", DuplicatedReality.localToWorldMatrix * RegionOfInterest.worldToLocalMatrix);
        pointcloudMat.SetMatrix("_ROI_Inversed", RegionOfInterest.worldToLocalMatrix);
        pointcloudMat.SetMatrix("_Dupl_Inversed", DuplicatedReality.worldToLocalMatrix);
    }
}
