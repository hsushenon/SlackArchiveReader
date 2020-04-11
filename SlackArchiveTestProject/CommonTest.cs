using Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SlackArchiveTestProject
{
    [TestClass]
    public class CommonTest
    {
        [TestMethod]
        public void TestTimeStampToDatetime()
        {
            double ts = 1585295313.001200;
            DateTime dateTime = Common.TimeStampToDateTime(ts);
            int expectedDay = 27;
            bool result = expectedDay == dateTime.Day;
            Assert.IsTrue(result,
                       String.Format("Expected for '{0}': true; Actual: {1}",
                                     expectedDay, dateTime.Day));
        }
    }
}
