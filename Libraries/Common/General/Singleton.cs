﻿using System.Threading;

namespace AXToolbox.Common
{
    /*
     * http://www.sanity-free.com/132/generic_singleton_pattern_in_csharp.html
     * 
     * Invocation: 
     *   ClassName x = Singleton<ClassName>.Instance;
     *   Singleton<ClassName>.Instance.SomeClassNameMethod();
    */
    public static class Singleton<T> where T : new()
    {
        static Mutex mutex = new Mutex();
        static T instance;
        public static T Instance
        {
            get
            {
                mutex.WaitOne();
                if (instance == null)
                {
                    instance = new T();
                }
                mutex.ReleaseMutex();
                return instance;
            }
        }
    }
}
