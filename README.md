# Dashy Crashy - Mobile Arcade Racer

## 1. My Approach
My primary goal was to create a vibrant, fast-paced arcade racing experience capable of running at a locked 60 FPS on mobile devices. I focused heavily on procedural generation, shader-based movement, and dynamic scaling difficulty to eliminate expensive physics operations and keep memory usage extremely low.

Instead of physically moving the player car through an endless 3D world, I employed a "treadmill" architecture. The player's car is fundamentally stationary on the Z-axis, while the road, scenery, and traffic cars are mathematically scrolled backward relative to a global `CurrentSpeed` variable. This allowed me to easily implement a progressive difficulty curve, boost mechanics, and crash-recovery slowdowns that stay perfectly synchronized across all game elements.

To enhance the arcade "game feel," I implemented:
*   **Procedural FOV Kicks:** The camera smoothly widens its field of view at high speeds to emphasize velocity.
*   **Dynamic Camera Shake:** A decaying mathematical screen-shake algorithm gives heavy weight to collisions.
*   **Invincibility Blinking:** A retro-style mesh toggling system provides clear feedback and a safe recovery period after a crash.
*   **Arcade Steering:** The player car visually tilts based on horizontal positional deltas.

## 2. Assets & Tools Used
*   **Unity Universal Render Pipeline (URP):** Utilized for its highly optimized mobile rendering path and lightweight post-processing.
*   **Custom Shaders (HLSL):** Wrote highly optimized unlit shaders for scrolling the road and grass, as well as a procedural UI-style indicator light.
*   **Unity New Input System:** Handled mobile touch/swipe inputs cleanly.
*   **Custom Editor Tooling:** Created automated C# Editor Scripts (`ArcadeLightingSetup.cs`, `ArcadeVFXGenerator.cs`) to dynamically configure scene lighting and generate complex VFX prefabs procedurally, avoiding reliance on bulky external asset packs.
*   **Built-in Particle System (Shuriken):** Handled all visual feedback (Speed lines, Dust puffs, Sparks) using lightweight stretched billboards and additive blending.

## 3. Optimization Decisions Taken
I took a very aggressive stance on optimization, tailoring every system specifically for mobile constraints:

*   **Shader-Driven Environment:** The road and grass are static planes. Their movement is driven entirely on the GPU via UV coordinate offsetting, synchronized perfectly with the `GameManager`'s speed.
*   **No Realtime Physics Tracking:** Completely bypassed Unity's dynamic physics engine for movement. The player uses `Is Kinematic = true` and `OnTriggerEnter` instead of `OnCollisionEnter`, calculating spark impact coordinates mathematically via `ClosestPoint` rather than relying on expensive physics contacts.
*   **Procedural Indicator Shaders:** Instead of relying on transparent PNG textures for the car blinkers (which cause overdraw and memory overhead), I wrote `ArcadeIndicator.shader`—a pure mathematical shader that draws soft, glowing circles with 0 textures.
*   **Traffic Object Pooling:** Traffic cars are never destroyed during gameplay. They are instantiated once into a fixed-size array on Start, and when they pass the camera, they are mathematically relocated backward into a random lane at a safe distance.
*   **Zero-Allocation Game Loop:** Ensured that the core update loops perform zero heap allocations to prevent Garbage Collection spikes and micro-stutters.
*   **Unlit/Ambient Aesthetic:** The scene is lit using `AmbientMode.Trilight` (Gradient) and a single directional sun. This creates a vibrant, shadowless "Nintendo/Arcade" aesthetic that drastically reduces mobile draw calls and lighting calculations.
