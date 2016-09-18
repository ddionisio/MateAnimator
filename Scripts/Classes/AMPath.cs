using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MateAnimator{
	public struct AMPath {
	    public Vector3[] path;
	    public int interp;			// interpolation
	    public int endFrame;		// ending frame
	    public int startIndex;		// starting key index
	    public int endIndex;		// ending key index

	    public AMPath(List<AMKey> keys, int _startIndex) {
	        // sort the keys by frame		
	        List<Vector3> _path = new List<Vector3>();
	        startIndex = _startIndex;
	        endIndex = startIndex;
	        endFrame = keys[startIndex].frame;

	        _path.Add((keys[startIndex] as AMTranslationKey).position);

	        // get path from startIndex until the next linear interpolation key (inclusive)
	        for(int i = startIndex + 1; i < keys.Count; i++) {
	            AMTranslationKey key = keys[i] as AMTranslationKey;
	            _path.Add(key.position);
	            endFrame = keys[i].frame;
	            endIndex = i;
	            if(!keys[startIndex].canTween
				   || key.interp != (int)AMTranslationKey.Interpolation.Curve) break;
	        }

	        interp = (keys[startIndex] as AMTranslationKey).interp;
	        path = _path.ToArray();
	    }
	}
}