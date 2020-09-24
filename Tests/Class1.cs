using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DXML;
using DXML.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class Tests
    {
        private string flowerText => new System.Net.WebClient().DownloadString("https://www.w3schools.com/xml/plant_catalog.xml");

        [TestMethod]
        public void TestParserParsesWithoutError()
        {
            var xml = DynamicXml.Parse(flowerText);
        }

        [TestMethod]
        public void TestObjectCanBeEnumerated()
        {
            var xml = DynamicXml.Parse(flowerText);

            Assert.IsTrue(xml.Any(x => x["COMMON"].ToString() == "Mayapple" && x["BOTANICAL"].ToString() == "Podophyllum peltatum"));
        }

        public class Flower
        {
            public string COMMON;
            public string BOTANICAL;
            public string ZONE;
            public string LIGHT;
            public string PRICE;
            public string AVAILABILITY;
        }
        
        [TestMethod]
        public void TestObjectCanBeDeserialized()
        {
            var xml = DynamicXml.Parse(flowerText);
            var list = xml.Select(x => x.Deserialize<Flower>());

            Assert.IsTrue(list.Any(x => x.COMMON == "Mayapple" && x.BOTANICAL == "Podophyllum peltatum"));
        }
    }
}
