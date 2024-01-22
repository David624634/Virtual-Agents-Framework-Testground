using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;

namespace i5.VirtualAgents.Editor
{
    public class OpenAIDependencyMenu : MonoBehaviour
    {
        [MenuItem("Virtual Agents Framework/OpenAI/Add unofficial OpenAI Helper Package")]
        static void AddDependencies()
        {
            Client.Add("https://github.com/srcnalt/OpenAI-Unity.git");
        }

        [MenuItem("Virtual Agents Framework/OpenAI/Remove OpenAI Helper Package")]
        static void RemoveDependencies()
        {
            Client.Remove("com.srcnalt.openai-unity");
        }
        [MenuItem("Virtual Agents Framework/OpenAI/Link to OpenAI Helper Package")]
        static void Link()
        {
            Application.OpenURL("https://github.com/srcnalt/OpenAI-Unity");
        }
    }
}
