<a name="cn-title"></a>CosmosEngine
=========================================
[https://github.com/Cosmosbox/CosmosEngine](https://github.com/Cosmosbox/CosmosEngine "CosmosEngine")

* [快速入门](#cn-quick-start)
* [简介](#cn-intro)
* [特性](#cn-feature)
* [约定](#cn-convention)
* [整体架构图](#cn-structure)
* [使用经验](#cn-exp)
* [工作流](#cn-workflow)
* [未来功能](#cn-future)




<a name="cn-quick-start"></a>快速入门
------------------
* 1.将NGUI 3.7+版拷到Assets目录下
* 2.在安装Unity的电脑上，打开场景文件: Assets/CosmosEngine/Demo/DemoGame/Bootstrap.unity
* 点击播放，开始一个框架DEMO, 功能包括：打开一个的UI窗（自动匹配脚本）、读取一个Tab表格

[回目录](#cn-title)





<a name="cn-intro"></a>简介
------------------
CosmosEngine - Unity3D /2D 轻量级模块化游戏开发框架，包含轻量级的代码和常用模块，提供一定的编程范式作约束和工作流程，旨在减少开发人员的重复工作和做决定的次数。

[回目录](#cn-title)





<a name="cn-feature"></a>特性
----------------------------

### 非侵入式设计
一切从CosmosEngine.Create()开始，你甚至可以在现有的项目基础上嵌入。

### 轻量级
CosmosEngine讨厌厚重的代码类，它十分轻量，使得任何人都可以很轻松的驾驭。

### 模块化
框架的设计上，一切都是模块化的，框架只是一个包含无数模块的简单容器。
其中，资源管理模块（ResourceModule）和UI模块（UIModule）是底层模块，其余所有你自己的游戏逻辑都通过自定义模块实现。另有一些提供常用模块。（如：常用技能模块、资源更新模块、配置读取模块、多语言模块等可热插拔）

### 约定优于配置
制定了一些规则和约定，旨在减少软件开发人员需做决定的次数，它们包括：类似Ruby on Rails MVC的UI模块，事件驱动编程准则等。

### 轻松使用第三方插件
得益于轻量化、模块化的设计，框架是包容一切第三方插件。
比如默认使用UI插件式NGUI，通过内部提供的NGUI桥接类实现与底层UIModule的衔接。

### 拥抱依赖和耦合
CosmosEngine中假定耦合是不可避免的，对依赖和耦合持开放的态度，使用显式声明来说明模块耦合情况。譬如UI模块，依赖Resource模块，通过[CModuleDependency(xxx)]这个Attribute来说明。
同时CosmosEngine中更多的通过C#的委托/事件机制来减少降低耦合度，譬如说，在快速入门的DEMO中，UI依赖资源，但是GameSettings并不需要依赖资源模块。

[回目录](#cn-title)





<a name="cn-convention"></a>约定
-------------------------------------------------------
作者早年从事Web开发，深深受到“约定优于配置”的影响，认为这是能够让开发人员、游戏策划人员走出无数配置文件陷阱的重要因素。
CosmosEngine中的约定，除去框架内部代码外，是没有强迫性的，如果你不喜欢，是可以拒绝使用，使用你自己的编程范式。
以下是一些使用CosmosEngine开发中建议的约定：

### 遵循微软官方C#命名规范
紧跟着老大哥的命名规范，同时作者认为“语言命名规范”是一个语言快速入门的利器：
http://msdn.microsoft.com/en-us/library/ff926074.aspx

### 尽可能的不使用配置文件
CosmosEngine讨厌配置，它倾向于通过命名之间的关系来确定逻辑关系。如一个GameObject叫UIWelcome，那么它使用CUIWelcome Class类进行匹配。
当然，它有一个全局的配置文件（Resources/CEngineConfig.txt），用于配置一些目录定义。


### 事件驱动编程
开发过程中更多使用C#的事件/委托来实现高内聚低耦合。事件的使用通常有两种方式，有人喜欢做全局事件，把所有事件扔到一个类里。CosmosEngine讨厌这种方式，会令到某一个功能类对全局有依赖。
CosmosEngine的事件定义，只针对地写在特定模块内。



### 最终目标 - 把某个模块拷到另一个项目可以不作任何修改运行，不重新发明轮子

新项目的开发过程中，最浪费时间的莫过于从头构建底层框架了，既枯燥又重复。更令人恐惧的是不同项目有些功能是一样的，却因部分耦合的问题，导致要重写、维护两套代码，恶心之极。
CosmosEngine中的所有设计，都基于这个开始思考的。

[回目录](#cn-title)




<a name="cn-structure"></a>整体结构图
----------------------------------------------
![Structure of CosmosEngine](https://raw.githubusercontent.com/mr-kelly/CosmosEngine/master/CosmosEngineStructure.png)

[回目录](#cn-title)




<a name="cn-exp"></a>使用经验
----------------------------------------------

### 异步编程，善用协程和回调
Unity的协程是一个非常好用的特性，它底层的很多异步都通过实现。
而CosmosEngine中回调也比较频繁使用，用来简化一些协程代码，比如UI模块函数CallUI，传入回调函数，等待资源完成加载后执行。

协程的本质是每一个游戏帧进行轮询，而回调的本质就是事件驱动编程。 CosmosEngine里的回调，基本是包裹着协程的。
不使用多线程、多进程，否则在Unity引擎中会有一些不可预料情况。

### 通过委托/事件大大的降低模块耦合度
CosmosEngine的设计原则是，把一个类拷到另一个项目，可以完整无缺的运行。所有的内置模块，最多会有1~2个的外部依赖，其它都是发送事件，交给外部进行事件绑定处理。

### GBK编码问题
尽管在以往项目有一套手机GBK解决方案，但并没有放入CosmosEngine。CosmosEngine中统一使用UTF-8。

### Excel保存表是GBK编码
使用Excel表的过程中，会使用GBK进行表的保存。我们在使用CosmosEngine中對Excel表中实行编译机制，既把GBK转成UTF-8，同时又可以对未编译的Excel表进行批注、乱涂乱画。

[回目录](#cn-title)



<a name="cn-workflow"></a>工作流
------------------------------------------
![Workflow of CosmosEngine](https://raw.githubusercontent.com/mr-kelly/CosmosEngine/master/Docs/Workflow.png)

CosmosEngine为三个角色（程序、美术、策划）的团队协作提供一个工作流：
* 程序进行代码的编写，修改进行提交
* 美术进行资源设计，修改后进行提交
* 策划以Excel形式进行游戏内容编辑，通过Excel编译器编译成Tab-CSV或其它程序可读内容。（在另一个开源项目[KKDaemon](https://github.com/mr-kelly/KKDaemon)中实现）
* Robot——机器人，CosmosEngine可配合另一个开源项目[Handyman](https://github.com/mr-kelly/Handyman)，进行Web端的定时自动代码编译、资源编译等。（类似的还有QuickBuild等）

乍一看，它没啥特别，事实它着重希望砍掉的一些重复的工作：
* 避免人工化的资源操作，譬如美术希望看到资源在游戏中的效果，提交并呼唤Robot进行资源编译就可以了，不用打扰程序和策划
* 策划可以自由使用成熟的编辑器——Excel进行编辑，可以加注释等让自己工作更轻松
* 程序不用每天被策划问表怎么填和被美术呼唤帮忙试一下了
* 释放多余劳动力，减少人力成本和时间成本————就是帮老板省钱

[回目录](#cn-title)





<a name="cn-future"></a>未来功能
----------------------------------------------
CosmosEngine旨在让一些复杂的事情变得简单。
游戏开发圈子盛行加班；是的，开发一款游戏不容易，但不代表它不能变得更加容易。作者认为，对开发人员来说，糟糕的系统设计和低易用性的功能模块是加班的罪魁祸首。有一些模块功能，它并复杂，可却经常重复。

未来将把这些模块整合进去, 它们基本都是已实现的独立模块：

* []通用技能模块(Skill-Bullet-Buff)
* []资源自动更新模块(CUpdateModule)
* []Http模块
* []新的资源依赖处理系统(CDependencyModule)
* []多语言系统(CLocalization)
* []预加载机制(CPreloadModule)
* []声效模块(CAudioModule)
* []Lua引擎(CScripts)

* etc...


[回目录](#cn-title)

[中文博客介绍](http://www.cnblogs.com/mrkelly/p/3944773.html)

Author/Contributors
===================
[mr-kelly](https://github.com/mr-kelly)


Forks
================
[Stable/Official](https://github.com/Cosmosbox/CosmosEngine)
[Alpha Fork](https://github.com/mr-kelly/CosmosEngine)




CosmosEngine
==============================================================

CosmosEngine - A Unity3D Game Framework

Design Origin
--------------------------------------------------------
For a experienced developer, develop a new game is depressing.They should pull their old code from their previous projects and refactor them.If they have a some bad code in them, Pull Old Code will become a terrible job.

For a game develop newbie, develop a new game is destress. They should construct the project's base code from the beginning.

So develop framework was borned. There are thousands of Web Develop Framework to help you develop a MVC style web application, but a few game develop framework to help us. 

Unity3D is excellent game engine. It has countless plugins to speed up your game develop.  However, using Unity3D to develop game is flexibleand it has many different ways: you can just use MonoBehaviour or Scene to construct a game, or use code purely, or other thounsands ways.

In short, no integrated solution to develop Unity3D game.

CosmosEngine is a try to solve this problem. Its style is a develop solution of pure code.

Design Patterns
------------------------------------------------

* __Very Lightweight__ - Less is more. She love elegant and clean, she hate clumsy code
* __Just Framework__ - No any specific functionalities like UI/2D Plugins
* __Depend on third party plugins__ - You can use your favourite Plugins like NGUI, 2DToolkit or something else
* __Modularized__ - Yes, she's clean. But you can dress the framework via add Modules

Structure
------------------------------

![Cosmos Engine Structure](https://raw.githubusercontent.com/mr-kelly/CosmosEngine/master/CosmosEngineStructure.png)
