using System;
using System.Linq;

namespace MData.Core
{
    public class BaseLogic<T>
    {
        private T _currentInstance;
        public T CurrentInstance
        {
            get { return _currentInstance; }
            set
            {
                _currentInstance = value;
                Init();
            }
        }

        public virtual void UnImplementedNoReturnMethodCall(string methodName, params object[] parameters)
        {
            //Console.WriteLine("Calling 'UnImplementedNoReturnMethodCall'\n\t => Method: {0},\n\t Parameters: {1}", methodName, parameters.Select(x => x == null ? "null" : x.ToString()).Aggregate((x, y) => x + ", " + y));   
        }

        public virtual TU UnImplementedMethodCall<TU>(string methodName, params object[] parameters)
        {
            //Console.WriteLine("Calling 'UnImplementedMethodCall<{2}>'\n\t => Method: {0},\n\t Parameters: {1}", methodName, parameters.Select(x => x.ToString()).Aggregate((x, y) => x + ", " + y), typeof(T).Name);   
            return default(TU);
        }

        public EntityBase<T> EntityBase { get { return CurrentInstance as EntityBase<T>; } }

        protected virtual void Init()
        {
            //Console.WriteLine("Init on {0}", GetType().FullName);
        }
    }
}

