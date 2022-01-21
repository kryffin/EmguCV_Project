using System;
using UnityEngine;
using UnityEngine.UI;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

public class DetectionFormes : MonoBehaviour
{
    private VideoCapture _vc; //webcam
    private Mat _frame; //frame on which the webcam will write
    private bool _areDebugWindowsOpened = false; //used to close the debug windows at runtime
    private bool _isWebcamWindowOpened = false; //used to close the webcam window at runtime

    [Header("Gameplay Loop")]

    public Transform Player1;
    public Transform Player2;

    [Header("Webcam Settings")]

    public RawImage WebcamScreen;
    [Tooltip("Recommended : 1280 x 720")]
    public Vector2Int FrameSize;

    [Header("Shape Detection Settings")]

    [Tooltip("Recommended : 50")]
    public double CannyThreshold = 50;
    [Tooltip("Recommended : 150")]
    public double CannyThresholdLinking = 150;
    [Tooltip("Recommended : 250")]
    public float MinRectSize = 250;

    [Header("DEBUG")]

    [Tooltip("Displays the webcam in a different window")]
    public bool WebcamInDifferentWindow;
    [Tooltip("Opens 2 additionnal windows displaying the red and blue captured by the webcam")]
    public bool DebugWindows;

    private void Start()
    {
        // Setting up the webcam
        _vc = new VideoCapture(0, VideoCapture.API.DShow);
        _vc.SetCaptureProperty(CapProp.Fps, 30);
        _vc.SetCaptureProperty(CapProp.FrameWidth, FrameSize.x);
        _vc.SetCaptureProperty(CapProp.FrameHeight, FrameSize.y);

        _vc.ImageGrabbed += HandleWebcamQueryFrame;

        _frame = new Mat();

        // Enables or not the webcam's Raw Image by turning it's alpha to 0
        if (WebcamInDifferentWindow)
            WebcamScreen.color = new UnityEngine.Color(1, 1, 1, .0f);
        else
            WebcamScreen.color = new UnityEngine.Color(1, 1, 1, .5f);

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

    // Processes a grayscale image and draws the recognized rectangles onto the frame
    private void ProcessChannel(Image<Gray, byte> channel, bool firstPlayer = true)
    {
        UMat cannyEdges = new UMat();
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        CvInvoke.Canny(channel, cannyEdges, CannyThreshold, CannyThresholdLinking);
        CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);

        List<RotatedRect> boxList = new List<RotatedRect>(); //stores each box found on screen

        for (int i = 0; i < contours.Size; i++)
        {
            VectorOfPoint approxContour = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(contours[i], approxContour, CvInvoke.ArcLength(contours[i], true) * 0.05f, true);

            // Only considers contours with area greater than MinRectSize
            if (CvInvoke.ContourArea(approxContour, false) > MinRectSize)
            {
                // Rectangle/Square shape has 4 contours
                if (approxContour.Size == 4)
                {
                    bool isRectangle = true;
                    Point[] pts = approxContour.ToArray();
                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                    // Keeps only corners between 80 and 100 degrees
                    for (int j = 0; j < edges.Length; j++)
                    {
                        double angle = Math.Abs(edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                        if (angle < 80f || angle > 100f)
                        {
                            isRectangle = false;
                            break;
                        }
                    }

                    if (isRectangle)
                        boxList.Add(CvInvoke.MinAreaRect(approxContour));
                }
            }
        }

        // Draws squares around the recognized boxes
        foreach (RotatedRect box in boxList)
        {
            CvInvoke.Polylines(_frame, Array.ConvertAll(box.GetVertices(), Point.Round), true, new Bgr(System.Drawing.Color.Green).MCvScalar, 2);

            if (firstPlayer)
                Player1.position = new Vector3(Player1.position.x, -(box.Center.Y - (FrameSize.y / 2f)) / 50f, 0f);
            else
                Player2.position = new Vector3(Player2.position.x, -(box.Center.Y - (FrameSize.y / 2f)) / 50f, 0f);
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
        Image<Hsv, byte> hsv = new Image<Hsv, byte>(FrameSize.x, FrameSize.y);
        CvInvoke.CvtColor(_frame, hsv, ColorConversion.Bgr2Hsv);

        // Filters out every color but the red
        Image<Gray, byte> redFrame = hsv.InRange(new Hsv(0, 100, 100), new Hsv(10, 255, 255)) + hsv.InRange(new Hsv(160, 100, 100), new Hsv(179, 255, 255));

        // Filters out every color but the blue
        Image<Gray, byte> blueFrame = hsv.InRange(new Hsv(105, 100, 100), new Hsv(135, 255, 255));
        
        // Displays the red and blue captured colors in seperate windows
        if (DebugWindows)
        {
            CvInvoke.Imshow("Red", redFrame);
            CvInvoke.Imshow("Blue", blueFrame);

            _areDebugWindowsOpened = true;
        }

        ProcessChannel(redFrame);
        ProcessChannel(blueFrame, false);

        // Displays the webcam frame in a seperate window
        if (WebcamInDifferentWindow)
        {
            CvInvoke.Imshow("Webcam", _frame);
            _isWebcamWindowOpened = true;
        }
        
        WebcamScreen.texture = _frame.ToTexture2D();
    }

    private void OnValidate()
    {
        // Turns transparent the webcam ingame screen when displaying the webcam to a different window
        if (UnityEditor.EditorApplication.isPlaying && !_isWebcamWindowOpened && WebcamInDifferentWindow)
            WebcamScreen.color = new UnityEngine.Color(1, 1, 1, .0f);

        // Destroys the webcam window when it's open and the WebcamInDifferentWindow bool is unchecked
        if (UnityEditor.EditorApplication.isPlaying && _isWebcamWindowOpened && !WebcamInDifferentWindow)
        {
            WebcamScreen.color = new UnityEngine.Color(1, 1, 1, .5f);
            CvInvoke.DestroyWindow("Webcam");

            _isWebcamWindowOpened = false;
        }

        // Destroys red & blue windows when they are open and the DebugWindows bool is unchecked
        if (UnityEditor.EditorApplication.isPlaying && _areDebugWindowsOpened && !DebugWindows)
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
