using UnityEngine;
using System.Collections;
using NUnit.Framework;

namespace CosmosEngineTest
{
    [TestFixture]
    public class CToolTest
    {
        [Test]
        public void WaveRandomNumber()
        {
            var n1 = CTool.GetWaveRandomNumber("12");
            Assert.AreEqual(n1, 12);

            var n2 = CTool.GetWaveRandomNumber("10~11");
            Assert.IsTrue(n2 >= 10 && n2 <= 11);
        }
    }
}
