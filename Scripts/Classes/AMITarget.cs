using UnityEngine;

public interface AMITarget {
	Transform TargetGetHolder();
	bool TargetIsMeta();
	object TargetGetCache(string path);
	void TargetSetCache(string path, object obj);
}