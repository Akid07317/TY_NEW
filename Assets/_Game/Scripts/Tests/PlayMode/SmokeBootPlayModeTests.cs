using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace CampusRPG.Tests.PlayMode
{
    public sealed class SmokeBootPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayModeTestAssemblyCompilesAndRuns()
        {
            yield return null;
            Assert.Pass();
        }
    }
}
