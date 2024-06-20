using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Microsoft.Azure.Kinect.Sensor;

namespace Kinect4Azure
{
    public class KinectController : MonoBehaviour
    {
        public static KinectController Instance;

        [Header("Pointcloud Configs")]
        public bool EnableOriginalPointcloud = true;
        public Shader PointCloudShader;
        [Range(0.01f, 0.1f)]
        public float MaxPointDistance = 0.02f;

        [Header("Background Configs\n(Only works if this script is attached onto the camera)")]
        public bool EnableARBackground = true;
        [Tooltip("Only needs to be set when BlitToCamera is checked")]
        public Material ARBackgroundMaterial;

        [Header("ReadOnly and exposed for Debugging")]
        [SerializeField] private Texture2D DepthImage;
        [SerializeField] private Texture2D ColorImage;
        [SerializeField] private Texture2D ColorInDepthImage;

        private Device _Device;

        public virtual void OnSetPointcloudProperties(Material pointcloudMat) { }

        private void Awake()
        {
            Instance = this;
            StartCoroutine(CameraCapture());
        }

        private void SetupTextures(ref Texture2D Color, ref Texture2D Depth, ref Texture2D ColorInDepth)
        {
            try
            {
                using (var capture = _Device.GetCapture())
                {

                    if (Color == null)
                        Color = new Texture2D(capture.Color.WidthPixels, capture.Color.HeightPixels, TextureFormat.BGRA32, false);
                    if (Depth == null)
                        Depth = new Texture2D(capture.Depth.WidthPixels, capture.Depth.HeightPixels, TextureFormat.R16, false);
                    if (ColorInDepth == null)
                        ColorInDepth = new Texture2D(capture.IR.WidthPixels, capture.IR.HeightPixels, TextureFormat.BGRA32, false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"An error occurred " + ex.Message);
            }

        }

        private Material SetupPointcloudShader(Texture2D ColorInDepth, ref Texture2D Depth)
        {
            GenerateXYTable();

            var PointCloudMat = new Material(PointCloudShader);

            PointCloudMat.SetPass(0);

            PointCloudMat.SetTexture("_ColorTex", ColorInDepth);
            PointCloudMat.SetInt("_ColorWidth", ColorInDepth.width);
            PointCloudMat.SetInt("_ColorHeight", ColorInDepth.height);

            PointCloudMat.SetTexture("_DepthTex", Depth);
            PointCloudMat.SetInt("_DepthWidth", Depth.width);
            PointCloudMat.SetInt("_DepthHeight", Depth.height);

            PointCloudMat.SetTexture("_XYLookup", GenerateXYTable());

            // apply sensor to device offset
            var extrinsics = _Device.GetCalibration().DeviceExtrinsics[(int)CalibrationDeviceType.Depth + (int)CalibrationDeviceType.Color];
            Matrix4x4 extrinsics4x4 = new Matrix4x4();
            extrinsics4x4.SetRow(0, new Vector4(extrinsics.Rotation[0], extrinsics.Rotation[3], extrinsics.Rotation[6], extrinsics.Translation[0] / 1000.0f));
            extrinsics4x4.SetRow(1, new Vector4(extrinsics.Rotation[1], extrinsics.Rotation[4], extrinsics.Rotation[7], extrinsics.Translation[1] / 1000.0f));
            extrinsics4x4.SetRow(2, new Vector4(extrinsics.Rotation[2], extrinsics.Rotation[5], extrinsics.Rotation[8], extrinsics.Translation[2] / 1000.0f));
            extrinsics4x4.SetRow(3, new Vector4(0, 0, 0, 1));

            Matrix4x4 color2DepthCalibration = extrinsics4x4;
            PointCloudMat.SetMatrix("_Col2DepCalibration", color2DepthCalibration);

            return PointCloudMat;
        }


        private IEnumerator CameraCapture()
        {
            if (Device.GetInstalledCount() == 0)
            {
                Debug.LogError("No Kinect Device Found");
                yield break;
            }

            _Device = Device.Open();

            var configuration = new DeviceConfiguration
            {
                ColorFormat = ImageFormat.ColorBGRA32,
                ColorResolution = ColorResolution.R1080p,
                DepthMode = DepthMode.NFOV_2x2Binned,
                SynchronizedImagesOnly = true,
                CameraFPS = FPS.FPS30
            };

            _Device.StartCameras(configuration);

            var kinectCalibration = _Device.GetCalibration(DepthMode.NFOV_2x2Binned, ColorResolution.R1080p).CreateTransformation();

            SetupTextures(ref ColorImage, ref DepthImage, ref ColorInDepthImage);

            Material PointcloudMat = SetupPointcloudShader(ColorInDepthImage, ref DepthImage);

            while (true)
            {
                using (var capture = _Device.GetCapture())
                {
                    ColorImage.LoadRawTextureData(capture.Color.Memory.ToArray());
                    ColorImage.Apply();

                    DepthImage.LoadRawTextureData(capture.Depth.Memory.ToArray());
                    DepthImage.Apply();
                    ColorInDepthImage.LoadRawTextureData(kinectCalibration.ColorImageToDepthCamera(capture).Memory.ToArray());
                    ColorInDepthImage.Apply();
                }

                if (EnableOriginalPointcloud) PointcloudMat.EnableKeyword("_ORIGINALPC_ON");
                else PointcloudMat.DisableKeyword("_ORIGINALPC_ON");

                int pixel_count = DepthImage.width * DepthImage.height;
                PointcloudMat.SetMatrix("_PointcloudOrigin", transform.localToWorldMatrix);
                PointcloudMat.SetFloat("_MaxPointDistance", MaxPointDistance);

                OnSetPointcloudProperties(PointcloudMat);

                Graphics.DrawProcedural(PointcloudMat, new Bounds(transform.position, Vector3.one * 10), MeshTopology.Points, pixel_count);

                yield return null;
            }
        }

        private Texture2D GenerateXYTable()
        {
            var cal = _Device.GetCalibration();
            Texture2D xylookup = new Texture2D(DepthImage.width, DepthImage.height, TextureFormat.RGBAFloat, false);
            Vector2[] data = new Vector2[xylookup.width * xylookup.height];
            int idx = 0;

            System.Numerics.Vector2 p = new System.Numerics.Vector2();
            System.Numerics.Vector3? ray;

            for (int y = 0; y < xylookup.height; y++)
            {
                p.Y = y;
                for (int x = 0; x < xylookup.width; x++)
                {
                    p.X = x;
                    ray = cal.TransformTo3D(p, 1f, CalibrationDeviceType.Depth, CalibrationDeviceType.Depth);
                    if (ray.HasValue)
                    {
                        float xf = ray.Value.X;
                        float yf = ray.Value.Y;
                        float zf = ray.Value.Z;
                        data[idx].x = xf;
                        data[idx].y = yf;
                        xylookup.SetPixel(x, y, new Color((xf + 1) / 2, (yf + 1) / 2, 0, 0));

                    }
                    else
                    {
                        xylookup.SetPixel(x, y, new Color(0, 0, 0, 0));
                        data[idx].x = 0;
                        data[idx].y = 0;
                    }
                }
            }

            xylookup.Apply();
            return xylookup;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {

            if (EnableARBackground && ARBackgroundMaterial)
            {
                Graphics.Blit(ColorImage, destination, new Vector2(1, -1), Vector2.zero);
                Graphics.Blit(source, destination, ARBackgroundMaterial);
            }
            else
                Graphics.Blit(source, destination);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            Task.WaitAny(Task.Delay(1000));

            if (_Device != null)
            {
                _Device.StopCameras();
                _Device.Dispose();
            }
        }
    }
}