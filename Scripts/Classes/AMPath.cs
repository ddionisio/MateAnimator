using UnityEngine;
using System.Collections;

// AMPath: holds a path and interpolation type
[System.Serializable]
public class AMPath {
	public Vector3[] path;
	public int interp;			// interpolation
	public int startFrame;		// starting frame
	public int endFrame;		// ending frame
	public int startIndex;		// starting key index
	public int endIndex;		// ending key index
	
	public AMPath() {
		
	}
	public AMPath(Vector3[] _path, int _interp, int _startFrame, int _endFrame) {
		path = _path;
		interp = _interp;
		startFrame = _startFrame;
		endFrame = _endFrame;
	}
	public AMPath(Vector3[] _path, int _interp, int _startFrame, int _endFrame, int _startIndex, int _endIndex) {
		path = _path;
		interp = _interp;
		startFrame = _startFrame;
		endFrame = _endFrame;
		startIndex = _startIndex;
		endIndex = _endIndex;
	}
	// number of frames
	public int getNumberOfFrames() {
		return endFrame-startFrame;
	}
}
