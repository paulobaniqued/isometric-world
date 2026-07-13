# Project Overview
- **Game Title**: Project Isometric: Clay-Robot World
- **High-Level Concept**: An interactive, procedurally generated 3D isometric diorama displaying a futuristic world. It features four distinct, roofless indoor areas arranged seamlessly in an L-shaped wing around a central courtyard, using a clean, monochromatic clay-matte art style inspired by industrial design.
- **Players**: Single player (interactive viewer / camera control)
- **Inspiration / Reference Games**: Townscaper, Monument Valley, industrial low-poly isometric dioramas.
- **Tone / Art Direction**: Minimalist, monochromatic (clay-like off-white and light grey), soft shadows, highly readable contours using Screen Space Ambient Occlusion (SSAO).
- **Target Platform**: PC (Standalone Windows)
- **Screen Orientation / Resolution**: Landscape (1920x1080)
- **Render Pipeline**: Universal Render Pipeline (URP) with SSAO Renderer Feature.

---

# Game Mechanics
## Core Gameplay Loop
1. **Generate**: The player clicks "Generate World" or runs the generator to build a brand new randomized L-shaped isometric world.
2. **Observe & Inspect**: The camera rotates, pans, and zooms around the seamless L-shaped diorama, showcasing the tiny, highly detailed activities (robots assembling items, scientists studying, person playing VR).
3. **Interact**: Simple click interaction to highlight objects, trigger tiny procedural animations (e.g., rotating conveyor belt boxes, waving robot arms, flashing computer screens).

## Controls and Input Methods
- **Mouse Drag (Left-Click)**: Rotate camera around the diorama center.
- **Mouse Drag (Right-Click) / WASD**: Pan camera left, right, forward, backward.
- **Scroll Wheel**: Zoom in and out.
- **Spacebar / UI Button**: Regenerate world with a new random seed.

---

# UI
- **In-Game HUD**:
  - **Top-Right Panel**: Controls and shortcuts (WASD to Pan, Drag to Rotate, Scroll to Zoom).
  - **Bottom-Center Button**: "Regenerate World" button styled with clean, minimalist white-on-grey UI elements.
  - **Left Panel**: Selection Inspector (displays info about a clicked area or robot, e.g., "Active Room: Scientific Lab - Studying humanoid prototype").

---

# Key Asset & Context
1. **`Assets/Scripts/ProceduralIsometricGenerator.cs`**:
   The core generation controller that defines the grid layout, places walls, and builds individual rooms procedurally from 3D primitives.
2. **`Assets/Scripts/IsometricCameraController.cs`**:
   Handles standard isometric rotation (yaw/pitch), panning, and orthographic/perspective zooming.
3. **`Assets/Materials/ClayMatte.mat`**:
   A clean, off-white, 100% matte material (Smoothness = 0, Metallic = 0, Base Color = RGB 0.9, 0.9, 0.9) using the URP `Universal Render Pipeline/Lit` shader.
4. **`Assets/Settings/PC_Renderer.asset`**:
   We will configure this file to add the **Screen Space Ambient Occlusion (SSAO)** renderer feature, making corners and crevices pop with soft contact shadows.

---

# Implementation Steps

## Step 1: Configure URP & Ambient Occlusion (SSAO)
- **Description**: Add the SSAO Renderer Feature to `PC_Renderer.asset` to achieve the soft clay look. Adjust settings (Direct Lighting Strength = 0.5, Radius = 0.5, Intensity = 1.2) to make contours extremely visible.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Implement Isometric Camera Controller
- **Description**: Create `Assets/Scripts/IsometricCameraController.cs` to allow orthographic and perspective viewing of the diorama. Set default isometric angles (X = 35.264, Y = 45).
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 3: Create the Clay Matte Material
- **Description**: Create `Assets/Materials/ClayMatte.mat` using the URP Lit shader, set Base Color to #E5E5E5 (off-white), Smoothness = 0, Metallic = 0. This material will be procedurally assigned to every generated object.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

## Step 4: Implement Procedural Base & Walls
- **Description**: Create the core generator in `Assets/Scripts/ProceduralIsometricGenerator.cs`. Write procedural methods to:
  1. Generate an L-shaped foundation grid (e.g. 2 wings: 1x2 rooms and 2x1 rooms meeting at a corner).
  2. Build open-top walls (no roofs) using procedurally scaled cubes, leaving gaps for seamless doorways.
  3. Create an outer tile border with fine seam-lines to mimic a physical modular plastic/clay toy.
