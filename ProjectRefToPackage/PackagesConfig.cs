using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ProjectRefToPackage
{
    class PackagesConfig
    {
        string packagesConfigFile_;
        XmlDocument xmlDoc_;

        public string PackageIdPrefix { get; internal set; }

        public PackagesConfig(string file)
        {
            packagesConfigFile_ = file;
        }

        public string Id
        {
            get
            {
                return PackageIdPrefix + Path.GetFileName(Path.GetDirectoryName(packagesConfigFile_));
            }
        }

        private List<string> dependencies_;
        public IEnumerable<string> Dependencies
        {
            get
            {
                if (dependencies_ != null)
                {
                    return dependencies_;
                }

                dependencies_ = new List<string>();
                if ((xmlDoc_ == null) && !File.Exists(packagesConfigFile_))
                {
                    return dependencies_;
                }

                if (xmlDoc_ == null)
                {
                    xmlDoc_ = new XmlDocument();
                    xmlDoc_.Load(packagesConfigFile_);
                }

                foreach (XmlNode n in xmlDoc_.DocumentElement.ChildNodes)
                {
                    if ((n is XmlElement) && (n.LocalName.Equals("package")))
                    {
                        string dep = n.Attributes["id"]?.Value;
                        if (!string.IsNullOrEmpty(dep))
                        {
                            dependencies_.Add(dep);
                        }
                    }
                }

                return dependencies_;
            }
        }

        private List<string> impliedDependencies_;
        /// <summary>
        /// Recursively detect all dependencies
        /// </summary>
        /// <param name="allProjPackages"></param>
        /// <returns></returns>
        public List<string> ResolveRecursiveDependencies(Dictionary<string, PackagesConfig> allProjPackages)
        {
            if (impliedDependencies_ != null)
            {
                return impliedDependencies_;
            }

            impliedDependencies_ = new List<string>(Dependencies);

            foreach (string dep in Dependencies)
            {
                // Dependency is not a local project or has no dependencies of its own?
                if (!allProjPackages.ContainsKey(dep))
                {
                    continue;
                }

                List<string> depDep = allProjPackages[dep].ResolveRecursiveDependencies(allProjPackages);
                if (depDep.Contains(Id))
                {
                    throw new Exception($"Cyclic dependency discovered {Id} <--> {dep}");
                }

                impliedDependencies_.AddRange(depDep);
            }
            
            return impliedDependencies_;
        }

        public void EnsureExists()
        {
            if (xmlDoc_ != null)
            {
                return;
            }

            if (File.Exists(packagesConfigFile_))
            {
                xmlDoc_ = new XmlDocument();
                xmlDoc_.Load(packagesConfigFile_);
                return;
            }

            xmlDoc_ = new XmlDocument();
            XmlElement root = xmlDoc_.CreateElement("packages");
            xmlDoc_.AppendChild(root);
        }

        public void Add(string packageId)
        {
            EnsureExists();

            foreach (XmlNode n in xmlDoc_.DocumentElement.ChildNodes)
            {
                if ((n is XmlElement) && (n.LocalName.Equals("package")) && (n.Attributes["id"]?.Value?.Equals(packageId, StringComparison.OrdinalIgnoreCase) == true))
                {
                    return;
                }
            }

            XmlElement xElem = xmlDoc_.CreateElement("package");
            xElem.SetAttribute("id", packageId);
            xElem.SetAttribute("version", "0.0.0.0");
            xmlDoc_.DocumentElement.AppendChild(xElem);
            dependencies_ = null; // Invalidate dependency list.
            impliedDependencies_ = null;
        }

        public void Save()
        {
            EnsureExists();
            xmlDoc_.Save(packagesConfigFile_);
        }
    }
}
