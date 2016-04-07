
AssetDep系统已经弃用！！！ Unity 4.x使用ResourcesDep模块！Unity 5.x直接使用官方更完善的AssetBundle机制即可！！！
=======================




依赖处理系统 AssetDep
==========================

Unity3D官方提供的Push/Pop依赖机制有很多不方便的因素，其依赖是依赖于AssetBundle GUID而不是依赖资源本身，需要AssetBundle存在内存。所以独立开发AssetDep依赖机制。

* 好处：节省内存，依赖打包全自动
* 缺点：要增强某些依赖项，需要单独写脚本

AssetDep依赖原理
--------------------
* 在A GameObject上挂一个KAssetDep.cs脚本，用来标注它依赖什么资源
* 加载A GameObject后，KAssetDep会加载依赖资源，不同类型的KAssetDep会做不同的相应处理

举例：一个KUISpriteAssetDep，用于加载UI Sprite，打包时，将往A GameObject挂上脚本，同时把A GameObject上的UISprite控件挖空依赖（atlas = null），再进行AssetBundle打包。
加载时KUISpriteAssetDep将进行加载(load atlas)，完成后设置UISprite属性。
