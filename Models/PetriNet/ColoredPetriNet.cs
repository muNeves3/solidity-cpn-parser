using System.Collections.Generic;
using System.Xml.Serialization;

namespace SolidityRDP.Models.PetriNet
{
    public class TextElement
    {
        [XmlElement("text")]
        public string Text { get; set; }
    }

    public class PnmlDocument
    {
        [XmlElement("net")]
        public Net Net { get; set; }
    }

    public class Net
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("type")]
        public string Type { get; set; } = "http://www.pnml.org/version-2009/grammar/pnmlcoremodel";

        [XmlElement("page")]
        public Page Page { get; set; }
    }

    public class Page
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("place")]
        public List<Place> Places { get; set; } = new();

        [XmlElement("transition")]
        public List<Transition> Transitions { get; set; } = new();

        [XmlElement("arc")]
        public List<Arc> Arcs { get; set; } = new();
    }

    public class Place
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("name")]
        public TextElement Name { get; set; }

        // Se esta propriedade for nula, a tag <initialMarking> será omitida
        [XmlElement("initialMarking")]
        public TextElement InitialMarking { get; set; }
    }

    public class Transition
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("name")]
        public TextElement Name { get; set; }

        [XmlElement("guard")]
        public TextElement Guard { get; set; }
    }

    public class Arc
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("source")]
        public string SourceId { get; set; }

        [XmlAttribute("target")]
        public string TargetId { get; set; }

        [XmlElement("inscription")]
        public TextElement Expression { get; set; }
    }
}
