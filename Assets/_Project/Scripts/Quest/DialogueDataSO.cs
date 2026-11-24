using System;
using UnityEngine;

namespace NeoVeil.Quest
{
    [Serializable]
    public struct DialogueLine
    {
        public string speakerName;
        public Color speakerColor;
        [TextArea(2, 4)] public string text;
    }

    /// <summary>
    /// Data-driven anime dialogue sequence for NPCs in District 07 and Cafe 07 Hub.
    /// </summary>
    [CreateAssetMenu(fileName = "DialogueData", menuName = "NeoVeil/Quest/Dialogue Data")]
    public class DialogueDataSO : ScriptableObject
    {
        public string dialogueId = "npc_cafe_intro";
        public DialogueLine[] lines;

        public static DialogueDataSO CreateRuntimeSample(string id, string npcName, Color nameColor, params string[] texts)
        {
            var data = CreateInstance<DialogueDataSO>();
            data.dialogueId = id;
            data.lines = new DialogueLine[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                data.lines[i] = new DialogueLine
                {
                    speakerName = npcName,
                    speakerColor = nameColor,
                    text = texts[i]
                };
            }
            return data;
        }
    }
}
