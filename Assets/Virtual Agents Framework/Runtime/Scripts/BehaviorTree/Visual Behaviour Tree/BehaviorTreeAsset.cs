using i5.VirtualAgents.AgentTasks;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace i5.VirtualAgents.BehaviourTrees.Visual
{
    /// <summary>
    /// Asset that can be used to create behaviour trees that are saved persistently. The tree is not executable, but an executable abstract copy can be retrived.
    /// </summary>
    [CreateAssetMenu(menuName = "Virtual Agents Framework/Behaviour Tree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        [SerializeField]
        private VisualNode rootNode;
        public List<VisualNode> Nodes = new List<VisualNode>();
        public event Action CreatedAndNamed;

#if UNITY_EDITOR
        private void OnEnable()
        {
            AddRoot();
        }

        /// <summary>
        /// Adds a new node based on an serializable task
        /// </summary>
        /// <param name="baseTask"></param>
        /// <returns></returns>
        public VisualNode AddNode(ISerializable baseTask)
        {
            VisualNode node = CreateInstance<VisualNode>();
            node.name = baseTask.GetType().Name;
            node.Guid = GUID.Generate().ToString();
            node.SetSerializedType(baseTask);
            Nodes.Add(node);
            AssetDatabase.AddObjectToAsset(node, this);
            return node;
        }

        // Adds a root node as long as non exists
        private void AddRoot()
        {
            if (rootNode == null)
            {
                if (AssetDatabase.Contains(this))
                {
                    rootNode = AddNode(new RootNode());
                }
                else
                {
                    // The tree is still being named and therefore not permanently saved. Delay the creation of the root node until the tree is properly created.
                    EditorApplication.update += AddRootDelayed;
                }

            }
        }

        // Adds the root once the tree is part of the asset database (i.e. once it is named)
        private void AddRootDelayed()
        {
            if (AssetDatabase.Contains(this))
            {
                EditorApplication.update -= AddRootDelayed;
                rootNode = AddNode(new RootNode());
                CreatedAndNamed?.Invoke();
            }
        }

        /// <summary>
        /// Deletes the given node from the tree
        /// </summary>
        /// <param name="nodeToDelete"></param>
        public void DeleteNode(VisualNode nodeToDelete)
        {
            Nodes.Remove(nodeToDelete);
            foreach (var node in Nodes)
            {
                node.Children.Remove(nodeToDelete);
            }
            AssetDatabase.RemoveObjectFromAsset(nodeToDelete);
        }
#endif

        /// <summary>
        /// Generates an abstract copy of the tree that is executable through the root nodes FullUpdate() function
        /// </summary>
        /// <returns> </returns>
        public ITask GetExecutableTree(NodesOverwriteData nodesOverwriteData = null)
        {
            rootNode = Nodes[0];
            SerializationDataContainer rootNodeData = null;
            if (nodesOverwriteData != null && nodesOverwriteData.KeyExists(rootNode.Guid))
            {
                rootNodeData = nodesOverwriteData.Get(rootNode.Guid);
            }
            ITask root = (ITask)rootNode.GetCopyOfSerializedInterface(rootNodeData);
            ConnectAbstractTree(rootNode, root, nodesOverwriteData);
            return root;
        }

        // Recursively generates the abstract childs for the given graphical node and connects them
        private void ConnectAbstractTree(VisualNode node, ITask abstractNode, NodesOverwriteData nodesOverwriteData)
        {
            // Sort the children by there height in order to execute higher children first
            node.Children.Sort((node1, node2) => { if (node1.Position.y > node2.Position.y) { return 1; } else if (node1.Position.y < node2.Position.y) { return -1; } else return 0; });

            foreach (var child in node.Children)
            {
                SerializationDataContainer nodeData = null;
                if (nodesOverwriteData != null && nodesOverwriteData.KeyExists(child.Guid))
                {
                    nodeData = nodesOverwriteData.Get(child.Guid);
                }
                ITask abstractChild = (ITask)child.GetCopyOfSerializedInterface(nodeData);
                if (abstractNode is ICompositeNode)
                {
                    (abstractNode as ICompositeNode).Children.Add(abstractChild);
                }
                if (abstractNode is IDecoratorNode)
                {
                    (abstractNode as IDecoratorNode).Child = abstractChild;
                }
                ConnectAbstractTree(child, abstractChild, nodesOverwriteData);
            }
        }
    }
}