using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField]
    private bool m_InMouse;
    public bool InMouse { get { return m_InMouse; } }
    [SerializeField]
    private bool m_Grabbed;
    public bool Grabbed { get => m_Grabbed; set { m_Grabbed = value; } }

    private PointerPhysics m_Pointer;

    private void Awake()
    {
        m_Pointer = GameObject.FindAnyObjectByType<PointerPhysics>();
        if (m_Pointer == null)
            Debug.LogError("Interactable could not find pointer!");

    }

    private void OnMouseEnter()
    {
        Debug.Log($"OnMouseEnter {this.name}");
        m_InMouse=true;
    }

    private void OnMouseDown()
    {
        Debug.Log($"OnMouseDown {this.name}");
        m_Pointer.Grab(this);
    }

    private void OnMouseUp()
    {
        Debug.Log($"OnMouseUp {this.name}");
        m_Pointer.Release(this);
    }

    private void OnMouseExit()
    {
        Debug.Log($"OnMouseExit {this.name}");
        m_InMouse =false;
    }
}
