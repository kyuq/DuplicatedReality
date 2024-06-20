using UnityEngine;

namespace DuplicatedReality
{
    [RequireComponent(typeof(Renderer))]
    public class Duplicable : MonoBehaviour
    {
        public bool EnableOriginal = true;
        public bool EnableDuplication = true;
        public Transform RegionOfInterest;
        public Transform DuplicatedReality;

        private Renderer _Renderer;

        private Shader OriginalOnly;
        private Shader DuplicateOnly;
        private Shader OriginalAndDuplicate;

        private Shader CurrentShader => EnableOriginal && EnableDuplication ? OriginalAndDuplicate :
                                        EnableOriginal ? OriginalOnly :
                                        DuplicateOnly ? DuplicateOnly :
                                        null;

        private void Awake()
        {
            OriginalOnly = Shader.Find("DR_Shader/BlinnPhong_Original");
            DuplicateOnly = Shader.Find("DR_Shader/BlinnPhong_Duplicated");
            OriginalAndDuplicate = Shader.Find("DR_Shader/BlinnPhong_OriginalAndDuplicated");

            _Renderer = GetComponent<Renderer>();
            _Renderer.sharedMaterials = _Renderer.materials;
        }

        public void Update()
        {
            UpdateMaterials();
        }

        private void UpdateMaterials()
        {
            _Renderer.enabled = EnableOriginal || EnableDuplication;

            if (!_Renderer.enabled) return;

            foreach (var m in _Renderer.sharedMaterials)
            {
                m.shader = CurrentShader;

                m.SetMatrix("_Roi2Dupl", DuplicatedReality.localToWorldMatrix * RegionOfInterest.worldToLocalMatrix);
                m.SetMatrix("_ROI_Inversed", RegionOfInterest.worldToLocalMatrix);
                m.SetMatrix("_Dupl_Inversed", DuplicatedReality.worldToLocalMatrix);
            }

            // Prevent Frustum Culling
            Bounds adjustedBounds = _Renderer.bounds;
            adjustedBounds.center = Camera.main.transform.position + (Camera.main.transform.forward * (Camera.main.farClipPlane - Camera.main.nearClipPlane) * 0.5f);
            adjustedBounds.extents = new Vector3(0.1f, 0.1f, 0.1f);

            _Renderer.bounds = adjustedBounds;
        }
    }

}
