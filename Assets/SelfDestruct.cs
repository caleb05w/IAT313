using UnityEngine;

public class SelfDestruct : MonoBehaviour
{
    public void DestroyThis() => Destroy(gameObject);
    public void DestroyTarget(GameObject target) => Destroy(target);
}
