using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace HomeworkLeak
{
    class Program
    {
        static void Main(string[] args)
        {
            var rand = new Random();
            while (true)
            {
                var obj = new XMLObj() { Nodes = new List<XMLNode>() { new() { Value = rand.Next(int.MaxValue) } } };
                var xmlSerializer = new XmlSerializer(typeof(XMLObj), new XmlRootAttribute("rootNode"));
                using (StringWriter textWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, obj);
                    Console.WriteLine(textWriter.ToString());
                }
                Thread.Sleep(100);
            }
        }
    }

    [Serializable]
    public class XMLObj
    {
        [XmlElement("block")]
        public List<XMLNode> Nodes { get; set; }
    }

    [Serializable]
    public class XMLNode
    {
        public int Value { get; set; }
    }
}
