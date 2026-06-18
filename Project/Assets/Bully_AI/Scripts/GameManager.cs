using NUnit.Framework;
using System.Collections.Generic;
using System.Drawing.Text;
using Unity.MLAgentsExamples;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<GroundContact> agentGroundContactList;
    [Header("Agent Settings")]
    public bool resetAgentOnGroundContact = false;
    

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
