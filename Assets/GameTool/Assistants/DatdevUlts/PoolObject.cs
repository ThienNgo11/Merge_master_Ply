using System;
using System.Collections.Generic;

namespace DatdevUlts
{
    public class PoolObjectManager<T> where T : PoolObject, new()
    {
        private List<T> _stack = new List<T>();

        public T GetObject()
        {
            if (_stack.Count == 0)
            {
                _stack.Add(new T());
            }

            var index = _stack.Count - 1;
            T item = _stack[index];
            item.OnRelease += () => Release(item);
            _stack.RemoveAt(index);

            return item;
        }

        private void Release(T poolObject)
        {
            _stack.Add(poolObject);
        }
    }

    public abstract class PoolObject
    {
        internal Action OnRelease { get; set; }

        public void Release()
        {
            OnRelease?.Invoke();
            OnRelease = null;
        }
    }
}