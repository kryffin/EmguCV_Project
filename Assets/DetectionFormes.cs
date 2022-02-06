using System;
using UnityEngine;
using UnityEngine.UI;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Threading;
using System.Drawing;
using UnityEngine.Serialization;

public class DetectionFormes : MonoBehaviour
{
    private VideoCapture _vc; //webcam
    private Mat _frame; //frame on which the webcam will write
    private Texture2D _imageTexture; // rawImage texture, used to avoid memory leaks
    private bool _areDebugWindowsOpened; //used to close the debug windows at runtime
    private bool _isWebcamWindowOpened; //used to close the webcam window at runtime

    [Header("Gameplay Loop")]

    public Transform player1;
    public Transform player2;

    [Header("Webcam Settings")]

    public RawImage webcamScreen;
    [Tooltip("Recommended : 1280 x 720")]
    public Vector2Int frameSize;

    [Header("Shape Detection Settings")]

    [Tooltip("Recommended : 50")]
    public double cannyThreshold = 50;
    [Tooltip("Recommended : 150")]
    public double cannyThresholdLinking = 150;
    [FormerlySerializedAs("MinRectSize")] [Tooltip("Recommended : 250")]
    public float minRectSize = 250;
    public int morphIterations = 10;
    

    [Header("Color Detection Settings")]
    public Vector3 blueLower = new Vector3(90, 50, 70);
    public Vector3 blueUpper = new Vector3(128, 255, 255);
    public Vector3 greenLower = new Vector3(36, 50, 70);
    public Vector3 greenUpper = new Vector3(89, 255, 255);
    //public Vector3 red1Lower = new Vector3(159, 50, 70);
    //public Vector3 red1Upper = new Vector3(180, 255, 255);
    //public Vector3 red2Lower = new Vector3(0, 50, 70);
    //public Vector3 red2Upper = new Vector3(9, 255, 255);

    [Header("DEBUG")]

    [Tooltip("Displays the webcam in a different window")]
    public bool webcamInDifferentWindow;
    [Tooltip("Opens 2 additional windows displaying the red and blue captured by the webcam")]
    public bool debugWindows;

    private void Start()
    {
        // Setting up the webcam
        _vc = new VideoCapture(0, VideoCapture.API.DShow);
        _vc.SetCaptureProperty(CapProp.Fps, 30);
        _vc.SetCaptureProperty(CapProp.FrameWidth, frameSize.x);
        _vc.SetCaptureProperty(CapProp.FrameHeight, frameSize.y);

        _vc.ImageGrabbed += HandleWebcamQueryFrame;

        _frame = new Mat();

        _imageTexture = new Texture2D(frameSize.x, frameSize.y);
        webcamScreen.texture = _imageTexture;

        // Enables or not the webcam's Raw Image by turning it's alpha to 0
        webcamScreen.color = webcamInDifferentWindow ? new UnityEngine.Color(1, 1, 1, .0f) : new UnityEngine.Color(1, 1, 1, .5f);

        _vc.Start();
    }

    private void Update()
    {
        if (!_vc.IsOpened)
        {
            Debug.LogError("Update() : VideoCapture is not opened !");
            return;
        }

        if (!_vc.Grab())
            Debug.LogError("Update() : VideoCapture frame could not be grabbed !");

        DisplayFrameOnPlane();
    }

    // Called when a frame is grabbed
    private void HandleWebcamQueryFrame(object sender, EventArgs e)
    {
        _vc.Retrieve(_frame);
    }

