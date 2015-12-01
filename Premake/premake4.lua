
--- Premake5 Dev -----

solution "KEngine.Solution"
    configurations { "Debug", "Release" }
    location ("../Solution/" .. (_ACTION or ""))
    debugdir ("../bin") --PATCHED
    debugargs { } --PATCHED

configuration "Debug"
    flags { "Symbols" }
    defines { "_DEBUG", "DEBUG", "TRACE" }
configuration "Release"
    flags { "Optimize" }
configuration "vs*"
	defines { "MS_DOTNET" }


local UNITY_ENGINE_DLL = "C:/Program Files (x86)/Unity/Editor/Data/Managed/UnityEngine.dll"
local UNITY_UI_DLL = "C:/Program Files (x86)/Unity/Editor/Data/UnityExtensions/Unity/GUISystem/4.6.9/UnityEngine.UI.dll"
local SharpZipLib_DLL = "../KEngine.UnityProject/Assets/KEngine/Lib/SharpZipLib/ICSharpCode.SharpZipLib.dll"
local UNITY_EDITOR_DLL = "C:/Program Files (x86)/Unity/Editor/Data/Managed/UnityEditor.dll"

------------------KEngine Main--------------------
project "KEngine"
language "C#"
kind "SharedLib"
framework "3.5"
targetdir "../Build/KEngine"

files
{
    "../KEngine.UnityProject/Assets/KEngine/**.cs",
}

defines
{
}

links
{
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    SharpZipLib_DLL
}
----------------------- KEngine Editor
project "KEngine.Editor"
language "C#"
kind "SharedLib"
framework "3.5"
targetdir "../Build/KEngine.Editor"

files
{
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/**.cs",
    "../KEngine.UnityProject/Assets/KEngine.EditorTools/Editor/**.cs",
}

defines
{
}

links
{
    "KEngine",
    "System",
    UNITY_ENGINE_DLL,
    UNITY_UI_DLL,
    SharpZipLib_DLL,
    UNITY_EDITOR_DLL,
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/CosmosTable.Compiler/DotLiquid.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OOXML.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OpenXml4Net.dll",
    "../KEngine.UnityProject/Assets/KEngine.Editor/Editor/NPOI/NPOI.OpenXmlFormats.dll",
}

------------------RoleServer--------------------
-- project "roleserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/roleserver/**.cs",
--     "../tvgxservers/roleserver/**.cs"
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_ROLESERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------GameCenter--------------------
-- project "gamecenter"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/gamecenter/**.cs",
--     "../tvgxservers/gamecenter/**.cs"
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_GAMECENTER",
-- 	"KTV_MAILSERVER",
-- 	"KTV_RELATIONSERVER",
-- 	"KTV_ARENASERVER",
-- 	"KTV_GUILDSERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",
--     "../deps/packages/DotNetZip/Ionic.Zip.Reduced",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------LoginServer--------------------
-- project "loginserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/loginserver/**.cs",
--     "../tvgxservers/loginserver/**.cs"
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_LOGINSERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------ServiceCenter--------------------
-- project "servicecenter"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/servicecenter/**.cs",
--     "../tvgxservers/servicecenter/**.cs"
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_SERVICECENTER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }


-- ------------------GameServer--------------------
-- project "gameserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../tvgxservers/gameserver/**.cs"
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_GAMESERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",
--     "../deps/packages/DotNetZip/Ionic.Zip.Reduced",
    
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     --"Microsoft.CSharp",-- BUG In Premake4.4!!

--     -- "dev_base"
-- }

-- ------------------WorldServer--------------------
-- project "worldserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/worldserver/**.cs",
-- 	"../common/base/**.cs",
-- }

-- defines
-- {
--     "KTV_SERVER",
-- 	"KTV_WORLDSERVER"
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "System.Web",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------ProtoTrans--------------------
-- project "prototrans"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/prototrans/**.cs",
-- }

-- defines
-- {
--     "KTV_SERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "System.Web",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------Test--------------------
-- project "test_net"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../common/test/**.cs",
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_TEST",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------TestClient--------------------
-- project "test_db"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
-- 	"../../CoreSource/DevBase/**.cs",
-- 	"../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/testclient/**.cs",
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_TESTCLIENT",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------VoiceRecognizeServer--------------------
-- project "voicerecognizeserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
-- 	"../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/voicerecognizeserver/**.cs",
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_VRSERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",
--     "../deps/packages/DotNetZip/Ionic.Zip.Reduced",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------ResourceServer--------------------
-- project "resourceserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/resourceserver/**.cs",
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_RESOURCESERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "System.Web",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",

--     -- "dev_base"
-- }

-- ------------------BattleServer--------------------
-- project "battleserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/battleserver/**.cs",
--     "../common/battleserver/**",
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_BATTLESERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",
--     "../deps/packages/DotNetZip/Ionic.Zip.Reduced",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",
--     --"Microsoft.CSharp",-- BUG In Premake4.4!!

--     -- "dev_base"
-- }

-- ------------------TOPServer--------------------
-- project "topserver"
-- language "C#"
-- kind "ConsoleApp"
-- framework "4.5"
-- targetdir "../_bin"

-- files
-- {
--     "../../CoreSource/DevBase/**.cs",
--     "../../CoreSource/GameBase/**.cs",
--     "../common/base/**.cs",
--     "../common/topserver/**.cs",
--     "../common/topserver/**",
-- }

-- defines
-- {
--     "KTV_SERVER",
--     "KTV_TOPSERVER",
-- }

-- links
-- {
--     "System",
--     "System.Core",
--     "System.Data",
--     "System.Xml",
--     "System.Xml.Linq",
--     "../deps/packages/MySql.Data.6.8.3/lib/net45/MySql.Data",
--     "../deps/packages/protobuf-net.2.0.0.668/lib/net35/protobuf-net",
--     "../deps/packages/LitJson",
--     "../deps/packages/Newtonsoft.Json",
--     "../deps/packages/DotNetZip/Ionic.Zip.Reduced",

--     -- NeoLua
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua",
--     -- "../deps/packages/NeoLua.1.0.3/lib/net45/Neo.Lua.Desktop",
--     --"Microsoft.CSharp",-- BUG In Premake4.4!!

--     -- "dev_base"
-- }

