﻿using NUnit.Framework;
using System.Collections.Generic;
using TerritoryTools.Common.AddressParser.Smart;

namespace TerritoryTools.Common.AddressParser.Tests.Smart
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void Parse_City_Name_Null()
        {
            Assert.IsNull(Test(null).City.Name);
        }

        [Test]
        public void Parse_Street_Number_StreetOnly()
        {
            Assert.AreEqual("123", Test("123 Main St").Street.Number.ToString());
        }

        [Test]
        public void Parse_Street_Name_StreetOnly()
        {
            Assert.AreEqual("Main St", Test("123 Main St").Street.Name.ToString());
        }

        //[Test]
        //public void Parse_Commas_StreetCityRegion()
        //{
        //    string text = "123 Main St, Seattle, WA";
        //    Assert.AreEqual("Main St", Test(text).Street.Name.ToString());
        //    Assert.AreEqual("Seattle", Test(text).City.Name);
        //    Assert.AreEqual("WA", Test(text).Region.Code);
        //}

        [Test]
        public void Parse_Street_TypelessName_wCityAndRegion()
        {
            string text = "123 Broadway Everett WA";
            Assert.AreEqual("Broadway", Test(text).Street.Name.ToString());
            Assert.AreEqual("Everett", Test(text).City.Name);
            Assert.AreEqual("WA", Test(text).Region.Code);
        }

        [Test]
        public void Parse_Street_TypelessName_TwoWordCityAndRegion()
        {
            string text = "123 Main North Bend WA";
            Assert.AreEqual("Main", Test(text).Street.Name.ToString());
            Assert.AreEqual("North Bend", Test(text).City.Name);
            Assert.AreEqual("WA", Test(text).Region.Code);
        }

        [Test]
        public void Parse_Street_TypelessName_OneWordCityAndRegion()
        {
            string text = "123 Main South Bend WA";
            Assert.AreEqual("Main South", Test(text).Street.Name.ToString());
            Assert.AreEqual("Bend", Test(text).City.Name);
            Assert.AreEqual("WA", Test(text).Region.Code);
        }

        [Test]
        public void Parse_NonStreet_PO_Box()
        {
            AssertStreetNumberName("POB 321", "321", "POB");
            AssertStreetNumberName("PO Box 321", "321", "PO Box");
            AssertStreetNumberName("P O B 321", "321", "P O B");
            AssertStreetNumberName("P.O.Box 321", "321", "P.O.Box");
            AssertStreetNumberName("P.O. Box 321", "321", "P.O. Box");
            AssertStreetNumberName("P. O. B. 321", "321", "P. O. B.");
            AssertStreetNumberName("P. O. Box 321", "321", "P. O. Box");
            AssertStreetNumberName("Post Office Box 321", "321", "Post Office Box");
        }

        [Test]
        public void Parse_NonStreet_Lot_Number()
        {

            AssertStreetNumberName("Lot 321", "321", "Lot");
        }

        [Test]
        public void Parse_NonStreet_Post_Office_Barn_Fails()
        {

            AssertStreetNumberName("Post Office Barn 321", string.Empty, string.Empty);
        }

        [Test]
        public void Parse_Region_Code_Normal()
        {
            Assert.AreEqual("WA", Test("123 Main St Lynnwood WA 98087").Region.Code.ToString());
        }

        [Test]
        public void Parse_Postal_Code_Normal()
        {
            Assert.AreEqual("98087", Test("123 Main St Lynnwood WA 98087").Postal.Code.ToString());
        }

        Address Test(string text)
        {
            var parser = new Parser(
                new List<string> { 
                    "Seattle",
                    "Everett",
                    "North Bend", 
                    "Lake Forest Park" });

            return parser.Parse(text);
        }

        void AssertStreetNumberName(string text, string streetNumber, string streetName)
        {
            Assert.AreEqual(streetName, Test(text).Street.Name.Name.ToString());
            Assert.AreEqual(streetNumber, Test(text).Street.Number.ToString());
        }
    }
}