    private void applyMorph(Image<Gray, byte> image)
    {
        Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(1, 1));
        CvInvoke.MorphologyEx(image, image, MorphOp.Close, kernel, new Point(-1, -1), morphIterations, BorderType.Default, new MCvScalar());
        CvInvoke.MorphologyEx(image, image, MorphOp.Open, kernel, new Point(-1, -1), morphIterations, BorderType.Default, new MCvScalar());
    }

    // Processes a grayscale image and draws the recognized rectangles onto the frame
    private void ProcessChannel(Image<Gray, byte> channel, bool firstPlayer = true)
    {
        UMat cannyEdges = new UMat();
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        CvInvoke.Canny(channel, cannyEdges, cannyThreshold, cannyThresholdLinking);
        CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
        
        //Debug.Log(CvInvoke.ContourArea(contours[0], false));
        for (int i = 0; i < contours.Size; i++)
        {
            double area = CvInvoke.ContourArea(contours[i]);
            if (area > minRectSize)
            {
                Moments moments = CvInvoke.Moments(contours[i]);
                int cX = (int) (moments.M10 / moments.M00);
                int cY = (int) (moments.M01 / moments.M00);
                CvInvoke.DrawContours(_frame, contours, i, new MCvScalar(255, 0, 0), 2);
                CvInvoke.Circle(_frame, new Point(cX, cY), 7, new MCvScalar(255, 255, 255), -1);

                if (firstPlayer)
                    player1.position = new Vector3(player1.position.x, -(cY - (frameSize.y / 2f)) / 50f, 0f);
                else
                    player2.position = new Vector3(player2.position.x, -(cY - (frameSize.y / 2f)) / 50f, 0f);
            }
        }
    }

    // Draws the frame on the UI
    private void DisplayFrameOnPlane()
    {
        if (_frame.IsEmpty)
        {
            Debug.LogWarning("DisplayFrameOnPlane() : Tried to display an empty frame !");
            return;
        }
        // Converts the image to an HSV for a better color filtering
        Image<Hsv, byte> hsv = new Image<Hsv, byte>(frameSize.x, frameSize.y);
        CvInvoke.CvtColor(_frame, hsv, ColorConversion.Bgr2Hsv);

        // Filters out every color but the red
        Image<Gray, byte> greenFrame = hsv.InRange(new Hsv(greenLower.x, greenLower.y, greenLower.z), new Hsv(greenUpper.x, greenUpper.y, greenUpper.z));
        //greenFrame += hsv.InRange(new Hsv(red2Lower.x, red2Lower.y, red2Lower.z), new Hsv(red2Upper.x, red2Upper.y, red2Upper.z));
        
        // Filters out every color but the blue
        Image<Gray, byte> blueFrame = hsv.InRange(new Hsv(blueLower.x, blueLower.y, blueLower.z), new Hsv(blueUpper.x, blueUpper.y, blueUpper.z));
        
        applyMorph(greenFrame);
        applyMorph(blueFrame);
        
        // Displays the red and blue captured colors in separate windows
        if (debugWindows)
        {
            CvInvoke.Imshow("Green", greenFrame);
            CvInvoke.Imshow("Blue", blueFrame);

            _areDebugWindowsOpened = true;
        }

        ProcessChannel(greenFrame);
        ProcessChannel(blueFrame, false);
        // Displays the webcam frame in a separate window
        if (webcamInDifferentWindow)
        {
            CvInvoke.Imshow("Webcam", _frame);
            _isWebcamWindowOpened = true;
        }
        
        Destroy(_imageTexture);
        webcamScreen.texture = _imageTexture = _frame.ToTexture2D();
    }

    private void OnValidate()
    {
        // Turns transparent the webcam ingame screen when displaying the webcam to a different window
        if (UnityEditor.EditorApplication.isPlaying && !_isWebcamWindowOpened && webcamInDifferentWindow)
            webcamScreen.color = new UnityEngine.Color(1, 1, 1, .0f);

        // Destroys the webcam window when it's open and the WebcamInDifferentWindow bool is unchecked
        if (UnityEditor.EditorApplication.isPlaying && _isWebcamWindowOpened && !webcamInDifferentWindow)
        {
            webcamScreen.color = new UnityEngine.Color(1, 1, 1, .5f);
            CvInvoke.DestroyWindow("Webcam");

            _isWebcamWindowOpened = false;
        }

        // Destroys red & blue windows when they are open and the DebugWindows bool is unchecked
        if (UnityEditor.EditorApplication.isPlaying && _areDebugWindowsOpened && !debugWindows)
        {
            CvInvoke.DestroyWindow("Red");
            CvInvoke.DestroyWindow("Blue");

            _areDebugWindowsOpened = false;
        }
    }

    private void OnDestroy()
    {
        if (_vc != null)
        {
            Thread.Sleep(60); //wait a bit for the webcam to stop sending frames
            _vc.Stop();
            _vc.Dispose();
        }

        CvInvoke.DestroyAllWindows();
    }
}
