
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;


namespace LiveSplit.Model.Input
{
    public delegate void EventHandlerT<T>(object sender, T value);

    public class KeyOrButton
    {
        public bool IsButton { get; protected set; }
        public bool IsKey { get { return !IsButton; } set { IsButton = !value; } }

        public Keys Key { get; protected set; }

        public KeyOrButton(Keys key)
        {
            Key = key;
            IsKey = true;
        }


        public KeyOrButton(string stringRepresentation)
        {
            if (stringRepresentation.Contains(' ') && !stringRepresentation.Contains(", "))
            {
                var split = stringRepresentation.Split(new char[] { ' ' }, 2);
               
                IsButton = true;
            }
            else
            {
                Key = (Keys)Enum.Parse(typeof(Keys), stringRepresentation, true);
                IsKey = true;
            }
        }

        public override string ToString()
        {
           // if (IsKey)
                return Key.ToString();

        }

        public static bool operator ==(KeyOrButton a, KeyOrButton b)
        {
            if ((object)a == null && (object)b == null)
                return true;
            if ((object)a == null || (object)b == null)
                return false;

            if (a.IsKey && b.IsKey)
            {
                return a.Key == b.Key;
            }

            return false;
        }

        public static bool operator !=(KeyOrButton a, KeyOrButton b)
        {
            return !(a == b);
        }
    }

    public class CompositeHook
    {
        protected LowLevelKeyboardHook KeyboardHook { get; set; }



        public event KeyEventHandler KeyPressed;

        public event EventHandlerT<KeyOrButton> KeyOrButtonPressed;

        public CompositeHook()
        {
            KeyboardHook = new LowLevelKeyboardHook();

            KeyboardHook.KeyPressed += KeyboardHook_KeyPressed;

        }


        void KeyboardHook_KeyPressed(object sender, KeyEventArgs e)
        {
            Console.WriteLine("KeyDetected");
            KeyPressed?.Invoke(this, e);
            KeyOrButtonPressed?.Invoke(this, new KeyOrButton(e.KeyCode | e.Modifiers));
        }
    
        public void RegisterHotKey(Keys key)
        {
            KeyboardHook.RegisterHotKey(key);
        }



/*
        public void RegisterHotKey(Keys key)
        {
                RegisterHotKey(key);

        }
        */
        public void Poll()
        {
            KeyboardHook.Poll();
        }

        public void UnregisterAllHotkeys()
        {
            KeyboardHook.UnregisterAllHotkeys();
        }
    }
}
