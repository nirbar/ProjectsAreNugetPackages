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
- NugetId: Package Id. Defaults to project file name.
- NugetIdPrefix: Package prefix. Used to distinguish project dependencies from available packages on nuget.org
- NugetBuildFolder: Tagret folder for Nuget packages. Expected to exist in nuget.config file.
- NugetSource: URL to nuget source to add packages to on UploadNugetPackage target
- NugetUpdatePreRelease: Whether or not to update/install pre-release nuget packages. Used on NugetInstallUpdate target.

## Custom Items

- NugetPackProperty: key=value pairs to pass as properties to Nuget pack command.
- NugetUpdateSource: List of nuget URLs to update from. Defaults to NugetBuildFolder and NugetSource.

## Custom Imports

Customization properties can be placed in solution folder and/or project folder in file 'ProjectsAreNugetPackages.custom.props' 

## Tasks

- ConvertProjectRefToNugetPackage: One-time task to convert project refrences and library inputs to Nuget dependencies
  - Inputs:
    - Projects [Required]: Item list with all projects to convert. May contain 'Properties' and 'AdditionalProperties' metadata with Key=Value pairs to set on project when loading it to resolve refernces
	- AllProjects [Required]: Item list with all projects. May contain 'Alias' metadata if project file name does not equal the library refernce name.
	- PackageIdPrefix: Prefix to project names as Nuget dependencies. See property NugetIdPrefix.
	- PackageVersion: Version of projects' NuGet dependencies.
- ResolveDependants: Resolve build order according to Nuget dependants
  - Inputs:
    - DependecyProject [Required]: The target project to build.
	- AllProjects [Required]: Item list with all projects.
	- PackageIdPrefix: Prefix to project names as Nuget dependencies. See property NugetIdPrefix.
	- ProcessorCount: Maximal projects to build in parallel. Defaults to Environment.ParallelBuildLevel value. See output parameter DependantProjectsBuildOrdered.
  - Outputs:
    - DependantProjectsBuildOrdered: Item list- projects to build in order. Projects that do not depend on DependecyProject are not on the list.
	  Contains metadata ParallelBuildLevel with numeric level order of projects that can be built in parallel.

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
1. Create a file 'TidyBuild.custom.props' with these properties:
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
  <Import Project="$(MSBuildThisFileDirectory)\packages\ProjectsAreNugetPackages.1.0.9\build\ProjectsAreNugetPackages.tasks"/>

  <ItemGroup>
    <AllProjects Include=".\**\*.*proj"/>
  </ItemGroup>
  <ItemDefinitionGroup>
    <AllProjects>
      <Properties>Configuration=Release;Platform=Win32</Properties>
    </AllProjects>
  </ItemDefinitionGroup>

  <PropertyGroup>
    <TargetProject>NativeDll1\NativeDll1.vcxproj</TargetProject>
  </PropertyGroup>

  <!-- Build in batches defined by ParallelBuildLevel metadata created by ResolveDependants task -->
  <Target Name="Build" DependsOnTargets="_ResolveBuildOrder" Inputs="@(ProjectBuildOrder)" Outputs="%(ParallelBuildLevel)\NeverExists">
    <Exec Command='"$(MSBuildBinPath)\MSBuild.exe" "%(ProjectBuildOrder.FullPath)" "/Property:%(ProjectBuildOrder.Properties)" /Target:NugetInstallUpdate' />
    <MSBuild Projects="@(ProjectBuildOrder)" Targets="Rebuild" BuildInParallel="true"/>
  </Target>

  <Target Name="_ResolveBuildOrder">
    <ResolveDependants DependecyProjects="$(TargetProject)" AllProjects="@(AllProjects)" PackageIdPrefix="Local.">
      <Output TaskParameter="DependantProjectsBuildOrdered" ItemName="ProjectBuildOrder" />
    </ResolveDependants>
    <Message Text="Projects: @(AllProjects)" Importance="low"/>
    <Message Text="Project parallel build in step %(ProjectBuildOrder.ParallelBuildLevel): @(ProjectBuildOrder)"/>
  </Target>
</Project>
~~~~~~~~~~~

## Routine builds

1. MSBuild build.xml target 'Build'. Make sure to set 'TargetProject' property from command line.
