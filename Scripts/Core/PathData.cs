using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace M8.Animator {
    public struct PathData {
        public Vector3[] path;
        public Key.Interpolation interp;          // interpolation
        public int endFrame;        // ending frame
        public int startIndex;      // starting key index
        public int endIndex;		// ending key index

        public static PathData GenerateCurve(List<Key> keys, int _startIndex) {
            PathData newPath = new PathData();

            // sort the keys by frame		
            List<Vector3> _path = new List<Vector3>();
            newPath.startIndex = _startIndex;
            newPath.endIndex = _startIndex;
            newPath.endFrame = keys[_startIndex].frame;

            _path.Add((keys[_startIndex] as TranslationKey).position);

            // get path from startIndex until the next linear interpolation key (inclusive)
            for(int i = _startIndex + 1; i < keys.Count; i++) {
                TranslationKey key = keys[i] as TranslationKey;
                _path.Add(key.position);
                newPath.endFrame = keys[i].frame;
                newPath.endIndex = i;
                if(!keys[_startIndex].canTween
                   || key.interp != Key.Interpolation.Curve) break;
            }

            newPath.interp = Key.Interpolation.Curve;
            newPath.path = _path.ToArray();

            return newPath;
        }

        public static PathData GenerateLinear(List<Key> keys, int _startIndex) {
            PathData newPath = new PathData();

            // sort the keys by frame		
            List<Vector3> _path = new List<Vector3>();
            newPath.startIndex = _startIndex;
            newPath.endIndex = _startIndex;
            newPath.endFrame = keys[_startIndex].frame;

            _path.Add((keys[_startIndex] as TranslationKey).position);

            int nextIndex = _startIndex + 1;
            if(nextIndex < keys.Count) {
                TranslationKey key = keys[nextIndex] as TranslationKey;
                _path.Add(key.position);
                newPath.endFrame = keys[nextIndex].frame;
                newPath.endIndex = nextIndex;
            }

            newPath.interp = Key.Interpolation.Linear;
            newPath.path = _path.ToArray();

            return newPath;
        }

        public static PathData GenerateSingle(Key key, int _startIndex) {
            PathData newPath = new PathData();

            // sort the keys by frame
            newPath.startIndex = _startIndex;
            newPath.endIndex = _startIndex;
            newPath.endFrame = key.frame;
            newPath.path = new Vector3[] { (key as TranslationKey).position };
            newPath.interp = Key.Interpolation.None;

            return newPath;
        }
    }
}