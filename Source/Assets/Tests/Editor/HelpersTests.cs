using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Controller2k.Tests
{
    public class HelpersTests
    {
        GameObject go;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            GameObject.DestroyImmediate(go);
        }

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

        [Test]
        public void GetTopSphereWorldPosition()
        {
            //  y
            // 2|    ___
            //  |   / . \        top sphere center   = (2, 1.5, 0)
            //  |  |\___/|
            // 1|  |  p  |       p = (local)position = (0, 1, 0)
            //  |  |     |
            // 0|___\___/____x   transformedPosition = (2, 0, 0)
            //  0  1  2  3
            //
            Vector3 top = Helpers.GetTopSphereWorldPosition(new Vector3(0, 1, 0), new Vector3(2, 0, 0), 0.5f, 2);
            Assert.That(top, Is.EqualTo(new Vector3(2, 1.5f, 0)));
        }

        [Test]
        public void GetTopSphereWorldPositionSimulated()
        {
            //  y
            // 2|    ___
            //  |   / . \        top sphere center   = (2, 1.5, 0)
            //  |  |\___/|
            // 1|  |  p  |       p = (local)position = (0, 1, 0)
            //  |  |     |
            // 0|___\___/____x   transformedPosition = (2, 0, 0)
            //  0  1  2  3
            //
            go.transform.position = new Vector3(0, 1, 0);
            go.transform.localScale = new Vector3(1, 1, 1);
            Vector3 top = Helpers.GetTopSphereWorldPositionSimulated(go.transform, new Vector3(2, 0, 0), 0.5f, 2);
            Assert.That(top, Is.EqualTo(new Vector3(2, 1.5f, 0)));
        }

        [Test]
        public void GetBottomSphereWorldPosition()
        {
            //  y
            // 2|    ___
            //  |   /   \
            //  |  |     |
            // 1|  | _p_ |       p = (local)position  = (0, 1, 0)
            //  |  |/ . \|       bottom sphere center = (2, 0.5, 0)
            // 0|___\___/____x   transformedPosition  = (2, 0, 0)
            //  0  1  2  3
            //
            Vector3 top = Helpers.GetBottomSphereWorldPosition(new Vector3(0, 1, 0), new Vector3(2, 0, 0), 0.5f, 2);
            Assert.That(top, Is.EqualTo(new Vector3(2, 0.5f, 0)));
        }

        [Test]
        public void GetBottomSphereWorldPositionSimulated()
        {
            //  y
            // 2|    ___
            //  |   /   \
            //  |  |     |
            // 1|  | _p_ |       p = (local)position  = (0, 1, 0)
            //  |  |/ . \|       bottom sphere center = (2, 0.5, 0)
            // 0|___\___/____x   transformedPosition  = (2, 0, 0)
            //  0  1  2  3
            //
            go.transform.position = new Vector3(0, 1, 0);
            go.transform.localScale = new Vector3(1, 1, 1);
            Vector3 top = Helpers.GetBottomSphereWorldPositionSimulated(go.transform, new Vector3(2, 0, 0), 0.5f, 2);
            Assert.That(top, Is.EqualTo(new Vector3(2, 0.5f, 0)));
        }
    }
}
