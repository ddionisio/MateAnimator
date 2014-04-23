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
* Keyboard shortcuts: arrowkeys to select keys/tracks, undo/redo/duplicate for keys, copy/paste for keys, delete to delete selected keys.
* Shared Animator (AnimatorMeta) allows you to share animation per AnimatorData.  This helps with updating any shared animation reliably, and reduces overhead.
* Sprite animation support.  You can also drag sprites to the Animator window to automatically add the track/keys.


Installation
============
* Make sure to get HOTween and install to your project: http://hotween.demigiant.com/download.html
* Clone this project to your Assets folder (or Plugins if you are scripting in javascript).  If your project is already setup for Git, then clone this project as a submodule.
* Open your project, you should be able to see Animator in the Windows menu, or M8/Animator in the 'Add Component' droplist.

TODO
====
* Camera Transition - soon!
* Import/Export - AnimatorMeta sort of does this already, but adding a JSON format wouldn't hurt.
* Add a way to make this tool extensive - will help with implementing tk2d, etc.
* Work with MechAnim. - allow change state, etc. and preview.
* Work with Particles - allow play/pause/stop and preview.
* Triggers.
