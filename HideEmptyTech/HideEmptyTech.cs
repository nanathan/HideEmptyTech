/* This is free and unencumbered software released into the public domain. */

using System;
using System.Collections.Generic;
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
            TechTreeWrapper tree = new TechTreeWrapper(techTree);

            tree.visit(node => {
                Debug.Log("Node " + node.name + " " + node.PartsInTotal());
                node.gameObject.SetActive(node.PartsInTotal() > 0);
            });

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
}
