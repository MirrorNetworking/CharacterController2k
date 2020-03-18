using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Controller2k.Tests
{
    public class HelpersTests
    {
        // A Test behaves as an ordinary method
        [Test]
        public void IsMoveVectorAlmostZero()
        {
            // it's almost zero if all components are smaller than the threshold
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(0,               0,               0),               Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(0,               0,               Mathf.Epsilon),   Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(0,               Mathf.Epsilon,   0),               Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(Mathf.Epsilon,   Mathf.Epsilon,   0),               Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(Mathf.Epsilon,   Mathf.Epsilon,   Mathf.Epsilon),   Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(Mathf.Epsilon,   Mathf.Epsilon,   Mathf.Epsilon),   Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(-Mathf.Epsilon,  Mathf.Epsilon,   Mathf.Epsilon),   Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(-Mathf.Epsilon,  Mathf.Epsilon,   -Mathf.Epsilon),  Mathf.Epsilon), Is.True);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(Mathf.Epsilon*2, Mathf.Epsilon,   Mathf.Epsilon),   Mathf.Epsilon), Is.False);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(Mathf.Epsilon,   Mathf.Epsilon*2, Mathf.Epsilon),   Mathf.Epsilon), Is.False);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(Mathf.Epsilon,   Mathf.Epsilon,   Mathf.Epsilon*2), Mathf.Epsilon), Is.False);
            Assert.That(Helpers.IsMoveVectorAlmostZero(new Vector3(1,               2,               3),               Mathf.Epsilon), Is.False);
        }
    }
}
