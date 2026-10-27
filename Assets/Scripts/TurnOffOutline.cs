using UnityEngine;

public class TurnOffOutline : MonoBehaviour
{
    private Outline currentOutline;

    void Start()
    {
        currentOutline = GetComponent<Outline>();
        currentOutline.enabled = false;
    }
}
