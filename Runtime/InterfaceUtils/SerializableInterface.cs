#nullable disable
#pragma warning disable SA1618 // Generic type parameters should be documented
namespace HH.SerializableInterface
{
    /*****
    * Provides a serializable wrapper that allows you to store a reference to
    * a serialized UnityEngine.Object that implement a certain interface.
    * The "Instance" property provides direct easy access to the serialized reference.
    * This should work with Components as well as ScriptableObjects.
    * It ships with a convenient property drawer that should streamline the usage.
    *
    * Copyright (c) 2023 Bunny83
    *
    * Permission is hereby granted, free of charge, to any person obtaining a copy
    * of this software and associated documentation files (the "Software"), to
    * deal in the Software without restriction, including without limitation the
    * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
    * sell copies of the Software, and to permit persons to whom the Software is
    * furnished to do so, subject to the following conditions:
    *
    * The above copyright notice and this permission notice shall be included in
    * all copies or substantial portions of the Software.
    *
    * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
    * IN THE SOFTWARE.
    *
    *****/
    using UnityEngine;

    /// <summary>
    /// Edited version of implementation found online. <see cref="https://github.com/Bunny83/Utilities/blob/master/SerializableInterface.cs"/>
    // </summary>
    [System.Serializable]
    public class SerializableInterface<T> where T : class
    {
        [SerializeField]
        private Object obj;
        private T instance = null;

        public SerializableInterface(T instance)
        {
            SetInstance(instance);
        }

        public T Instance { get => GetInstance(); set => SetInstance(value); }

        public T GetInstance()
        {
            if (instance == null || (object)instance != obj)
            {
                if (obj == null)
                {
                    SetInstance(null);
                }
                else if (obj is T inst)
                {
                    instance = inst;
                }
                else if (obj is GameObject go && go.TryGetComponent(out inst))
                {
                    instance = inst;
                }
                else
                {
                    SetInstance(null);
                }
            }

            return instance;
        }

        private void SetInstance(T aInstance)
        {
            instance = aInstance;
            obj = instance as Object;
        }
    }
}
