using System;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action AgentFell;

    [SerializeField]
    private List<GroundContact> agentGroundContactList;
    [Header("Agent Settings")]
    public bool resetAgentOnGroundContact = false;

    [Header("Fall Detection")]
    [Tooltip("How long a torso/head contact must persist before it counts as a fall.")]
    [Min(0f)] public float fallConfirmationSeconds = 0.2f;

    public bool IsAgentFallen { get; private set; }

    float fallStartedAt = float.NegativeInfinity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (agentGroundContactList != null && agentGroundContactList.Count > 0)
        {
            foreach (var agentGroundContact in agentGroundContactList)
            {
                if (agentGroundContact != null)
                    agentGroundContact.agentDoneOnGroundContact = resetAgentOnGroundContact;
            }
        }
    }

    void Update()
    {
        bool fallenContact = HasFallenBodyContact();
        if (!fallenContact)
        {
            fallStartedAt = float.NegativeInfinity;
            IsAgentFallen = false;
            return;
        }

        if (float.IsNegativeInfinity(fallStartedAt))
            fallStartedAt = Time.time;

        if (!IsAgentFallen && Time.time - fallStartedAt >= fallConfirmationSeconds)
        {
            IsAgentFallen = true;
            AgentFell?.Invoke();
        }
    }

    public void ResetFallState()
    {
        fallStartedAt = float.NegativeInfinity;
        IsAgentFallen = false;
    }

    bool HasFallenBodyContact()
    {
        if (agentGroundContactList == null)
            return false;

        foreach (GroundContact contact in agentGroundContactList)
        {
            if (contact == null || !contact.touchingGround)
                continue;

            string partName = contact.name.ToLowerInvariant();
            if (partName.Contains("hips") || partName.Contains("spine") ||
                partName.Contains("chest") || partName.Contains("head"))
            {
                return true;
            }
        }

        return false;
    }
}
