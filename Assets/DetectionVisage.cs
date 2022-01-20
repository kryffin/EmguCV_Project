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
    private Image<Bgr, byte> _frame;
    private Thread _thread;

    public RawImage WebcamScreen;

    private void Start()
    {
        /*
         *  Cette fonction servira à initialiser la webcam et le filtre de cascade de Haar, à
         *  associer la fonction HandleWebcamQueryFrame() à l’évènement d’acquisition d’une image
         *  et enfin à récupérer le GameObjet sur lequel on va afficher notre webcam dans le jeu.
        */

        _vc = new VideoCapture();
        _thread = new Thread(() => _vc.ImageGrabbed += HandleWebcamQueryFrame);
        _vc.Start();

        _frame = new Image<Bgr, byte>(_vc.Width, _vc.Height);

        _thread.Start();
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
            _vc.Grab(); //Call HandleWebcamQueryFrame() event

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
        Debug.Log(_frame.Width + " " + _frame.Height);

        // --> WebcamFrame (ressource commune)
    }

    private void DisplayFrameOnPlane()
    {
        /*
         * Permet de charger la frame courante dans une texture et de
         * l’appliquer à notre GameObject.
        */

        Texture2D texture = new Texture2D(_frame.Width, _frame.Height);

        for (int x = 0; x < _frame.Width; x++)
            for (int y = 0; y < _frame.Height; y++)
                texture.SetPixel(x, y, new Color(_frame.Data[y, x, 2], _frame.Data[y, x, 1], _frame.Data[y, x, 0]));

        WebcamScreen.texture = texture;
    }

    private void OnDestroy()
    {
        /*
         * Fermeture du flux de la caméra.
        */

        _vc.Stop();
        _vc.Dispose();
    }
}
