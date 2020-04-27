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
            double ts = 1578098457.000600;
            DateTime dateTime = Common.TimeStampToLocalDateTime(ts);
            DateTime dateTime2 = Common.TimeStampToUTCDateTime(ts);
            int expectedDay = 4;
            bool result = expectedDay == dateTime.Day;
            Assert.IsTrue(result,
                       String.Format("Expected for '{0}': true; Actual: {1}",
                                     expectedDay, dateTime.Day));
        }
    }
}