- **Assigned role**: developer
- **Dependencies**: Step 3
- **Parallelizable**: No

## Step 5: Implement Procedural Room 1: VR House Room
- **Description**: Add code to `ProceduralIsometricGenerator.cs` to build the Bedroom:
  1. **Bed**: Composed of a frame (cube), mattress (cube), and pillows (cubes).
  2. **Desk & Computer**: Tabletop (cube), legs (cylinders), monitor (cubes), and tower.
  3. **VR Player**: Assemble a low-poly character using a capsule (body), sphere (head), cylinders (limbs) sitting or standing with a rectangular box (VR headset) on their head.
  4. **Window**: Open frame cut out of the wall.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 6: Implement Procedural Room 2: Robot Assembly Room
- **Description**: Add code to `ProceduralIsometricGenerator.cs` to build the Manufacturing Room:
  1. **Conveyor Belt**: High-tech long table structure with cylindrical rollers.
  2. **Two Robot Arms**: Standing bases (cylinders) with multi-jointed arms (angled cylinders/cubes) positioned next to the belt.
  3. **PC Workstation**: Computer monitor and desk with a few worker figures (capsules) standing or sitting.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 7: Implement Procedural Room 3: Scientific Laboratory
- **Description**: Add code to `ProceduralIsometricGenerator.cs` to build the Lab:
  1. **Hanging Humanoid Robot**: A complete humanoid shape (chest, limbs, detailed head) suspended from a ceiling beam/scaffolding with thin wires (cylinders/lines).
  2. **Tables & Lab Devices**: Multiple workbenches with stacked monitor terminals, keyboards, and chemical/technical containers (spheres/cylinders).
  3. **Scientists**: Figures wearing coats (represented by slightly wider capsules) gathered around the hanging robot.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 8: Implement Procedural Room 4: Warehouse Storage
- **Description**: Add code to `ProceduralIsometricGenerator.cs` to build the Warehouse:
  1. **Shelving Units**: High-rise frames with multiple horizontal shelves.
  2. **Boxes & Crates**: Stacked cubes of varying dimensions and rotations on shelves and floors to look natural.
  3. **Humanoid Robot**: An active humanoid standing near the shelves, carrying or reaching for a box.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 9: Implement Procedural Courtyard / Outdoor Transitions
- **Description**: Add outdoor decoration in the L-shape inner corner:
  1. **Low-Poly Trees**: Cone or sphere-clump canopies over cylinder trunks.
  2. **Planters & Benches**: Cubes and cylinders arranged as a natural rest area.
  3. **Paths**: Flat thin tiles leading from doors to the courtyard.
- **Assigned role**: developer
- **Dependencies**: Step 4
- **Parallelizable**: No

## Step 10: Implement Simple Interactive Highlights & UI
- **Description**: Add a simple canvas with a "Generate" button. Implement raycasting from mouse clicks to select props, showing their names in the Selection UI.
- **Assigned role**: developer
- **Dependencies**: Steps 5, 6, 7, 8, 9
- **Parallelizable**: Yes

---

# Verification & Testing
## Automated & Script Verification
1. **Shader Compatibility**: Check console logs to ensure no compilation errors occur on URP Lit shaders or script assemblies.
2. **SSAO Validation**: Visually confirm that soft ambient occlusion shadows appear at the base of walls and around characters in the Scene View.

## Manual Playmode Verification Cases
1. **Regeneration Test**: Run the game and click "Regenerate World" multiple times. Verify that all rooms generate cleanly without overlapping walls or floating items, and that the layout respects the L-shape.
2. **Camera Navigation**: Verify that dragging the mouse rotates the camera smoothly, right-clicking pans, and the scroll wheel zooms in/out.
3. **No Roofs Verification**: View the diorama from above and rotate 360 degrees. Confirm that all four areas are completely open from the top, allowing a full view of the furniture, people, and robots inside.
4. **Interaction Selection**: Click on the conveyor belt, the hanging robot, and the bed to ensure the UI updates with the correct area name.
