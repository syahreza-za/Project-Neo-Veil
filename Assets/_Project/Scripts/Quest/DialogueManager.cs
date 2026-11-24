using System;
using UnityEngine;
using UnityEngine.InputSystem;
using NeoVeil.Player;

namespace NeoVeil.Quest
{
    /// <summary>
    /// Controls visual-novel style NPC dialogue playback, temporarily locking player movement
    /// and exposing events for both CombatHUDDebugOverlay and future custom UGUI dialogue boxes.
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Optional Reference (Auto-Found if Empty)")]
        [SerializeField] private PlayerMovementController playerMovement;

        private DialogueDataSO _activeDialogue;
        private int _currentLineIndex;
        private Action _onCompleteCallback;

        public bool IsDialogueOpen => _activeDialogue != null;
        public DialogueLine CurrentLine => IsDialogueOpen && _currentLineIndex < _activeDialogue.lines.Length
            ? _activeDialogue.lines[_currentLineIndex]
            : default;
        public int CurrentLineNumber => _currentLineIndex + 1;
        public int TotalLines => IsDialogueOpen && _activeDialogue.lines != null ? _activeDialogue.lines.Length : 0;

        public event Action<DialogueDataSO> OnDialogueStarted;
        public event Action<DialogueLine, int, int> OnLineChanged;
        public event Action<DialogueDataSO> OnDialogueEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovementController>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!IsDialogueOpen || Keyboard.current == null) return;

            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            {
                AdvanceLine();
            }
            else if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseDialogue(invokeCallback: true);
            }
        }

        public void StartDialogue(DialogueDataSO dialogue, Action onComplete = null)
        {
            if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _activeDialogue = dialogue;
            _currentLineIndex = 0;
            _onCompleteCallback = onComplete;

            if (playerMovement != null) playerMovement.SetMovementLocked(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            OnDialogueStarted?.Invoke(_activeDialogue);
            OnLineChanged?.Invoke(CurrentLine, CurrentLineNumber, TotalLines);
        }

        public void AdvanceLine()
        {
            if (!IsDialogueOpen) return;

            _currentLineIndex++;
            if (_currentLineIndex >= _activeDialogue.lines.Length)
            {
                CloseDialogue(invokeCallback: true);
                return;
            }

            OnLineChanged?.Invoke(CurrentLine, CurrentLineNumber, TotalLines);
        }

        public void CloseDialogue(bool invokeCallback = false)
        {
            if (!IsDialogueOpen) return;

            DialogueDataSO finished = _activeDialogue;
            Action callback = _onCompleteCallback;

            _activeDialogue = null;
            _currentLineIndex = 0;
            _onCompleteCallback = null;

            if (playerMovement != null) playerMovement.SetMovementLocked(false);

            OnDialogueEnded?.Invoke(finished);
            if (invokeCallback) callback?.Invoke();
        }
    }
}
