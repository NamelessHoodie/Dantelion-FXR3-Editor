using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFXR3Editor
{
    //This entire class is copied from DSParamStudio
    [Flags]
    public enum ActionEvent
    {
        NoEvent = 0,

        // An object was added or removed from a scene
        ObjectAddedRemoved = 1,
    }

    /// <summary>
    /// Interface for objects that may react to events caused by actions that
    /// happen. Useful for invalidating caches that various editors may have.
    /// </summary>
    public interface IActionEventHandler
    {
        public void OnActionEvent(ActionEvent evt);
    }

    /// <summary>
    /// Manages undo and redo for an editor context
    /// </summary>
    public class ActionManager
    {
        private List<IActionEventHandler> _eventHandlers = new List<IActionEventHandler>();

        private Stack<Action> _undoStack = new Stack<Action>();
        private Stack<Action> _redoStack = new Stack<Action>();

        public void AddEventHandler(IActionEventHandler handler)
        {
            _eventHandlers.Add(handler);
        }

        private void NotifyHandlers(ActionEvent evt)
        {
            if (evt == ActionEvent.NoEvent)
            {
                return;
            }
            foreach (var handler in _eventHandlers)
            {
                handler.OnActionEvent(evt);
            }
        }

        public void ExecuteAction(Action a)
        {
            NotifyHandlers(a.Execute());
            _undoStack.Push(a);
            _redoStack.Clear();
        }

        public Action PeekUndoAction()
        {
            if (_undoStack.Count() == 0)
            {
                return null;
            }
            return _undoStack.Peek();
        }

        public void UndoAction()
        {
            if (_undoStack.Count() == 0)
            {
                return;
            }
            var a = _undoStack.Pop();
            NotifyHandlers(a.Undo());
            _redoStack.Push(a);
        }

        public void RedoAction()
        {
            if (_redoStack.Count() == 0)
            {
                return;
            }
            var a = _redoStack.Pop();
            NotifyHandlers(a.Execute());
            _undoStack.Push(a);
        }

        public bool CanUndo()
        {
            return _undoStack.Count() > 0;
        }

        public bool CanRedo()
        {
            return _redoStack.Count() > 0;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
