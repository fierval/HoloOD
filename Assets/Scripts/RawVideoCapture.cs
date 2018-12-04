using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.AI.MachineLearning;
using Windows.Storage;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Capture.Frames;
#endif

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

    public async void CreateMediaCapture()
    {
        MediaCapture = new MediaCapture();
        MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
        settings.StreamingCaptureMode = StreamingCaptureMode.Video;
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
            return format.Subtype == MediaEncodingSubtypes.Argb32;

        }).FirstOrDefault();

        var mediaFrameReader = await MediaCapture.CreateFrameReaderAsync(colorFrameSource);
        await mediaFrameReader.StartAsync();

        StartPullFrames(mediaFrameReader);
    }

    private void StartPullFrames(MediaFrameReader sender)
    {
        Task.Run(async () =>
        {
            for (;;)
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
