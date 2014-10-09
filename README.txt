README 

Demo http://youtu.be/203y6WW7W0c

There are a lot of finger recognition projects using Kinect. What sets this project 
apart is its ability to detect and recognize different finger guestures with a 
custom-defined surface. This is hard because as your hand get close to any surface 
the kinect's ability to clearly differentiate your hand and the surface quickly 
decreases. This project also implements a simple gesture recognizer to map different 
gestures to various mouse actions. 

This project was meant to be a proof of concept, so the code was written in C# to get 
us going fast. For a more efficient implementation, you would want to use C++ instead. 

Contributors *****************************************************
GLEN CHAO
IVAN SHAO
NAMAL RAJATHEVA
ELI XI CHEN

Setup Instruction ************************************************
1. Acquire a Kinect for PC (note: "for PC" not "xbox360")
   Will not be able to run the code without the kinect.
2. Acquire a tripod to put your kinect on.
3. Point the kinect at a clear area of your desk. Light color desks will work much better.
4. Install Visual Studio 2012
5. Follow instruct on MSDN to setup Kinect SDK http://msdn.microsoft.com/en-us/library/hh855354.aspx 
6. Open source code in Visual Studio 2012
7. Connect Kinect to PC
8. Run compile and run source code in Visual Studio 2012

First Use ********************************************************
1. Initialize environment with button at the bottom left 
2. Drag the red rectangle to clear flat surface
3. Click "confirm" to define the ActionArea

Possible Gestures ************************************************
1 Finger Movement 		-- Cursor Move
1 Finger Single Click 	-- Mouse Left Click
1 Finger Double Click	-- Mouse Left Double Click
1 Finger Triple Click	-- Mouse Left Triple Click
2 Finger Single Click	-- Mouse Right Click
2 Finger Movement 		-- Vertical Scrolling 
1 Finger Click + Move 	-- Dragging 

Extra Info ********************************************************
Kinect C# SDK Documentation http://msdn.microsoft.com/en-us/library/microsoft.kinect.aspx
Finger recognition algorithm:
* We used a contour finding algorithm 
  3D Hand and Finger Recognition using Kinect, F. Trapero Cerezo, Universidad de Granada (UGR), Spain
* The link to the paper we based our algorithm on doesn't seem to work anymore... read comments in the code for details
