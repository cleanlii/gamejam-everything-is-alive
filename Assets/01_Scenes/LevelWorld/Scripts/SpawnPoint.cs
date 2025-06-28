using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public string itemIndex;

    public Vector3 GetPosition()
    {
        // return transform.position;
        return GetComponent<RectTransform>().anchoredPosition;
    }
}