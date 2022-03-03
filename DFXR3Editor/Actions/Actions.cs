using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using SoulsFormats;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Numerics;
using DFXR3Editor.Dependencies;

namespace DFXR3Editor
{
    /// <summary>
    /// An action that can be performed by the user in the editor
    /// these actions get pushed to a stack for undo/redo
    /// </summary>
    public abstract class Action
    {
        abstract public ActionEvent Execute();
        abstract public ActionEvent Undo();
    }

    public class EditPublicCPickerVector4 : Action
    {
        private Vector4 _newVector;
        private Vector4 _oldVector;

        public EditPublicCPickerVector4(Vector4 newVector)
        {
            _oldVector = MainUserInterface.CPicker;
            this._newVector = newVector;
        }

        public override ActionEvent Execute()
        {
            MainUserInterface.CPicker = _newVector;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            MainUserInterface.CPicker = _oldVector;
            return ActionEvent.NoEvent;
        }
    }
    public class ModifyXAttributeInt : Action
    {
        private XAttribute _editedAttribute;
        private string _oldValue;
        private string _newValue;

        public ModifyXAttributeInt(XAttribute attributeToEdit, int newValue)
        {
            this._editedAttribute = attributeToEdit;
            this._oldValue = attributeToEdit.Value;
            this._newValue = newValue.ToString();
        }

        public override ActionEvent Execute()
        {
            _editedAttribute.Value = _newValue;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            _editedAttribute.Value = _oldValue;
            return ActionEvent.NoEvent;
        }
    }
    public class ModifyXAttributeFloat : Action
    {
        private XAttribute _editedAttribute;
        private string _oldValue;
        private string _newValue;

        public ModifyXAttributeFloat(XAttribute attributeToEdit, float newValue)
        {
            this._editedAttribute = attributeToEdit;
            this._oldValue = attributeToEdit.Value;
            this._newValue = newValue.ToString("0.####");
        }

        public override ActionEvent Execute()
        {
            _editedAttribute.Value = _newValue;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            _editedAttribute.Value = _oldValue;
            return ActionEvent.NoEvent;
        }
    }
    public class ModifyXAttributeString : Action
    {
        private XAttribute _editedAttribute;
        private string _oldValue;
        private string _newValue;

        public ModifyXAttributeString(XAttribute attributeToEdit, string newValue)
        {
            this._editedAttribute = attributeToEdit;
            this._oldValue = attributeToEdit.Value;
            this._newValue = newValue;
        }

        public override ActionEvent Execute()
        {
            _editedAttribute.Value = _newValue;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            _editedAttribute.Value = _oldValue;
            return ActionEvent.NoEvent;
        }
    }
    public class XElementReplaceChildren : Action
    {
        private XElement _objXElement;
        private XElement _originalXElement;
        private XElement _newXElement;

        public XElementReplaceChildren(XElement node, XElement newXElement)
        {
            this._objXElement = node;
            this._originalXElement = new XElement(node);
            this._newXElement = new XElement(newXElement);
        }

        public override ActionEvent Execute()
        {
            if (_objXElement != null)
            {
                _objXElement.RemoveNodes();
                _objXElement.Add(_newXElement.Elements());
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (_objXElement != null)
            {
                _objXElement.RemoveNodes();
                _objXElement.Add(_originalXElement.Elements());
            }
            return ActionEvent.NoEvent;
        }
    }
    public class XElementReplaceChildrenWithSnapshot : Action
    {
        private XElement _objXElement;
        private XElement _originalXElement;
        private XElement _newXElement;
        private bool _skipFirstDo = false;

        public XElementReplaceChildrenWithSnapshot(XElement node, XElement oldXelement)
        {
            this._objXElement = node;
            this._originalXElement = new XElement(oldXelement);
            this._newXElement = new XElement(node);
        }

        public override ActionEvent Execute()
        {
            if (_objXElement != null)
            {
                if (_skipFirstDo)
                {
                    _objXElement.RemoveNodes();
                    _objXElement.Add(_newXElement.Elements());
                }
                else
                {
                    _skipFirstDo = true;
                }
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (_objXElement != null)
            {
                _objXElement.RemoveNodes();
                _objXElement.Add(_originalXElement.Elements());
            }
            return ActionEvent.NoEvent;
        }
    }
    public class ResetEditorSelection : Action
    {
        Ffxui _ui;
        public ResetEditorSelection(Ffxui ffxRelevant)
        {
            _ui = ffxRelevant;
        }

        public override ActionEvent Execute()
        {
            MainUserInterface.ResetEditorSelection(_ui);
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            MainUserInterface.ResetEditorSelection(_ui);
            return ActionEvent.NoEvent;
        }
    }
    public class XElementRemove : Action
    {
        private XElement _objXElement;
        private XElement _xParent;
        private XElement _originalXElement;
        private int _indexInParent;

        public XElementRemove(XElement nodeToRemove)
        {
            this._objXElement = nodeToRemove;
            this._originalXElement = new XElement(nodeToRemove);
            this._xParent = nodeToRemove.Parent;
            _indexInParent = FfxHelperMethods.GetNodeIndexinParent(nodeToRemove);
        }

        public override ActionEvent Execute()
        {
            if (_objXElement != null)
            {
                    _objXElement.Remove();
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (_objXElement != null)
            {
                if (_indexInParent > 0)
                {
                    _xParent.Elements().ElementAt(_indexInParent - 1).AddAfterSelf(_originalXElement);
                    _objXElement = _xParent.Elements().ElementAt(_indexInParent);
                }
                else
                {
                    _xParent.AddFirst(_originalXElement);
                    _objXElement = _xParent.Elements().First();
                }

            }
            return ActionEvent.NoEvent;
        }
    }
    public class XElementAdd : Action
    {
        private XElement _objXElement;
        private XElement _newChild;

        public XElementAdd(XElement node, XElement newChild)
        {
            this._objXElement = node;
            this._newChild = new XElement(newChild);
        }

        public override ActionEvent Execute()
        {
            if (_objXElement != null)
            {
                _objXElement.AddAfterSelf(_newChild);
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (_objXElement != null)
            {
                _newChild.Remove();
            }
            return ActionEvent.NoEvent;
        }
    }
    public class CompoundAction : Action
    {
        private List<Action> _actions;

        private Action<bool> _postExecutionAction = null;

        public CompoundAction(List<Action> actions)
        {
            _actions = actions;
        }

        public void SetPostExecutionAction(Action<bool> action)
        {
            _postExecutionAction = action;
        }

        public override ActionEvent Execute()
        {
            var evt = ActionEvent.NoEvent;
            foreach (var act in _actions)
            {
                if (act != null)
                {
                    evt |= act.Execute();
                }
            }
            if (_postExecutionAction != null)
            {
                _postExecutionAction.Invoke(false);
            }
            return evt;
        }

        public override ActionEvent Undo()
        {
            var evt = ActionEvent.NoEvent;
            foreach (var act in _actions)
            {
                if (act != null)
                {
                    evt |= act.Undo();
                }
            }
            if (_postExecutionAction != null)
            {
                _postExecutionAction.Invoke(true);
            }
            return evt;
        }
    }
}