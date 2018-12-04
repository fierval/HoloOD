using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Devices.Enumeration;
using Windows.AI.MachineLearning;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Capture.Frames;
using Windows.Foundation;
using Windows.Foundation.Collections;
#endif

public struct Resolution
{
    /// <summary>
    /// The width property.
    /// </summary>
    public readonly int width;

    /// <summary>
    /// The height property.
    /// </summary>
    public readonly int height;

    public Resolution(int width, int height)
    {
        this.width = width;
        this.height = height;
    }

    public override string ToString()
    {
        return $"width={width}, height={height}";
    }
}


public class RawVideoCapture : MonoBehaviour
{
    public GameObject Label;
    private TextMesh LabelText;
    TimeSpan predictEvery = TimeSpan.FromMilliseconds(50);
    string textToDisplay;
    bool textToDisplayChanged;

    public float detectionThreshold = 0.5f;

#if UNITY_WSA && !UNITY_EDITOR
    TinyYoloV2O12Model imageRecoModel;
    MediaCapture MediaCapture;
#endif

    void Start()
    {
        LabelText = Label.GetComponent<TextMesh>();

#if UNITY_WSA && !UNITY_EDITOR
        InitializeModel();
        CreateMediaCapture();
#else
        DisplayText("Does not work in player.");
#endif
    }

    private void DisplayText(string text)
    {
        textToDisplay = text;
        textToDisplayChanged = true;
    }

#if UNITY_WSA && !UNITY_EDITOR
    public async void InitializeModel()
    {
        bool res = await ObjectDetector.LoadModel();
        if (!res)
        {
            Debug.Log("Could not load model");
        }

        ObjectDetector.DetectionThreshold = detectionThreshold;
    }

    public IEnumerable<Resolution> GetSupportedResolutions()
    {
        List<Resolution> resolutions = new List<Resolution>();
        var STREAM_TYPE = MediaStreamType.VideoPreview;
        var allPropertySets = MediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(STREAM_TYPE).Select(x => x as VideoEncodingProperties); //Returns IEnumerable<VideoEncodingProperties>
        foreach (var propertySet in allPropertySets)
        {
            resolutions.Add(new Resolution((int)propertySet.Width, (int)propertySet.Height));
        }

        return resolutions;
    }

    public async void CreateMediaCapture()
    {
        var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
        var deviceInformation = devices.FirstOrDefault();

        MediaCapture = new MediaCapture();

        MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings { VideoDeviceId = deviceInformation.Id };
        settings.StreamingCaptureMode = StreamingCaptureMode.Video;

        IReadOnlyList<MediaCaptureVideoProfile> profiles = MediaCapture.FindAllVideoProfiles(deviceInformation.Id);

        var match = (from profile in profiles
                     from desc in profile.SupportedRecordMediaDescription
                     select new { profile, desc }).OrderBy(pd => pd.desc.Width * pd.desc.Height).FirstOrDefault();

        if (match != null)
        {
            settings.VideoProfile = match.profile;
            settings.RecordMediaDescription = match.desc;
        }
        else
        {
            // Could not locate a WVGA 30FPS profile, use default video recording profile
            settings.VideoProfile = profiles[0];
        }
        await MediaCapture.InitializeAsync(settings);

        CreateFrameReader();
    }

    private async void CreateFrameReader()
    {
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

        MediaFrameSourceGroup selectedGroup = null;
        MediaFrameSourceInfo colorSourceInfo = null;

        foreach (var sourceGroup in frameSourceGroups)
        {
            foreach (var sourceInfo in sourceGroup.SourceInfos)
            {
                if (sourceInfo.MediaStreamType == MediaStreamType.VideoPreview
                    && sourceInfo.SourceKind == MediaFrameSourceKind.Color)
                {
                    colorSourceInfo = sourceInfo;
                    break;
                }
            }
            if (colorSourceInfo != null)
            {
                selectedGroup = sourceGroup;
                break;
            }
        }

        var colorFrameSource = MediaCapture.FrameSources[colorSourceInfo.Id];
        var preferredFormat = colorFrameSource.SupportedFormats.Where(format =>
        {
            return format.Subtype == MediaEncodingSubtypes.Bgra8;

        }).FirstOrDefault();

        var mediaFrameReader = await MediaCapture.CreateFrameReaderAsync(colorFrameSource);
        await mediaFrameReader.StartAsync();

        StartPullFrames(mediaFrameReader);
    }

    private void StartPullFrames(MediaFrameReader sender)
    {
        Task.Run(async () =>
        {
            for (; ; )
            {
                var frameReference = sender.TryAcquireLatestFrame();
                var videoFrame = frameReference?.VideoMediaFrame?.GetVideoFrame();

                if (videoFrame == null)
                {
                    continue; //ignoring frame
                }

                if (videoFrame.Direct3DSurface == null)
                {
                    continue; //ignoring frame
                }


                try
                {
                    if (ObjectDetector.CameraWidth == 0)
                    {
                        ObjectDetector.CameraWidth = videoFrame.Direct3DSurface.Description.Width;
                        ObjectDetector.CameraHeight = videoFrame.Direct3DSurface.Description.Height;
                    }

                    var preds = await ObjectDetector.AnalyzeImage(videoFrame);
                    textToDisplay = preds.OrderByDescending(b => b.Confidence).Take(5).Aggregate("", (a, l) => $"{a}\r\n{l.Label}");
                    DisplayText(textToDisplay);
                }
                catch
                {
                    //Log errors
                }

                await Task.Delay(predictEvery);
            }

        });
    }
#endif

    void Update()
    {
        if (textToDisplayChanged)
        {
            LabelText.text = textToDisplay;
            textToDisplayChanged = false;
        }
    }
}
