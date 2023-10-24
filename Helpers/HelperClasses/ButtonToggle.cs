using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.Helpers.HelperClasses
{
    public delegate bool PressingButton();

    public delegate void ButtonAction(object o);
    internal class ButtonToggle
    {
        private PressingButton ButtonInput;
        private ButtonAction ButtonAction;
        private bool pressingButton = false;

        public ButtonToggle(PressingButton buttonInput, ButtonAction action)
        {
            ButtonInput = buttonInput;
            ButtonAction = action;
        }

        public void Update(object o)
        {
            if (ButtonInput.Invoke())
            {
                if (!pressingButton && ButtonInput.Invoke())
                {
                    ButtonAction.Invoke(o);
                    pressingButton = true;
                }
            }
            else
                pressingButton = false;
        }
    }
}
