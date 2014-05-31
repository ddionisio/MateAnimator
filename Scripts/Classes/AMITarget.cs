using UnityEngine;

public interface AMITarget {
	Transform TargetGetRoot();
	Transform TargetGetDataHolder();
	bool TargetIsMeta();
	void TargetMissing(string path, bool isMissing);
	Transform TargetGetCache(string path);
	void TargetSetCache(string path, Transform obj);
    void TargetSequenceComplete(AMSequence seq);
    void TargetSequenceTrigger(AMSequence seq, AMKey key, AMTriggerData trigDat);
}