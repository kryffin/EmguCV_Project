using System;
using UnityEngine;
using UnityEngine.UI;
using Emgu;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Threading;
using System.Drawing;
using System.Collections.Generic;

public class DetectionVisage : MonoBehaviour
{

    private VideoCapture _vc;
    private Mat _frame;
    private Texture2D _texture;
    private Thread _t;

    public Transform Player;
    public RawImage WebcamScreen;
    public Vector2Int FrameSize;
    public float MinRectSize = 250;
    public double CannyThreshold = 120;
    public double CannyThresholdLinking = 120;
    public bool FilterOnlyRedBoxes = true;

    private void Start()
    {
        /*
         *  Cette fonction servira à initialiser la webcam et le filtre de cascade de Haar, à
         *  associer la fonction HandleWebcamQueryFrame() à l’évènement d’acquisition d’une image
         *  et enfin à récupérer le GameObjet sur lequel on va afficher notre webcam dans le jeu.
        */

        _vc = new VideoCapture(0, VideoCapture.API.DShow);
        _vc.SetCaptureProperty(CapProp.Fps, 30);
        _vc.SetCaptureProperty(CapProp.FrameWidth, FrameSize.x);
        _vc.SetCaptureProperty(CapProp.FrameHeight, FrameSize.y);

        _vc.ImageGrabbed += HandleWebcamQueryFrame;

        _frame = new Mat();

        /*_texture = new Texture2D(FrameSize.x, FrameSize.y, TextureFormat.BGRA32, false);
        _texture.Apply();*/

        WebcamScreen.color = new UnityEngine.Color(1, 1, 1, .5f);

        _vc.Start();
    }

    private void Update()
    {
        /*
         * Nous appellerons ici la fonction qui sert à déclencher les évènements d’acquisition
         * de la webcam, ainsi que la fonction permettant de mettre à jour l’affichage de l’image sur le
         * GameObject.
        */

        if (_vc.IsOpened)
        {
            //Call HandleWebcamQueryFrame() event
            if (!_vc.Grab())
            {
                Debug.LogError("VideoCapture frame could not be grabbed !");
                //UnityEditor.EditorApplication.isPlaying = false;
            }

            //Logique du jeu
            //Affichage + <-- WebcamFrame (ressource commune)
        }

        DisplayFrameOnPlane();
    }

    private void HandleWebcamQueryFrame(object sender, EventArgs e)
    {
        /*
         * Fonction appelée lors de l’évènement d’acquisition. C’est ici
         * que se fera la récupération de la frame courante ainsi que la détection de visage
        */

        if (_vc.IsOpened)
            _vc.Retrieve(_frame);

        //Traitement sur l'image

        // --> WebcamFrame (ressource commune)
    }

    private void DisplayFrameOnPlane()
    {
        /*
         * Permet de charger la frame courante dans une texture et de
         * l’appliquer à notre GameObject.
        */

        if (_frame.IsEmpty) return;

        Image<Gray, byte> grayFrame = _frame.ToImage<Gray, byte>();

        // Converts grayFrame to only contain the red objects
        if (FilterOnlyRedBoxes)
        {
            Image<Hsv, byte> hsv = new Image<Hsv, byte>(FrameSize.x, FrameSize.y);
            CvInvoke.CvtColor(_frame, hsv, ColorConversion.Bgr2Hsv);
            Image<Gray, byte> lowerRed = hsv.InRange(new Hsv(0, 100, 100), new Hsv(10, 255, 255));
            Image<Gray, byte> upperRed = hsv.InRange(new Hsv(160, 100, 100), new Hsv(179, 255, 255));
            grayFrame = lowerRed + upperRed;
            CvInvoke.Imshow("Grayscale", grayFrame);
        }

        UMat cannyEdges = new UMat();
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        CvInvoke.Canny(grayFrame, cannyEdges, CannyThreshold, CannyThresholdLinking);
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

        // Draws blue bowes around the recognized squares
        foreach (RotatedRect box in boxList)
        {
            CvInvoke.Polylines(_frame, Array.ConvertAll(box.GetVertices(), Point.Round), true, new Bgr(System.Drawing.Color.Blue).MCvScalar, 2);
            Player.position = new Vector3(
                Player.position.x,
                -(box.Center.Y - (FrameSize.y / 2f)) / 50f,
                0f
                );
        }

        WebcamScreen.texture = _frame.ToTexture2D();
    }

    private void OnDestroy()
    {
        /*
         * Fermeture du flux de la caméra.
        */

        if (_vc != null)
        {
            Thread.Sleep(60);
            _vc.Stop();
            _vc.Dispose();
        }
    }
}
