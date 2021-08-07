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
    /// An action that can be performed by the user in the editor that represents
    /// a single atomic editor action that affects the state of the map. Each action
    /// should have enough information to apply the action AND undo the action, as
    /// these actions get pushed to a stack for undo/redo
    /// </summary>
    public abstract class Action
    {
        abstract public ActionEvent Execute();
        abstract public ActionEvent Undo();
    }

    public class EditPublicCPickerVector4 : Action
    {
        private Vector4 newVector;
        private Vector4 oldVector;

        public EditPublicCPickerVector4(Vector4 newVector)
        {
            oldVector = MainUserInterface._cPicker;
            this.newVector = newVector;
        }

        public override ActionEvent Execute()
        {
            MainUserInterface._cPicker = newVector;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            MainUserInterface._cPicker = oldVector;
            return ActionEvent.NoEvent;
        }
    }
    public class ModifyXAttributeInt : Action
    {
        private XAttribute editedAttribute;
        private string oldValue;
        private string newValue;

        public ModifyXAttributeInt(XAttribute attributeToEdit, int newValue)
        {
            this.editedAttribute = attributeToEdit;
            this.oldValue = attributeToEdit.Value;
            this.newValue = newValue.ToString();
        }

        public override ActionEvent Execute()
        {
            editedAttribute.Value = newValue;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            editedAttribute.Value = oldValue;
            return ActionEvent.NoEvent;
        }
    }
    public class ModifyXAttributeFloat : Action
    {
        private XAttribute editedAttribute;
        private string oldValue;
        private string newValue;

        public ModifyXAttributeFloat(XAttribute attributeToEdit, float newValue)
        {
            this.editedAttribute = attributeToEdit;
            this.oldValue = attributeToEdit.Value;
            this.newValue = newValue.ToString("0.####");
        }

        public override ActionEvent Execute()
        {
            editedAttribute.Value = newValue;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            editedAttribute.Value = oldValue;
            return ActionEvent.NoEvent;
        }
    }
    public class ModifyXAttributeString : Action
    {
        private XAttribute editedAttribute;
        private string oldValue;
        private string newValue;

        public ModifyXAttributeString(XAttribute attributeToEdit, string newValue)
        {
            this.editedAttribute = attributeToEdit;
            this.oldValue = attributeToEdit.Value;
            this.newValue = newValue;
        }

        public override ActionEvent Execute()
        {
            editedAttribute.Value = newValue;
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            editedAttribute.Value = oldValue;
            return ActionEvent.NoEvent;
        }
    }
    public class XElementReplaceChildren : Action
    {
        private XElement objXElement;
        private XElement originalXElement;
        private XElement newXElement;

        public XElementReplaceChildren(XElement node, XElement newXElement)
        {
            this.objXElement = node;
            this.originalXElement = new XElement(node);
            this.newXElement = new XElement(newXElement);
        }

        public override ActionEvent Execute()
        {
            if (objXElement != null)
            {
                objXElement.RemoveNodes();
                objXElement.Add(newXElement.Elements());
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (objXElement != null)
            {
                objXElement.RemoveNodes();
                objXElement.Add(originalXElement.Elements());
            }
            return ActionEvent.NoEvent;
        }
    }
    public class XElementReplaceChildrenWithSnapshot : Action
    {
        private XElement objXElement;
        private XElement originalXElement;
        private XElement newXElement;
        private bool skipFirstDo = false;

        public XElementReplaceChildrenWithSnapshot(XElement node, XElement oldXelement)
        {
            this.objXElement = node;
            this.originalXElement = new XElement(oldXelement);
            this.newXElement = new XElement(node);
        }

        public override ActionEvent Execute()
        {
            if (objXElement != null)
            {
                if (skipFirstDo)
                {
                    objXElement.RemoveNodes();
                    objXElement.Add(newXElement.Elements());
                }
                else
                {
                    skipFirstDo = true;
                }
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (objXElement != null)
            {
                objXElement.RemoveNodes();
                objXElement.Add(originalXElement.Elements());
            }
            return ActionEvent.NoEvent;
        }
    }
    public class ResetEditorSelection : Action
    {
        FFXUI ui;
        public ResetEditorSelection(FFXUI ffxRelevant)
        {
            ui = ffxRelevant;
        }

        public override ActionEvent Execute()
        {
            MainUserInterface.ResetEditorSelection(ui);
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            MainUserInterface.ResetEditorSelection(ui);
            return ActionEvent.NoEvent;
        }
    }
    public class XElementRemove : Action
    {
        private XElement objXElement;
        private XElement XParent;
        private XElement originalXElement;
        private int indexInParent;

        public XElementRemove(XElement nodeToRemove)
        {
            this.objXElement = nodeToRemove;
            this.originalXElement = new XElement(nodeToRemove);
            this.XParent = nodeToRemove.Parent;
            indexInParent = FFXHelperMethods.GetNodeIndexinParent(nodeToRemove);
        }

        public override ActionEvent Execute()
        {
            if (objXElement != null)
            {
                    objXElement.Remove();
            }
            return ActionEvent.NoEvent;
        }

        public override ActionEvent Undo()
        {
            if (objXElement != null)
            {
                if (indexInParent > 0)
                {
                    XParent.Elements().ElementAt(indexInParent - 1).AddAfterSelf(originalXElement);
                    objXElement = XParent.Elements().ElementAt(indexInParent);
                }
                else
                {
                    XParent.AddFirst(originalXElement);
                    objXElement = XParent.Elements().First();
                }

            }
            return ActionEvent.NoEvent;
        }
    }
    public class CompoundAction : Action
    {
        private List<Action> Actions;

        private Action<bool> PostExecutionAction = null;

        public CompoundAction(List<Action> actions)
        {
            Actions = actions;
        }

        public void SetPostExecutionAction(Action<bool> action)
        {
            PostExecutionAction = action;
        }

        public override ActionEvent Execute()
        {
            var evt = ActionEvent.NoEvent;
            foreach (var act in Actions)
            {
                if (act != null)
                {
                    evt |= act.Execute();
                }
            }
            if (PostExecutionAction != null)
            {
                PostExecutionAction.Invoke(false);
            }
            return evt;
        }

        public override ActionEvent Undo()
        {
            var evt = ActionEvent.NoEvent;
            foreach (var act in Actions)
            {
                if (act != null)
                {
                    evt |= act.Undo();
                }
            }
            if (PostExecutionAction != null)
            {
                PostExecutionAction.Invoke(true);
            }
            return evt;
        }
    }
}