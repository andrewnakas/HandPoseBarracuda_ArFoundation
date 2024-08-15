using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System;

public class ArCameraTexture : MonoBehaviour
{
    public ARCameraManager cameraManager;
    public Material targetMaterial;

    private Texture2D _cameraTexture;
    private bool _textureInitialized;

    private void OnEnable()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    private void OnDisable()
    {
        if (cameraManager != null)
        {
            cameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!_textureInitialized)
        {
            CreateTexture();
        }

        if (_cameraTexture != null)
        {
            UpdateCameraTexture();
        }
    }

    private void CreateTexture()
    {
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            _cameraTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            _textureInitialized = true;
            targetMaterial.mainTexture = _cameraTexture;
            image.Dispose();
        }
    }

    private unsafe void UpdateCameraTexture()
    {
        if (cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            XRCpuImage.ConversionParams conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorY
            };

            int size = image.GetConvertedDataSize(conversionParams);
            NativeArray<byte> buffer = new NativeArray<byte>(size, Allocator.Temp);

            image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

            _cameraTexture.LoadRawTextureData(buffer);
            _cameraTexture.Apply();

            buffer.Dispose();
            image.Dispose();
        }
    }
}