using System;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public static event Action<Interactable> InteractionStarted;
    public static event Action<Interactable> InteractionEnded;
    public static event Action<Interactable> AgentInterrupted;

    [SerializeField]
    private bool m_InMouse;
    public bool InMouse { get { return m_InMouse; } }
    [SerializeField]
    private bool m_Grabbed;
    public bool Grabbed
    {
        get => m_Grabbed;
        set
        {
            if (m_Grabbed == value)
                return;

            m_Grabbed = value;
            lastOperatedAt = Time.time;
            if (m_Grabbed)
                InteractionStarted?.Invoke(this);
            else
                InteractionEnded?.Invoke(this);
        }
    }

    [Tooltip("A recently released object still counts as player-operated for this long.")]
    [Min(0f)] public float operatedCollisionGraceSeconds = 5f;

    private PointerPhysics m_Pointer;
    float lastOperatedAt = float.NegativeInfinity;

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
        if (m_Pointer != null)
            m_Pointer.Grab(this);
    }

    private void OnMouseUp()
    {
        Debug.Log($"OnMouseUp {this.name}");
        if (m_Pointer != null)
            m_Pointer.Release(this);
    }

    private void OnMouseExit()
    {
        Debug.Log($"OnMouseExit {this.name}");
        m_InMouse =false;
    }

    void OnDisable()
    {
        if (!m_Grabbed)
            return;

        m_Grabbed = false;
        InteractionEnded?.Invoke(this);
    }

    void Update()
    {
        if (m_Grabbed)
            lastOperatedAt = Time.time;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.transform.CompareTag("agent"))
            return;

        bool playerOperated = m_Grabbed ||
            Time.time - lastOperatedAt <= operatedCollisionGraceSeconds;
        if (playerOperated)
            AgentInterrupted?.Invoke(this);
    }
}
