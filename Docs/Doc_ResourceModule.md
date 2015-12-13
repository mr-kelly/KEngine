
资源模块 / Resource Module
============================

资源模块可以说是KEngine最核心的构成，所有其它模块可以说都会对它进行依赖。
它负责AssetBunbld的打包自动化、异步加载化和统一的资源路径规范管理。

资源模块由3个子模块组成：

* 加载系统(KResourceModule): 用于普遍性的对AssetBundle的加载，通用程度较高
* 打包系统(KResourceBuilder): 基于工程需求的资源打包，比方说针对UI、特效，通过写脚本，分别对应不同的打包策略
* 依赖系统(KAssetDep): U3D官方提供的Push/Pop Dependency以外的另一个选择

加载系统 / KResourceModule
------------------------------------------

资源模块的核心部分，KResourceModule规范了不同平台所不同的打包路径、读取路径、Loader调度等基础部分。已KWWWLoader为基础，提供像AssetBundleLoader, TextureLoader, AssetLoader(GameObject), MaterialLoader等多个加载类，提供Callback和协程两种异步风格方式可选。


打包系统 / ResourceBuilder
------------------------------------------

提供一套可编程的AssetBundle打包器。基于ResourceModule提供的资源路径规范。

默认有KBuildUI类，可自扩充KBuildEffect等根据项目特殊需要进行的打包类。

## 依赖系统 / AssetDep

设计成可选的，用来替换Unity官方AssetBundle依赖机制，也可以用回官方的。

带有一个类似垃圾回收机制的资源释放模式。

加载时主要原理：
* 打包时，对GameObject添加一个用于记录依赖属性的MonoBehavior，并剥离依赖资源。
* GameObject加载完成后，读取依赖属性，加载依赖资源。
* 依赖资源完成后，通知GameObject，把依赖资源设置到指定位置
* GameObject完成加载

### 依赖打包 / DependencyBuilder
TODO：
### 依赖加载 / AssetDep

经过DependencyBuilder打包后，一个具有依赖的GameObject上面将绑有一个AssetDep组件，在运行时通过代码


清理无用的AssetBundle
-----------------------------
一个完整的Build过程中，所有涉及的资源是有记录的，因此，可以轻松通过Build记录来知道，哪些是废弃资源，然后删除。