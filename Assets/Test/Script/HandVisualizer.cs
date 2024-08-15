using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Klak.TestTools;
using MediaPipe.HandPose;

public sealed class HandVisualizer : MonoBehaviour
{
    #region Editable attributes
    [SerializeField] ImageSource _source = null;
    public bool isUsingAR;
    public Material targetMat;
    [SerializeField] Camera _camera = null;
    [SerializeField] ResourceSet _resources = null;
    [SerializeField] GameObject _jointPrefab = null;
    [SerializeField] GameObject _bonePrefab = null;
    [SerializeField] float _jointScale = 0.01f;
    [SerializeField] float _boneScale = 0.005f;
    [SerializeField] float _depthScale = 1f;
    [SerializeField] float _minDepth = 0.1f;
    [SerializeField] float _maxDepth = 10f;
    [SerializeField] float _visualizationDistance = 0.5f;
    [SerializeField] float _visualizationScale = 0.1f;
    [SerializeField] float _xOffset = 0f;
    [SerializeField] float _yOffset = 0f;
    [SerializeField] float _zOffset = 0f;
    [Space]
    [SerializeField] RawImage _mainUI = null;
    [SerializeField] RawImage _cropUI = null;
    [SerializeField] Shader _handRegionShader = null;
    [Space]
    [SerializeField] Canvas _settingsCanvas = null;
    [SerializeField] InputField _depthScaleInput = null;
    [SerializeField] InputField _minDepthInput = null;
    [SerializeField] InputField _maxDepthInput = null;
    [SerializeField] InputField _visualizationDistanceInput = null;
    [SerializeField] InputField _visualizationScaleInput = null;
    [SerializeField] InputField _xOffsetInput = null;
    [SerializeField] InputField _yOffsetInput = null;
    [SerializeField] InputField _zOffsetInput = null;
    #endregion

    #region Private members
    HandPipeline _pipeline;
    List<GameObject> _joints = new List<GameObject>();
    List<GameObject> _bones = new List<GameObject>();
    Vector3[] _worldSpaceKeyPoints = new Vector3[21];
    Material _handRegionMaterial;
    GameObject _visualizationRoot;
    #endregion

    #region Joint connections
    readonly int[][] _boneConnections = new int[][]
    {
        new int[] { 0, 1, 2, 3, 4 },      // Thumb
        new int[] { 0, 5, 6, 7, 8 },      // Index finger
        new int[] { 0, 9, 10, 11, 12 },   // Middle finger
        new int[] { 0, 13, 14, 15, 16 },  // Ring finger
        new int[] { 0, 17, 18, 19, 20 }   // Pinky
    };
    #endregion

    #region MonoBehaviour implementation
    void Start()
    {
        _pipeline = new HandPipeline(_resources);

        _handRegionMaterial = new Material(_handRegionShader);
        _handRegionMaterial.SetBuffer("_Image", _pipeline.HandRegionCropBuffer);

        _cropUI.material = _handRegionMaterial;

        _visualizationRoot = new GameObject("Hand Visualization Root");
        _visualizationRoot.transform.SetParent(_camera.transform, false);
        
        LoadSettings();
        UpdateVisualizationTransform();

        for (int i = 0; i < 21; i++)
        {
            GameObject joint = Instantiate(_jointPrefab, Vector3.zero, Quaternion.identity, _visualizationRoot.transform);
            joint.transform.localScale = Vector3.one * _jointScale;
            _joints.Add(joint);
        }

        foreach (int[] connection in _boneConnections)
        {
            for (int i = 1; i < connection.Length; i++)
            {
                GameObject bone = Instantiate(_bonePrefab, Vector3.zero, Quaternion.identity, _visualizationRoot.transform);
                bone.transform.localScale = new Vector3(_boneScale, _boneScale, 1);
                _bones.Add(bone);
            }
        }

        InitializeUI();
    }

    void OnDestroy()
    {
        _pipeline.Dispose();
        Destroy(_handRegionMaterial);
        Destroy(_visualizationRoot);
    }

    void LateUpdate()
    {
        if (isUsingAR == false)
        {
            _pipeline.ProcessImage(_source.Texture);
        }
        else
        {
            _pipeline.ProcessImage(targetMat.mainTexture);
        }
        ConvertToWorldSpace();

        for (int i = 0; i < _joints.Count; i++)
        {
            _joints[i].transform.localPosition = _worldSpaceKeyPoints[i];
        }

        int boneIndex = 0;
        foreach (int[] connection in _boneConnections)
        {
            for (int i = 1; i < connection.Length; i++)
            {
                Vector3 start = _worldSpaceKeyPoints[connection[i - 1]];
                Vector3 end = _worldSpaceKeyPoints[connection[i]];
                Vector3 center = (start + end) / 2;
                Vector3 direction = end - start;
                
                GameObject bone = _bones[boneIndex];
                bone.transform.localPosition = center;
                bone.transform.up = direction.normalized;
                bone.transform.localScale = new Vector3(_boneScale, direction.magnitude / 2, _boneScale);
                
                boneIndex++;
            }
        }

        if (isUsingAR == false)
        {
            _mainUI.texture = _source.Texture;
            _cropUI.texture = _source.Texture;        
        }
        else
        {
            _mainUI.texture = targetMat.mainTexture;
            _cropUI.texture = targetMat.mainTexture;     
        }
    }
    #endregion

