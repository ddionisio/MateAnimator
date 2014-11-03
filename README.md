MateAnimator
============

Animator++

Based on: https://github.com/absameen/Animator_Timeline

Now remade to use HOTween: http://www.holoville.com/hotween/index.html

This takes advantage of Sequence and the ability to cache all the tweens at startup. This allows the Animator to be used in critical parts of the game.

Also, Animator is no longer global and can be added to any game object.  You can now have many Sequences running simultaneously.

Mega Morph has been removed.

Import/Export, Code Generator are currently disabled. 

###Dependencies###
* HOTween v1.3.380


Updates
=======
### Update 2 ###
* Added Mate Animator track.  Now you can play animations in your animation, while you animate.  Useful for choreography and ensuring timing between multiple animations on the scene.  Note: Currently doesn't execute event and trigger tracks.
* Fixes with null exceptions here and there.
### Update 1 ###
* Undo/Redo reworked and changed to comply with Unity 4.3
* Keyboard shortcuts: arrowkeys to select keys/tracks, copy/paste/duplicate for keys, delete to delete selected keys.
* Shared Animator (AnimatorMeta) allows you to share animation per AnimatorData.  This helps with updating any shared animation reliably, and reduces overhead.
* Sprite animation support.  You can also drag sprites to the Animator window to automatically add the track/keys.
* Camera Transition added back.


Installation
============
* Make sure to get HOTween and install to your project: http://hotween.demigiant.com/download.html
* Clone this project to your Assets folder (or Plugins if you are scripting in javascript).  If your project is already setup for Git, then clone this project as a submodule.
* Open your project, you should be able to see "Cutscene Editor" in the Window menu, or M8/Animator in the 'Add Component' droplist.

TODO
====
* Import/Export - AnimatorMeta sort of does this already, but adding a JSON format wouldn't hurt.
* Add a way to make this tool extensive - will help with implementing tk2d, etc.
* Work with MechAnim. - allow change state, etc. and preview.
* Work with Particles - allow play/pause/stop and preview.
* Preserve ease type when moving keys around.
* Reverse selected keys feature.
* Material property track - for animating shader variables.
* Some glitches with mouse events.
* Refactor the entire UI code...(might as well create a new project)
* Camera transition fixes (too glitchy, maybe do something like UE4's director)

License
=======
* MateAnimator is licensed under a Creative Commons Attribution-NonCommercial 3.0 Unported License
Note: You may use the extension in commercial games and other Unity projects. This license applies to the source code of the extension only.
  - http://creativecommons.org/licenses/by/3.0/
