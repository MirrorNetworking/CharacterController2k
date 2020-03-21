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

        [Test]
        public void CalculateSlideVerticalVelocity()
        {
            // create a normal for a 45 degree angled slope
            // (= equal parts up and to the right)
            Vector3 slopeNormal = new Vector3(1, 1, 0).normalized;

            // zero if time spent sliding is 0
            Assert.That(CharacterController2k.CalculateSlideVerticalVelocity(slopeNormal, 0, 1, 1), Is.EqualTo(0));

            // half of 'gravity' down after sliding 1 seconds in a 45 degree angle
            // (the angle determines the speed, and 45 is half way between 0 and 90)
            Assert.That(CharacterController2k.CalculateSlideVerticalVelocity(slopeNormal, 1, 1, 10), Is.EqualTo(Physics.gravity.y/2));

            // exactly 'gravity' down after sliding 2 seconds in a 45 degree angle
            // (the angle determines the speed, and 45 is half way between 0 and 90)
            Assert.That(CharacterController2k.CalculateSlideVerticalVelocity(slopeNormal, 2, 1, 10), Is.EqualTo(Physics.gravity.y));

            // exactly 'gravity' down after sliding 1 seconds in a 45 degree angle
            // with 2x multiplier
            Assert.That(CharacterController2k.CalculateSlideVerticalVelocity(slopeNormal, 2, 1, 10), Is.EqualTo(Physics.gravity.y));

            // test max speed too
            Assert.That(CharacterController2k.CalculateSlideVerticalVelocity(slopeNormal, 1, 1, 1), Is.EqualTo(-1));
        }
    }
}
