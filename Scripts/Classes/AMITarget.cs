using UnityEngine;

public interface AMITarget {
	Transform TargetGetRoot();
	Transform TargetGetDataHolder();
	bool TargetIsMeta();
	object TargetGetCache(string path);
	void TargetSetCache(string path, object obj);
}