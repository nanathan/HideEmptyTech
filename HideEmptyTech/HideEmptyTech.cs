/* This is free and unencumbered software released into the public domain. */

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace HideEmptyTech
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class HideEmptyTechAddon : MonoBehaviour
    {
        private State state = State.PATHS_TO_PARTS;
        private RDTechTree techTree;
        
        public enum State
        {
            // Only show nodes that have parts (can leave un-reachable nodes)
            WITH_PARTS_ONLY,
            // Show nodes with parts, and also make sure those nodes are reachable
            PATHS_TO_PARTS,
            // Show all nodes
            ALL
        }

        public void Awake()
        {
            Log.log("Awake");
            RDTechTree.OnTechTreeSpawn.Add(new EventData<RDTechTree>.OnEvent(onTechTreeSpawn));
            RDTechTree.OnTechTreeDespawn.Add(new EventData<RDTechTree>.OnEvent(onTechTreeDespawn));
        }

        public void Start()
        {
            Log.log("Start()");
            DontDestroyOnLoad(this);
        }

        public void OnGUI()
        {
            if(techTree != null)
            {
                string title = state == State.WITH_PARTS_ONLY ? "filtered" : state == State.PATHS_TO_PARTS ? "default" : "all";
                if(GUI.Button(new Rect(50, 50, 90, 30), title))
                {
                    switch(state)
                    {
                        case State.WITH_PARTS_ONLY:
                            setState(State.PATHS_TO_PARTS);
                            break;
                        case State.PATHS_TO_PARTS:
                            setState(State.ALL);
                            break;
                        case State.ALL:
                            setState(State.WITH_PARTS_ONLY);
                            break;
                    }
                }
            }
        }

        public void onTechTreeSpawn(RDTechTree techTree)
        {
            this.techTree = techTree;
            updateVisibility();
        }

        public void onTechTreeDespawn(RDTechTree techTree)
        {
            this.techTree = null;
        }

        public void setState(State state)
        {
            Log.log("setState " + state);
            this.state = state;
            techTree.SpawnTechTreeNodes();
        }

        void updateVisibility()
        {
            Log.log("updateVisibility");
            TechTreeWrapper tree = new TechTreeWrapper(techTree);

            tree.visit(node => {
                Log.log("Node " + node.name + " " + node.PartsInTotal() + " " + ResearchAndDevelopment.GetTechnologyState(node.tech.techID).ToString());
                bool purchased = ResearchAndDevelopment.GetTechnologyState(node.tech.techID) == RDTech.State.Available;
                node.gameObject.SetActive(state == State.ALL || purchased || node.PartsInTotal() > 0);
            });

            if(state != State.PATHS_TO_PARTS)
            {
                return;
            }

            int testingLoopCount = 0;
            int progress;
            do
            {
                testingLoopCount += 1;
                progress = 0;

                tree.visit(node => {
                    if(!node.gameObject.activeSelf)
                    {
                        foreach(string childName in tree.getChildren(node.name))
                        {
                            RDNode child = tree.getNode(childName);
                            if(child.gameObject.activeSelf)
                            {
                                node.gameObject.SetActive(true);
                                progress += 1;
                                break;
                            }
                        }
                    }
                });

            } while(progress > 0 && testingLoopCount < 20);
        }
    }

    public delegate void Visitor(RDNode node);

    public class TechTreeWrapper
    {
        RDTechTree tree;
        // children are not actually stored within the tree that I can find (the "children" property is not populated)
        IDictionary<string, HashSet<string>> allChildren = new Dictionary<string, HashSet<string>>();
        IDictionary<string, RDNode> allNodes = new Dictionary<string, RDNode>();
        HashSet<string> empty = new HashSet<string>();

        public TechTreeWrapper(RDTechTree tree)
        {
            this.tree = tree;
            // Pre-compute useful traversal information
            visit(node => {
                allNodes.Add(node.name, node);
                foreach(RDNode.Parent parent in node.parents)
                {
                    addChild(parent.parent.node.name, node.name);
                }
            });
        }

        public void visit(Visitor visit)
        {
            foreach(RDNode node in tree.controller.nodes)
            {
                visit(node);
            }
        }

        public RDNode getNode(string name)
        {
            return allNodes[name];
        }

        public HashSet<string> getChildren(string parentName)
        {
            return allChildren.ContainsKey(parentName) ? allChildren[parentName] : empty;
        }

        private void addChild(string parent, string child)
        {
            HashSet<string> children;
            if(!allChildren.TryGetValue(parent, out children))
            {
                allChildren.Add(parent, children = new HashSet<string>());
            }
            children.Add(child);
        }
    }

    public class Log
    {
        public static void log(object message)
        {
#if DEBUG
            Debug.Log(message);
#endif
        }
    }
}
