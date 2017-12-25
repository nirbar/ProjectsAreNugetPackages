# ProjectsAreNugetPackages

- Build a nuget package after having built the project.
- Consume project dependencies as nuget packages after a one-time migration

## Custom Properties

- BuildNugetPackage: true/false. Whether or not to build a Nuget project after build
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