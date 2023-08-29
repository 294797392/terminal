﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModengTerm.Base
{
    public static class ReusableQueue<T> where T : ICloneable<T>
    {
        private static readonly Dictionary<Type, ConstructorInfo> ConstructorMap = new Dictionary<Type, ConstructorInfo>();

        private static readonly Queue<T> BaseQueue = new Queue<T>();

        /// <summary>
        /// 创建一个新的字符
        /// </summary>
        /// <remarks>
        /// 该方法会首先从缓存里取字符，如果缓存里没有空闲字符，那么创建一个新的
        /// </remarks>
        /// <returns></returns>
        public static T Create()
        {
            T instance = default(T);

            if (BaseQueue.Count > 0)
            {
                instance = BaseQueue.Dequeue();
                instance.SetDefault();
            }
            else
            {
                Type t = typeof(T);

                ConstructorInfo constructorInfo;
                if (!ConstructorMap.TryGetValue(t, out constructorInfo))
                {
                    lock (ConstructorMap)
                    {
                        constructorInfo = typeof(T).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
                        ConstructorMap[t] = constructorInfo;
                    }
                }

                instance = (T)constructorInfo.Invoke(null);
            }

            return instance;
        }

        /// <summary>
        /// 回收某个字符
        /// 该字符用不到了，下次可以复用
        /// </summary>
        /// <param name="instance">要回收到字符</param>
        public static void Recycle(T instance)
        {
            BaseQueue.Enqueue(instance);
        }

        public static void Recycle(IEnumerable<T> instances)
        {
            foreach (T character in instances)
            {
                ReusableQueue<T>.Recycle(character);
            }
        }

        /// <summary>
        /// 把copyFrom拷贝到copyTo
        /// 保证copyTo和copyFrom的数量和值是相同的
        /// </summary>
        /// <param name="copyTo"></param>
        /// <param name="copyFrom"></param>
        public static void CopyTo(List<T> copyTo, List<T> copyFrom)
        {
            // copyTo比copyFrom多的或者少的字符个数
            int value = Math.Abs(copyTo.Count - copyFrom.Count);

            if (copyTo.Count > copyFrom.Count)
            {
                // 删除多余的字符
                for (int i = 0; i < value; i++)
                {
                    T character = copyTo[0];
                    copyTo.Remove(character);
                    ReusableQueue<T>.Recycle(character);
                }
            }
            else if (copyTo.Count < copyFrom.Count)
            {
                // 补齐字符
                for (int i = 0; i < value; i++)
                {
                    T character = ReusableQueue<T>.Create();
                    copyTo.Add(character);
                }
            }

            for (int i = 0; i < copyFrom.Count; i++)
            {
                T toCopy = copyTo[i];
                T fromCopy = copyFrom[i];

                fromCopy.CopyTo(toCopy);
            }
        }
    }
}
