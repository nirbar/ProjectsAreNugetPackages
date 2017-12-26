# ProjectsAreNugetPackages

- Build a nuget package after having built the project.
- Consume project dependencies as nuget packages after a one-time migration

## Custom Properties

- BuildNugetPackage: true/false. Whether or not to build a Nuget package after build
- OfficialBuild: true/false. Helper property to resolve default values for other properties.
- NugetToolPath: Folder path of Nuget.exe
- NugetConfig: Path to Nuget.config
- NuSpecPath: Path to project's nuspec file. If empty, defaults to built-in nuspec file.
- NugetManufacturer: Package author field.
- NuspecVersion: Package version (without suffix).
- NuspecVersionSuffix: Package version suffix. Default to:
  - 'local' on non-official release builds
  - 'localdebug' on non-official debug builds
  - nothing on release official builds
- NugetIdPrefix: Package prefix. Used to distinguish project dependencies from available packages on nuget.org
- NugetBuildFolder: Tagret folder for Nuget packages. Expected to exist in nuget.config file.
- NugetSource: URL to nuget source to add packages to on UploadNugetPackage target
- NugetUpdatePreRelease: Whether or not to update/install pre-release nuget packages. Used on NugetInstallUpdate target.

## Custom Items

- NugetPackProperty: key=value pairs to pass as properties to Nuget pack command.
- NugetUpdateSource: List of nuget URLs to update from. Defaults to NugetBuildFolder and NugetSource.

## Tasks

- ConvertProjectRefToNugetPackage: One-time task to convert project refrences to Nuget dependencies
  - Inputs:
    - Projects [Required]: Item list with all projects to convert.
	- SolutionDir [Required]: Solution folder
	- PackageIdPrefix: Prefix to project names as Nuget dependencies. See property NugetIdPrefix.
- ResolveDependants: Resolve build order according to Nuget dependants
  - Inputs:
    - DependecyProject [Required]: The target project to build.
	- AllProjects [Required]: Item list with all projects.
	- PackageIdPrefix: Prefix to project names as Nuget dependencies. See property NugetIdPrefix.
  - Outputs:
    - DependantProjectsBuildOrdered: Item list- projects to build in order. Projects that do not depend on DependecyProject are not on the list

## Targets

- ConvertProjectRefToNugetPackage: See ConvertProjectRefToNugetPackage task
- ResolveDependants: See ResolveDependants task
- NugetInstallUpdate: Install and update nuget packages
- UploadNugetPackage: Nuget add built package to 'NugetSource' repository
- CreateNugetPackage: Pack a project.

# How to use

## First time init

1. Install the package to all projects
1. Create a local nuget.config file
  - Add source with relative path to local nuget build folder.
  - [Optional] Add source to organization's nuget repository.
1. Create a file 'ToduBuild.custom.props' with these properties:
  - NugetConfig
  - NugetIdPrefix
  - NuspecVersion
  - FullVersion (for TidyBuild)
  - [Optional] NugetSource
1. Build all projects normally
1. MSBuild all projects with target 'ConvertProjectRefToNugetPackage'
1. Build again all projects.
1. Create build.xml file. Sample code:
~~~~~~~~~~~
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildThisFileDirectory)\packages\ProjectsAreNugetPackages.1.0.5\build\ProjectsAreNugetPackages.tasks"/>

  <ItemGroup>
    <AllProjects Include=".\**\*.*proj"/>
  </ItemGroup>
  <PropertyGroup>
    <TargetProject>ConsoleApplication1\ConsoleApplication1.vcxproj</TargetProject>
  </PropertyGroup>

  <Target Name="Build">
    <ResolveDependants DependecyProject="$(TargetProject)" AllProjects="@(AllProjects)" PackageIdPrefix="Local.">
      <Output TaskParameter="DependantProjectsBuildOrdered" ItemName="ProjectBuildOrder" />
    </ResolveDependants>
    <Message Text="Project build order: @(ProjectBuildOrder)"/>

    <!-- Must set 'RunEachTargetSeparately' since NugetInstallUpdate target changes the project file so it must reload -->    
    <MSBuild Projects="%(ProjectBuildOrder.FullPath)" Targets="NugetInstallUpdate;Rebuild" RunEachTargetSeparately="true" />

    <!-- After successful build, upload Nuget packages -->
    <MSBuild Projects="%(ProjectBuildOrder.FullPath)" Targets="UploadNugetPackage"/>
  </Target>
</Project>
~~~~~~~~~~~

## Routine builds

1. MSBuild build.xml target 'Build'. Make sure to set 'TargetProject' property from command line.
