MateAnimator
============

Fixing up the open sourced Animator from Unity to be more useful.

Based on: https://github.com/absameen/Animator_Timeline

Now remade to use HOTween: http://www.holoville.com/hotween/index.html

This takes advantage of Sequence and the ability to cache all the tweens at startup. This allows the Animator to be used in critical parts of the game.

Also, Animator is no longer global and can be added to any game object.  You can now have many Sequences running simultaneously.

Camera Transition and Mega Morph has been removed

Import/Export, Code Generator doesn't work. JSON stuff mangled. 

Undo/Redo works as far as I've tested.

How To
======

TODO
====

Fix Orientation

Add a way to make this tool extensive

Fix Wonky window positioning and pop-up menu weirdness
