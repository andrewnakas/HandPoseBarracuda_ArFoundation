HandPoseBarracuda
=================
![gif](https://imgur.com/a/YdvSpW0.gif)
![gif](https://i.imgur.com/jvHmCMc.gif)


**HandPoseBarracuda_ArFoundation** is a proof-of-concept implementation of a neural network
hand/finger tracker that works with a monocular color camera now with an added screen-to-world-space projector for 3D
  applications working with the defult scene and arfoundation. 

All cameras and hands are different so i added 8 input fields so you can mess with the world space projector to fit your needs depending on device. 

Shout out to Keijiro for the initial project. 
https://github.com/keijiro/HandPoseBarracuda

Basically, HandPoseBarracuda is a partial port of the [MediaPipe Hands]
pipeline. Although it is not a straight port of the original package, it uses
the same basic design and the same pre-trained models.

[MediaPipe Hands]: https://google.github.io/mediapipe/solutions/hands.html

Note that this is just a proof-of-concept implementation. It lacks some
essential features needed for practical applications:

- **It only accepts a single hand.** Although you can reuse the most part of
  the implementation, you will need to redesign the system to support multiple
  hands.

Related projects
----------------

HandPoseBarracuda uses the following sub-packages:

- [BlazePalmBarracuda (lightweight hand detector)](https://github.com/keijiro/BlazePalmBarracuda)
- [HandLandmarkBarracuda (hand/finger landmark detector)](https://github.com/keijiro/HandLandmarkBarracuda)

