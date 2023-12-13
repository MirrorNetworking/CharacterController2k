# CharacterController2k
This is an open source Collide & Slide Character Controller for Unity.

# What Problem does this solve?
First, it's important to understand that there are generally two types of Character Controllers:
- Rigidbody: kind of like GTA with very real physics, can feel a bit sluggish to respond.
- Collide & Slide: full manual control over movement physics, not super realistic but just feels very fun. Kind of like Counter-Strike / Quake where movement is super fast and you can move while jumping, etc.

I prefer Collide & Slide for my projects.
- Unity's built in 'CharacterController' component is collide & slide, based on the PhysX character controller
- This is great, except that Unity doesn't expose the 'sliding down slopes' feature
- If you are standing on slide like a diagonal roof, you'll just stand there and never slide down

Sliding down slopes is absolutely essential for any serious game, so we need something else.
- Unity hired a contractor to implement 'Open Character Controller' - essentially the same, but open and with more control.
- This controller supported 'sliding down slopes', which is great.
- In typical Unity fashion, it's in half baked state and the contractor is long gone, effectively being abandoned.

**CharacterController2k** is a fork of Unity's OpenCharacterController, with several bug fixes, code improvements and some test coverage.

This is used in my uMMORPG and uSURVIVAL assets on the Unity Asset Store.
Unity's original OpenCharacterController is under the '**Unity Companion License**'. As result, so is this one.

# Contributing
PRs with bug fixes are most welcome.
