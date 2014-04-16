using UnityEngine;

public interface AMITarget {
	Transform TargetGetRoot();
	Transform TargetGetDataHolder();
	bool TargetIsMeta();
	void TargetMissing(string path, bool isMissing);
	object TargetGetCache(string path);
	void TargetSetCache(string path, object obj);
}