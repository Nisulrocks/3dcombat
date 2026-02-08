using UnityEngine;




[System.Serializable]
public class RespawnPoint
{
    [SerializeField] private string pointName;
    [SerializeField] private Transform transform;
    [SerializeField] private bool isDefault = false;

    public string PointName => pointName;
    public Transform Transform => transform;
    public bool IsDefault => isDefault;

    public RespawnPoint(string name, Transform transform, bool isDefault = false)
    {
        this.pointName = name;
        this.transform = transform;
        this.isDefault = isDefault;
    }

    public Vector3 Position => transform != null ? transform.position : Vector3.zero;
    public Quaternion Rotation => transform != null ? transform.rotation : Quaternion.identity;
}
