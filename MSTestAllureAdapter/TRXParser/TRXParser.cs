﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MSTestAllureAdapter
{
    /// <summary>
    /// TRX parser.
    /// Based on the trx2html parser code: http://trx2html.codeplex.com/ 
    /// </summary>
    public class TRXParser
    {
        private readonly XNamespace mTrxNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        /// <summary>
        /// The category given to tests without any category.
        /// </summary>
        public static readonly string DEFAULT_CATEGORY = "NO_CATEGORY";

        /*
        ErrorInfo ParseErrorInfo(XElement r)
        {
            ErrorInfo err = new ErrorInfo();
            if (r.Element(ns + "Output") != null && 
                r.Element(ns + "Output").Element(ns + "ErrorInfo") != null &&
                r.Element(ns + "Output").Element(ns + "ErrorInfo").Element(ns + "Message") != null )
                {
                    err.Message = r.Element(ns + "Output").Element(ns + "ErrorInfo").Element(ns + "Message").Value;

                }

            if (r.Descendants(ns + "StackTrace").Count()> 0 )
                {
                    err.StackTrace = r.Descendants(ns + "StackTrace").FirstOrDefault().Value;
                }

            if (r.Descendants(ns + "DebugTrace").Count() > 0)
                {
                    err.StdOut = r.Descendants(ns + "DebugTrace").FirstOrDefault().Value.Replace("\r\n", "<br />");
                }

            return err;
        }
        */
       


        public IEnumerable<MSTestResult> GetTestResults(string filePath)
        {
            XDocument doc = XDocument.Load(filePath);
            XNamespace ns = mTrxNamespace;

            string testRunName = doc.Document.Root.Attribute("name").Value;
            string runUser = doc.Document.Root.Attribute("runUser").Value;

            IEnumerable<XElement> unitTests = doc.Descendants(ns + "UnitTest").ToList();           

            IEnumerable<XElement> unitTestResults = doc.Descendants(ns + "UnitTestResult").ToList();

            Func<XElement, string> outerKeySelector = _ => _.Element(ns + "Execution").Attribute("id").Value;
            Func<XElement, string> innerKeySelector = _ => _.Attribute("executionId").Value;
            Func<XElement, XElement, MSTestResult> resultSelector = CreateMSTestResult;

            IEnumerable<MSTestResult> result = 
                unitTests.Join<XElement, XElement, string, MSTestResult>(unitTestResults, outerKeySelector, innerKeySelector, resultSelector, null);

            return result;
        }

        private MSTestResult CreateMSTestResult(XElement unitTest, XElement unitTestResult)
        {
            string testName = unitTest.GetSafeAttributeValue(mTrxNamespace + "TestMethod", "name");
            TestOutcome outcome = (TestOutcome)Enum.Parse(typeof(TestOutcome), unitTestResult.Attribute("outcome").Value);
            DateTime start = DateTime.Parse(unitTestResult.Attribute("startTime").Value);
            DateTime end = DateTime.Parse(unitTestResult.Attribute("endTime").Value);
            string[] categories = (from testCategory in unitTest.Descendants(mTrxNamespace + "TestCategoryItem")
                                            select testCategory.GetSafeAttributeValue("TestCategory")).ToArray<string>();
            if (categories.Length == 0)
                categories = new string[]{ "NO_CATEGORY" };

            return new MSTestResult(testName, outcome, start, end, categories);
        }
    }

    internal static class XElementExtensions
    {
        public static string GetSafeValue(this XElement element, XName name)
        {
            string result = String.Empty;

            element = element.Element(name);

            if (element != null)
            {
                result = element.Value;
            }

            return result;
        }

        public static string GetSafeAttributeValue(this XElement element, XName name, XName attributeName)
        {
            string result = String.Empty;

            element = element.Element(name);

            if (element != null && element.Attribute(attributeName) != null)
            {
                result = element.Attribute(attributeName).Value;
            }

            return result;
        }

        public static string GetSafeAttributeValue(this XElement element, XName attributeName)
        {
            string result = String.Empty;

            if (element != null && element.Attribute(attributeName) != null)
            {
                result = element.Attribute(attributeName).Value;
            }

            return result;
        }

        public static TimeSpan ParseDuration(this XElement element, string attributeName)
        {
            TimeSpan result = new TimeSpan(0);

            XAttribute attribute = element.Attribute(attributeName);

            if (attribute != null)
            {
                result = TimeSpan.Parse(attribute.Value);
            }

            return result;
        }
    }
}

