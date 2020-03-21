using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Controller2k.Tests
{
    public class CharacterController2kTests
    {
        [Test]
        public void IsSlideableAngle()
        {
            // 90 degree is too steep to slide
            Assert.That(CharacterController2k.IsSlideableAngle(90, 45), Is.False);

            // 0 degree is too flat to slide
            Assert.That(CharacterController2k.IsSlideableAngle(0, 45), Is.False);

            // 30 degree is something but not enough for sliding
            Assert.That(CharacterController2k.IsSlideableAngle(30, 45), Is.False);

            // 50 degree is > 45, so we should slide
            Assert.That(CharacterController2k.IsSlideableAngle(50, 45), Is.True);

            // 85 degree is really steep, but still valid for sliding
            Assert.That(CharacterController2k.IsSlideableAngle(85, 45), Is.True);
        }
    }
}
