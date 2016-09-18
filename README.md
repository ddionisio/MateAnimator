MateAnimator
============

Animator++

Based on: https://github.com/absameen/Animator_Timeline

Now remade to use DOTween: http://dotween.demigiant.com/index.php

This takes advantage of Sequence and the ability to cache all the tweens at startup. This allows the Animator to be used in critical parts of the game.

Also, Animator is no longer global and can be added to any game object.  You can now have many Sequences running simultaneously.

Mega Morph has been removed.

Import/Export, Code Generator are currently disabled. 

### Dependencies ###
* DOTween v1.1.310


Updates
=======
### Update 5 ###
* Converted to use DOTween
### Update 4 ###
* Unity 5 compatibility.
### Update 3 ###
* Added Rotation Euler.  This will allow you to create spin animations.  Includes exclusive rotation for x, y, or z (be careful of Gimbal-lock!).
* Added Material track.  Now you can animate any properties you specify in the shader used by the material.  This requires a GameObject with a Renderer component.  You can also use this to override the material.  For now there is no preview during editing.
* A bunch of refactoring to remove editor specific code in AnimatorData.  You will no longer see any editor API when using AnimatorData.
* Various tweaks to building sequences.
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
* Make sure to get DOTween and install to your project: http://dotween.demigiant.com/download.php
* Clone this project to your Assets folder (or Plugins if you are scripting in javascript).  If your project is already setup for Git, then clone this project as a submodule.
* Open your project, you should be able to see "Cutscene Editor" in the Window menu, or M8/Animator in the 'Add Component' droplist.

TODO
====
* Find a way to use serialization for tracks to remove the need for component.
* Dynamic frame scrolling and expand/shrink (get rid of the unnecessary configuration of total frames)
* Unify pathing of position track to all other tracks.
* Alternate Meta by using ID component (easier, less human error, but with a little bit more overhead)
* Allow typing of function for SendMessage event key.
* Import/Export - AnimatorMeta sort of does this already, but adding a JSON format wouldn't hurt.
* Add a way to make this tool extensive - will help with implementing tk2d, etc.
* Work with MechAnim. - allow change state, etc. and preview.
* Work with Particles - allow play/pause/stop and preview.
* Scene preview for Material property track.
* Any other glitches with mouse events.
* Camera transition fixes (too glitchy, maybe do something like UE4's director)

DEBUG
=====
Add "MATE\_DEBUG_ANIMATOR" (without quotes) in Player Settings -> Scripting Define Symbols.

For now it only shows a trace message whenever the GameObject "_animdata" is destroyed.

License
=======
* MateAnimator is licensed under a Creative Commons Attribution-NonCommercial 3.0 Unported License
Note: You may use the extension in commercial games and other Unity projects. This license applies to the source code of the extension only.
  - http://creativecommons.org/licenses/by/3.0/
