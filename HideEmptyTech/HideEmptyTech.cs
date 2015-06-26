using System;
using System.Text;
using UnityEngine;

namespace HideEmptyTech
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class HideEmptyTechAddon : MonoBehaviour
    {
        public void Awake()
        {
            Debug.Log("Awake");
            RDTechTree.OnTechTreeSpawn.Add(new EventData<RDTechTree>.OnEvent(onTechTreeSpawn));
        }

        public void Start()
        {
            Debug.Log("Start()");
            DontDestroyOnLoad(this);
        }

        public void onTechTreeSpawn(RDTechTree techTree)
        {
            if(techTree != null)
            {
                hideNodes(techTree);
            }
        }

        public void hideNodes(RDTechTree techTree)
        {
            foreach(RDNode node in techTree.controller.nodes)
            {
                Debug.Log("Node " + node.name + " " + node.PartsInTotal());
                node.gameObject.SetActive(node.PartsInTotal() > 0);
            }
        }
    }
}