    #region Helper methods
    void ConvertToWorldSpace()
    {
        ComputeBuffer keyPointBuffer = _pipeline.KeyPointBuffer;
        Vector4[] keyPoints = new Vector4[21];
        keyPointBuffer.GetData(keyPoints);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector4 kp in keyPoints)
        {
            min.x = Mathf.Min(min.x, kp.x);
            min.y = Mathf.Min(min.y, kp.y);
            max.x = Mathf.Max(max.x, kp.x);
            max.y = Mathf.Max(max.y, kp.y);
        }
        float handSize = Mathf.Max(max.x - min.x, max.y - min.y);

        float depth = Mathf.Lerp(_minDepth, _maxDepth, 1 - handSize) * _depthScale;

        for (int i = 0; i < keyPoints.Length; i++)
        {
            Vector3 viewportPoint = new Vector3(keyPoints[i].x - 0.5f, keyPoints[i].y - 0.5f, depth);
            
            if (isUsingAR)
            {
                // Rotate the point 90 degrees to the right around the Z-axis
                float x = viewportPoint.x;
                float y = viewportPoint.y;
                viewportPoint.x = -y;
                viewportPoint.y = x;
            }
            
            // Apply offsets
            viewportPoint += new Vector3(_xOffset, _yOffset, _zOffset);
            
            _worldSpaceKeyPoints[i] = viewportPoint;
        }
    }

    void InitializeUI()
    {
        _depthScaleInput.text = _depthScale.ToString();
        _minDepthInput.text = _minDepth.ToString();
        _maxDepthInput.text = _maxDepth.ToString();
        _visualizationDistanceInput.text = _visualizationDistance.ToString();
        _visualizationScaleInput.text = _visualizationScale.ToString();
        _xOffsetInput.text = _xOffset.ToString();
        _yOffsetInput.text = _yOffset.ToString();
        _zOffsetInput.text = _zOffset.ToString();

        _depthScaleInput.onEndEdit.AddListener(OnDepthScaleChanged);
        _minDepthInput.onEndEdit.AddListener(OnMinDepthChanged);
        _maxDepthInput.onEndEdit.AddListener(OnMaxDepthChanged);
        _visualizationDistanceInput.onEndEdit.AddListener(OnVisualizationDistanceChanged);
        _visualizationScaleInput.onEndEdit.AddListener(OnVisualizationScaleChanged);
        _xOffsetInput.onEndEdit.AddListener(OnXOffsetChanged);
        _yOffsetInput.onEndEdit.AddListener(OnYOffsetChanged);
        _zOffsetInput.onEndEdit.AddListener(OnZOffsetChanged);
    }

    void UpdateVisualizationTransform()
    {
        _visualizationRoot.transform.localPosition = new Vector3(0, 0, _visualizationDistance);
        _visualizationRoot.transform.localScale = Vector3.one * _visualizationScale;
    }

    void OnDepthScaleChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _depthScale = result;
            SaveSettings();
        }
    }

    void OnMinDepthChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _minDepth = result;
            SaveSettings();
        }
    }

    void OnMaxDepthChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _maxDepth = result;
            SaveSettings();
        }
    }

    void OnVisualizationDistanceChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _visualizationDistance = result;
            UpdateVisualizationTransform();
            SaveSettings();
        }
    }

    void OnVisualizationScaleChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _visualizationScale = result;
            UpdateVisualizationTransform();
            SaveSettings();
        }
    }

    void OnXOffsetChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _xOffset = result;
            SaveSettings();
        }
    }

    void OnYOffsetChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _yOffset = result;
            SaveSettings();
        }
    }

    void OnZOffsetChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            _zOffset = result;
            SaveSettings();
        }
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat("HandVisualizer_DepthScale", _depthScale);
        PlayerPrefs.SetFloat("HandVisualizer_MinDepth", _minDepth);
        PlayerPrefs.SetFloat("HandVisualizer_MaxDepth", _maxDepth);
        PlayerPrefs.SetFloat("HandVisualizer_VisualizationDistance", _visualizationDistance);
        PlayerPrefs.SetFloat("HandVisualizer_VisualizationScale", _visualizationScale);
        PlayerPrefs.SetFloat("HandVisualizer_XOffset", _xOffset);
        PlayerPrefs.SetFloat("HandVisualizer_YOffset", _yOffset);
        PlayerPrefs.SetFloat("HandVisualizer_ZOffset", _zOffset);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        _depthScale = PlayerPrefs.GetFloat("HandVisualizer_DepthScale", _depthScale);
        _minDepth = PlayerPrefs.GetFloat("HandVisualizer_MinDepth", _minDepth);
        _maxDepth = PlayerPrefs.GetFloat("HandVisualizer_MaxDepth", _maxDepth);
        _visualizationDistance = PlayerPrefs.GetFloat("HandVisualizer_VisualizationDistance", _visualizationDistance);
        _visualizationScale = PlayerPrefs.GetFloat("HandVisualizer_VisualizationScale", _visualizationScale);
        _xOffset = PlayerPrefs.GetFloat("HandVisualizer_XOffset", _xOffset);
        _yOffset = PlayerPrefs.GetFloat("HandVisualizer_YOffset", _yOffset);
        _zOffset = PlayerPrefs.GetFloat("HandVisualizer_ZOffset", _zOffset);
    }
    #endregion
}