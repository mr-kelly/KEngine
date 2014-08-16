//-------------------------------------------------------------------------
//
//      CosmosEngine - The Lightweight Unity3D Game Develop Framework
//
//                     Copyright © 2011-2014
//                   MrKelly <23110388@qq.com>
//              https://github.com/mr-kelly/CosmosEngine
//
//-------------------------------------------------------------------------
/**
    *   Use:
    *   
    *      Response.Write(Singleton<Test>.Instance().Time);  ...
    */
public class CSingleton<T> where T : new()
{
    public CSingleton() { }

    public static T Instance
    {
        get
        {
            return SingletonCreator.instance;
        }
    }

    class SingletonCreator
    {
        // Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit 明确的静态构造函数告诉C#编译器不标记类型在 字段初始化前
        static SingletonCreator() { }
        internal static T instance = new T();
    }
}