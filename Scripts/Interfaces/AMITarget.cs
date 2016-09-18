using UnityEngine;
using System.Collections.Generic;

namespace M8.Animator {
	public interface AMITarget {
	    float animScale { get; }

	    Transform root { get; }

	    Transform holder { get; }

	    List<AMTakeData> takes { get; }

	    bool isMeta { get; }

		Transform GetCache(string path);
		void SetCache(string path, Transform obj);
	    void SequenceComplete(AMSequence seq);
	    void SequenceTrigger(AMSequence seq, AMKey key, AMTriggerData trigDat);

	    string[] GetMissingTargets();
	    void MaintainTargetCache(AMTrack track);
	    void MaintainTakes();
	    void GenerateMissingTargets(string[] missingPaths);
	}
}