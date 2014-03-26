MateAnimator
============

Fixing up the open sourced Animator from Unity to be more useful.

Based on: https://github.com/absameen/Animator_Timeline

Now remade to use HOTween: http://www.holoville.com/hotween/index.html

This takes advantage of Sequence and the ability to cache all the tweens at startup. This allows the Animator to be used in critical parts of the game.

Also, Animator is no longer global and can be added to any game object.  You can now have many Sequences running simultaneously.

Camera Transition and Mega Morph has been removed

Import/Export, Code Generator doesn't work. JSON stuff mangled. 

Update
======
* Undo/Redo reworked and changed to comply with Unity 4.3


How To
======

TODO
====
* Camera Transition (I will create a complete repository for this as it makes more sense for it to be separate)
* Import/Export - going to change it such that you load/save track data.  This allows for the track to be re-usable across any project, but also allow for shared animation for separate animators.
* Add a way to make this tool extensive - will help with implementing unity2d, tk2d, etc.
* Fix Wonky window positioning and pop-up menu weirdness.  I doubt I want to touch this part of the code...yeah...
