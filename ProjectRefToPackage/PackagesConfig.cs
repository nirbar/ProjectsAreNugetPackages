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

        public PackagesConfig(string file)
        {
            packagesConfigFile_ = file;
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
                if ((n is XmlElement) && (n.LocalName.Equals("package")) && (n.Attributes["id"]?.Name?.Equals(packageId, StringComparison.OrdinalIgnoreCase) == true))
                {
                    return;
                }
            }

            XmlElement xElem = xmlDoc_.CreateElement("package");
            xElem.SetAttribute("id", packageId);
            xElem.SetAttribute("version", "0.0.0.0");
            xmlDoc_.DocumentElement.AppendChild(xElem);
        }

        public void Save()
        {
            EnsureExists();
            xmlDoc_.Save(packagesConfigFile_);
        }
    }
}
