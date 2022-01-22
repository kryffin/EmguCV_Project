# AR Pong

D�veloppeurs : Nathan Coustance & Nicolas Kleinhentz

Projet universitaire dans le but d'apprendre � se servir d'EmguCV, un wrapper .NET de la librairie OpenCV, le tout incorpor� dans un jeu Unity.

*Read this in other languages : [French](README.md), [English](README.en.md).*

## Gameplay Loop

La gameplay loop correspond au jeu Pong :

![Gameplay Loop](ReadmeResources/ARPong_GameplayLoop.gif)

La balle appara�t au milieu du terrain avec un direction al�atoire suivant un angle de 90� en direction du joueur ayant subit le dernier but.  
Lorsqu'elle rentre dans un but, la balle r�apparait au milieu du terrain.  
Le joueur 1 est repr�sent� par la plaque rouge et le 2 par la plaque 2.

## ARed Controls

Le contr�le des deux plaques se fait gr�ce � des carr�s de couleurs, un rouge et un bleu, d�plac�s devant une webcam :

*[Placeholder : gif d�montrant le d�placement des plaques gr�ce � la webcam]*

La webcam capte l'image et la transmet � un script utilisant EmguCV pour exraire, d'une part, le rouge de l'image et d'une autre le bleu.  
Une fois ceci fait, le script cherche les carr�s contenus dans ces images et utilise leurs positions dans la cam�ra pour d�placer les plaques des joueurs.

### Processus d�taill�

- L'image captur�e par la webcam est transmise au script
- Cette image est convertie en HSV
- La conversion en HSV nous permet de r�cup�rer plus facilement les �l�ments rouges et bleus de l'image [(voir images 1, 2 et 3)](#Annexes)
- Les contours de ces images sont d�tect�s gr�ce � un filtre de Canny
- Pour chacun de ces contours on en r�cup�re des polygones
- On traite ensuite seulement les polygones � 4 c�t�s, d'apr�s lesquels on construit des bo�tes englobantes [(voir images 4 et 5)](#Annexes)
- Les bo�tes englobantes sont utilis�es pour d�placer les plaques des joueurs par rapport � leur position dans l'�cran

## Annexes

![Webcam](ReadmeResources/webcam.png)  
*Image 1 : capture de la webcam*

![Red](ReadmeResources/red.png)  
*Image 2 : couleur rouge de la capture (voir Image 1)*

![Blue](ReadmeResources/blue.png)  
*Image 3 : couleur bleue de la capture (voir Image 1)*

![Sample](ReadmeResources/sample.png)  
*Image 4 : image de test utilis�e pour la reconnaissance de carr�s color�s*

![Square Detection](ReadmeResources/square_detection.png)  
*Image 5 : d�tection de carr�s rouges et bleus sur l'image de test (voir Image 4)*