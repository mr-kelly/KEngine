//------------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Version 0.9.1 (20151010)
//                     Copyright © 2011-2015
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//------------------------------------------------------------------------------
/**
    *   Use:
    *   
    *      Response.Write(Singleton<Test>.Instance().Time);  ...
    */

namespace KEngine.Utils
{
    public class Singleton<T> where T : new()
    {
        public Singleton() { }

        public static T Instance
        {
            get
            {
                return SingletonCreator.instance;
            }
        }

        private class SingletonCreator
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit 明确的静态构造函数告诉C#编译器不标记类型在 字段初始化前
            static SingletonCreator() { }
            internal static T instance = new T();

            private SingletonCreator()
            {
            }
        }
    }
    
}