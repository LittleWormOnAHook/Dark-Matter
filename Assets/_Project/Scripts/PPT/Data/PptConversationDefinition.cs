using System;
using UnityEngine;

namespace Project.PPT
{
    [Serializable]
    public class PptConversationChoice
    {
        [SerializeField] private string label;
        [SerializeField] private string nextNodeId;
        [SerializeField] private string[] logKeywordIds;

        public string Label => label;
        public string NextNodeId => nextNodeId;
        public string[] LogKeywordIds => logKeywordIds;
    }

    [Serializable]
    public class PptConversationNode
    {
        [SerializeField] private string nodeId;
        [TextArea(2, 4)]
        [SerializeField] private string speakerLine;
        [SerializeField] private PptConversationChoice[] choices;
        [SerializeField] private string[] logKeywordIds;

        public string NodeId => nodeId;
        public string SpeakerLine => speakerLine;
        public PptConversationChoice[] Choices => choices;
        public string[] LogKeywordIds => logKeywordIds;
    }

    [CreateAssetMenu(
        fileName = "ppt_conversation",
        menuName = "Dark Matter: Genesis/PPT/Conversation")]
    public class PptConversationDefinition : ScriptableObject
    {
        [SerializeField] private string conversationId;
        [SerializeField] private string startNodeId;
        [SerializeField] private PptConversationNode[] nodes;

        public string ConversationId => string.IsNullOrWhiteSpace(conversationId) ? name : conversationId;
        public string StartNodeId => startNodeId;
        public PptConversationNode[] Nodes => nodes;

        public bool TryGetNode(string nodeId, out PptConversationNode node)
        {
            node = null;
            if (string.IsNullOrWhiteSpace(nodeId) || nodes == null)
                return false;

            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && string.Equals(nodes[i].NodeId, nodeId, StringComparison.Ordinal))
                {
                    node = nodes[i];
                    return true;
                }
            }

            return false;
        }
    }
}
