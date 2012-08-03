tron-lc-simulator
=================

Tron Light Cycle Simulator for Entelect Challenge

ChangeLog
=========
- 24/07/2012: - Added code for checking, visualizing and handling unique pole (y=0, y=29) behaviour.
- 28/07/2012: - Added code for executing external start.bat type bots.
- 02/08/2012: - Added integration with my TronLC.Framework project to provide debugging via Visual Studio by dynamically loading bot assemblies during startup.
- 03/08/2012: - Display games won/lost/played per player

ToDo List
=========
- Add in all the rules and checks to find illegal moves made, etc.
- Lots and lots of testing

What is working
============
I pretty much have the visualization, game.state generation for each player's turn, and "Game Over" detection done and working. 
Currently the simulator has a built-in "Random move" AI that I used for testing. 
Also have start.bat type bots supported.
Integrated with TronLC.Framework (http://www.github.com/sschocke/tron-lc-framework) to support dynamic bot loading and debugging.

How to use this project
================
While the simulator can be compiled and used standalone without Visual Studio, this seems a bit pointless for testing and debugging bots, which is the main purpose of this simulator.

To use this simulator in the intended way, you will need a copy of the TronLC.Framework (http://www.github.com/sschocke/tron-lc-framework) project as well.
 - Create a new solution in Visual Studio, or use your existing one with your bots project in it.
 - Add the TronLC.Framework project to the solution. Make sure you do this first, as the TronLCSim project has a Project Dependency on this project.
 - Add the TronLC.Framework as a Project Reference to your existing bot project(s).
 - Change your bot to implement the IAIBot interface (details below)
 - Add the TronLCSim project to the solution.
 - Add your bot project(s) as Project References to the TronLCSim project.
 - Start the TronLCSim project.

Once the simulator is running, hit the "Set Player1" button, and in the dialog select "AIBot Interface DLL Bot". If everything was done correctly, your bot(s) should be listed in the dropdown combobox.

IAIBot Interface Details
=================
The IAIBot Interface has a single method called "ExecuteMove" with the following signature:

void ExecuteMove(string gameStateFile);

This method gets called by the simulator each time it is your bots turn to move. The "gameStateFile" parameter contains the path to the game.state file generated by the simulator for the current state of the game from your point of view. In this regard, the "ExecuteMove" method works identically to the way the bots should work for the Entelect challenge, except that the path is passed via this function call instead of via the command line.

Entelect Challenge
=================

http://challenge.entelect.co.za/
