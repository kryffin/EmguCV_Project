using System;
using UnityEngine;
using UnityEngine.UI;
using Emgu;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System.Threading;

public class DetectionVisage : MonoBehaviour
{

    private VideoCapture _vc;
    private Mat _frame;
    private Vector2Int _frameSize;
    private Texture2D _texture;

    public RawImage WebcamScreen;

    private void Start()
    {
        /*
         *  Cette fonction servira à initialiser la webcam et le filtre de cascade de Haar, à
         *  associer la fonction HandleWebcamQueryFrame() à l’évènement d’acquisition d’une image
         *  et enfin à récupérer le GameObjet sur lequel on va afficher notre webcam dans le jeu.
        */

        _vc = new VideoCapture();
        _vc.FlipVertical = true;
        _vc.ImageGrabbed += HandleWebcamQueryFrame;

        _frameSize = new Vector2Int((int)_vc.GetCaptureProperty(CapProp.FrameWidth), (int)_vc.GetCaptureProperty(CapProp.FrameHeight));

        _frame = new Mat();

        _texture = new Texture2D(_frameSize.x, _frameSize.y, TextureFormat.BGRA32, false);
        _texture.Apply();

        WebcamScreen.texture = _texture;
        WebcamScreen.SetNativeSize();

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
                UnityEditor.EditorApplication.isPlaying = false;
            }

            //Logique du jeu
            //Affichage + <-- WebcamFrame (ressource commune)
            DisplayFrameOnPlane();
        }
    }

    private void HandleWebcamQueryFrame(object sender, EventArgs e)
    {
        /*
         * Fonction appelée lors de l’évènement d’acquisition. C’est ici
         * que se fera la récupération de la frame courante ainsi que la détection de visage
        */

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

        CvInvoke.Resize(_frame, _frame, new System.Drawing.Size(_frameSize.x, _frameSize.y));
        CvInvoke.CvtColor(_frame, _frame, ColorConversion.Bgr2Bgra);

        //CvInvoke.Imshow("win", _frame); //DEBUG

        _texture.LoadRawTextureData(_frame.ToImage<Bgra, byte>().Bytes);

        _texture.Apply();
    }

    private void OnDestroy()
    {
        /*
         * Fermeture du flux de la caméra.
        */

        if (_vc != null)
        {
            _vc.Stop();
            _vc.Dispose();
        }
    }
}
