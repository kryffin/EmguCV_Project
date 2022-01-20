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
    private Texture2D _texture;

    public RawImage WebcamScreen;

    private void Start()
    {
        /*
         *  Cette fonction servira � initialiser la webcam et le filtre de cascade de Haar, �
         *  associer la fonction HandleWebcamQueryFrame() � l��v�nement d�acquisition d�une image
         *  et enfin � r�cup�rer le GameObjet sur lequel on va afficher notre webcam dans le jeu.
        */

        _vc = new VideoCapture();
        _vc.ImageGrabbed += HandleWebcamQueryFrame;
        _vc.Start();

        _frame = new Mat();

        _texture = new Texture2D(_vc.Width, _vc.Height, TextureFormat.RGBA32, false);
        _texture.Apply();

        WebcamScreen.texture = _texture;
        //WebcamScreen.material.mainTexture = _texture;
        WebcamScreen.SetNativeSize();
    }

    private void Update()
    {
        /*
         * Nous appellerons ici la fonction qui sert � d�clencher les �v�nements d�acquisition
         * de la webcam, ainsi que la fonction permettant de mettre � jour l�affichage de l�image sur le
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
         * Fonction appel�e lors de l��v�nement d�acquisition. C�est ici
         * que se fera la r�cup�ration de la frame courante ainsi que la d�tection de visage
        */

        _vc.Retrieve(_frame);

        //Traitement sur l'image

        // --> WebcamFrame (ressource commune)
    }

    private void DisplayFrameOnPlane()
    {
        /*
         * Permet de charger la frame courante dans une texture et de
         * l�appliquer � notre GameObject.
        */

        CvInvoke.Resize(_frame, _frame, new System.Drawing.Size(_vc.Width, _vc.Height));
        CvInvoke.CvtColor(_frame, _frame, ColorConversion.Bgr2Rgba);
        CvInvoke.Flip(_frame, _frame, FlipType.Vertical);

        _texture.LoadRawTextureData(_frame.ToImage<Rgba, byte>().Bytes);

        _texture.Apply();
    }

    private void OnDestroy()
    {
        /*
         * Fermeture du flux de la cam�ra.
        */

        if (_vc != null)
        {
            _vc.Stop();
            _vc.Dispose();
        }
    }
}
