using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace M8.Animator {
	public struct AMPath {
	    public Vector3[] path;
	    public int interp;			// interpolation
	    public int endFrame;		// ending frame
	    public int startIndex;		// starting key index
	    public int endIndex;		// ending key index

        public static AMPath GenerateCurve(List<AMKey> keys, int _startIndex) {
            AMPath newPath = new AMPath();

            // sort the keys by frame		
            List<Vector3> _path = new List<Vector3>();
            newPath.startIndex = _startIndex;
            newPath.endIndex = _startIndex;
            newPath.endFrame = keys[_startIndex].frame;

            _path.Add((keys[_startIndex] as AMTranslationKey).position);

            // get path from startIndex until the next linear interpolation key (inclusive)
            for(int i = _startIndex + 1; i < keys.Count; i++) {
                AMTranslationKey key = keys[i] as AMTranslationKey;
                _path.Add(key.position);
                newPath.endFrame = keys[i].frame;
                newPath.endIndex = i;
                if(!keys[_startIndex].canTween
                   || key.interp != (int)AMTranslationKey.Interpolation.Curve) break;
            }

            newPath.interp = (int)AMTranslationKey.Interpolation.Curve;
            newPath.path = _path.ToArray();

            return newPath;
        }

        public static AMPath GenerateLinear(List<AMKey> keys, int _startIndex) {
            AMPath newPath = new AMPath();

            // sort the keys by frame		
            List<Vector3> _path = new List<Vector3>();
            newPath.startIndex = _startIndex;
            newPath.endIndex = _startIndex;
            newPath.endFrame = keys[_startIndex].frame;

            _path.Add((keys[_startIndex] as AMTranslationKey).position);

            int nextIndex = _startIndex + 1;
            if(nextIndex < keys.Count) {
                AMTranslationKey key = keys[nextIndex] as AMTranslationKey;
                _path.Add(key.position);
                newPath.endFrame = keys[nextIndex].frame;
                newPath.endIndex = nextIndex;
            }

            newPath.interp = (int)AMTranslationKey.Interpolation.Linear;
            newPath.path = _path.ToArray();

            return newPath;
        }

        public static AMPath GenerateSingle(AMKey key, int _startIndex) {
            AMPath newPath = new AMPath();

            // sort the keys by frame
            newPath.startIndex = _startIndex;
            newPath.endIndex = _startIndex;
            newPath.endFrame = key.frame;
            newPath.path = new Vector3[] { (key as AMTranslationKey).position };
            newPath.interp = (int)AMTranslationKey.Interpolation.None;
            
            return newPath;
        }
    }
}