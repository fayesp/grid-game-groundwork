"""Regenerate Unity's 3 legacy csproj files from the current .cs files on disk.

Mirrors Unity 2019.4's assembly split:
- Assembly-CSharp-firstpass: Assets/Plugins/**/*.cs
- Assembly-CSharp: everything else except any /Editor/ folder
- Assembly-CSharp-Editor: any .cs under an /Editor/ folder

Then run:  dotnet build Assembly-CSharp-firstpass.csproj &&
          dotnet build Assembly-CSharp.csproj &&
          dotnet build Assembly-CSharp-Editor.csproj
"""
import io, glob, os

ROOT = r'D:/desktop/grid-game-groundwork'
UNITY = r'C:/Program Files/Unity/Hub/Editor/2019.4.10f1/Editor/Data'
FW = r'C:/Program Files (x86)/Reference Assemblies/Microsoft/Framework/.NETFramework/v4.7.1'
DOTWEEN = ROOT + '/Assets/Plugins/Demigiant/DOTween/DOTween.dll'
DOTWEEN_EDITOR = ROOT + '/Assets/Plugins/Demigiant/DOTween/Editor/DOTweenEditor.dll'
UI = ROOT + '/Library/ScriptAssemblies/UnityEngine.UI.dll'
TMPRO = ROOT + '/Library/ScriptAssemblies/Unity.TextMeshPro.dll'
OUT = 'Temp/bin/Debug'

DEFINES = (
    'DEBUG;TRACE;UNITY_2019_4_10;UNITY_2019_4;UNITY_2019;UNITY_5_3_OR_NEWER;'
    'UNITY_5_4_OR_NEWER;UNITY_5_5_OR_NEWER;UNITY_5_6_OR_NEWER;UNITY_2017_1_OR_NEWER;'
    'UNITY_2017_2_OR_NEWER;UNITY_2017_3_OR_NEWER;UNITY_2017_4_OR_NEWER;UNITY_2018_1_OR_NEWER;'
    'UNITY_2018_2_OR_NEWER;UNITY_2018_3_OR_NEWER;UNITY_2018_4_OR_NEWER;UNITY_2019_1_OR_NEWER;'
    'UNITY_2019_2_OR_NEWER;UNITY_2019_3_OR_NEWER;UNITY_2019_4_OR_NEWER;PLATFORM_ARCH_64;UNITY_64;'
    'UNITY_INCLUDE_TESTS;ENABLE_AUDIO;ENABLE_PHYSICS;ENABLE_UNITYEVENTS;ENABLE_VR;'
    'ENABLE_WEBCAM;ENABLE_UNITYWEBREQUEST;ENABLE_WWW;ENABLE_CLOUD_SERVICES;'
    'ENABLE_MONO;NET_STANDARD_2_0;ENABLE_PROFILER;UNITY_ASSERTIONS;UNITY_EDITOR;'
    'UNITY_EDITOR_64;UNITY_EDITOR_WIN;ENABLE_LEGACY_INPUT_MANAGER;CSHARP_7_OR_LATER;CSHARP_7_3_OR_NEWER'
)

BS = chr(92)
ENGINE_DLLS = sorted(glob.glob(UNITY + '/Managed/UnityEngine/*.dll'))
FRAMEWORK = ['mscorlib', 'System', 'System.Core', 'System.Data', 'System.Xml', 'System.Xml.Linq', 'System.Drawing']


def collect_cs():
    # glob returns os.sep-separated paths on Windows; normalize to '/' first
    all_cs = [f.replace(os.sep, '/') for f in sorted(glob.glob('Assets/**/*.cs', recursive=True))]
    # Assets/Tests 属于 Tests.PlayMode 等 asmdef 程序集（引用 NUnit），
    # 不归入 legacy csproj，否则 dotnet build 缺少测试框架引用会报错
    all_cs = [f for f in all_cs if not f.startswith('Assets/Tests/')]
    plugins = [f for f in all_cs if f.startswith('Assets/Plugins/')]
    editor = [f for f in all_cs if '/Editor/' in f]
    editor_set, plugins_set = set(editor), set(plugins)
    firstpass = [f for f in plugins if f not in editor_set]
    runtime = [f for f in all_cs if f not in plugins_set and f not in editor_set]
    return firstpass, runtime, editor


