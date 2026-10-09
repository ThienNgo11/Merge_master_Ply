using System;

namespace DatdevUlts
{
    public class CallbackUlts
    {
        private int _currentKeyProtect;

        public int CurrentKeyProtect => _currentKeyProtect;

        public void CallNonProtect(Action callBack, int keyProtect)
        {
            if (keyProtect == _currentKeyProtect)
            {
                callBack?.Invoke();
            }
        }
        
        public void CancelAllCallbackNonProtect()
        {
            _currentKeyProtect = AddCheckProtect(_currentKeyProtect);
        }
        
        private int AddCheckProtect(int check)
        {
            if (check > int.MaxValue - 1000)
            {
                check = -1;
            }

            check++;

            return check;
        }
    }
}