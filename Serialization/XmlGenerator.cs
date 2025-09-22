using System.IO;
using System.Xml;
using System.Xml.Serialization;
using SolidityRDP.Models.PetriNet;

namespace SolidityRDP.Serialization
{
    public class XmlGenerator
    {
        public void Serialize(PnmlDocument pnml, string outputPath)
        {
            var serializer = new XmlSerializer(typeof(PnmlDocument));
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");

            using (var writer = new StreamWriter(outputPath))
            {
                serializer.Serialize(writer, pnml, ns);
            }
        }
    }
}