def compile_block(files):
    return '\n'.join('    <Compile Include="%s" />' % f.replace('/', BS) for f in files)


def ref(name, path):
    return '    <Reference Include="%s">\n      <HintPath>%s</HintPath>\n    </Reference>' % (name, path)


def project_xml(asm_name, files, refs):
    return '''<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <CscToolPath>C:/Program Files/Unity/Hub/Editor/2019.4.10f1/Editor/Data/Tools/RoslynScripts</CscToolPath>
    <CscToolExe>unity_csc.bat</CscToolExe>
  </PropertyGroup>
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <OutputType>Library</OutputType>
    <AssemblyName>%(asm)s</AssemblyName>
    <TargetFrameworkVersion>v4.7.1</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>%(out)s/</OutputPath>
    <DefineConstants>%(defines)s</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <NoWarn>0169</NoWarn>
  </PropertyGroup>
  <ItemGroup>
%(compile)s
  </ItemGroup>
  <ItemGroup>
%(refs)s
  </ItemGroup>
  <Import Project="$(MSBuildToolsPath)/Microsoft.CSharp.targets" />
</Project>
''' % {'asm': asm_name, 'out': OUT, 'defines': DEFINES,
       'compile': compile_block(files), 'refs': '\n'.join(refs)}


def engine_refs():
    return [(os.path.basename(p)[:-4], p) for p in ENGINE_DLLS]


def framework_refs():
    return [(n, FW + '/' + n + '.dll') for n in FRAMEWORK]


firstpass, runtime, editor = collect_cs()

# firstpass: engine + DOTween + UI + TMPro + framework (Unity adds all package refs to every assembly)
fp_refs = (engine_refs()
           + [('DOTween', DOTWEEN), ('UnityEngine.UI', UI), ('Unity.TextMeshPro', TMPRO)]
           + framework_refs())
with io.open(ROOT + '/Assembly-CSharp-firstpass.csproj', 'w', encoding='utf-8') as f:
    f.write(project_xml('Assembly-CSharp-firstpass', firstpass, [ref(n, p) for n, p in fp_refs]))

# runtime: engine + DOTween + UI + TMPro + framework + firstpass output
rt_refs = (engine_refs()
           + [('DOTween', DOTWEEN), ('UnityEngine.UI', UI), ('Unity.TextMeshPro', TMPRO)]
           + framework_refs()
           + [('Assembly-CSharp-firstpass', ROOT + '/' + OUT + '/Assembly-CSharp-firstpass.dll')])
with io.open(ROOT + '/Assembly-CSharp.csproj', 'w', encoding='utf-8') as f:
    f.write(project_xml('Assembly-CSharp', runtime, [ref(n, p) for n, p in rt_refs]))

# editor: engine + UnityEditor + DOTween(+Editor) + UI + TMPro + framework + firstpass/runtime outputs
ed_refs = (engine_refs()
           + [('UnityEditor', UNITY + '/Managed/UnityEditor.dll'),
              ('DOTween', DOTWEEN), ('DOTweenEditor', DOTWEEN_EDITOR),
              ('UnityEngine.UI', UI), ('Unity.TextMeshPro', TMPRO)]
           + framework_refs()
           + [('Assembly-CSharp-firstpass', ROOT + '/' + OUT + '/Assembly-CSharp-firstpass.dll'),
              ('Assembly-CSharp', ROOT + '/' + OUT + '/Assembly-CSharp.dll')])
with io.open(ROOT + '/Assembly-CSharp-Editor.csproj', 'w', encoding='utf-8') as f:
    f.write(project_xml('Assembly-CSharp-Editor', editor, [ref(n, p) for n, p in ed_refs]))

print('firstpass: %d files | runtime: %d files | editor: %d files' % (len(firstpass), len(runtime), len(editor)))
