# Elevator Simulation

Unity version: `6000.3.10f1`

This project implements a 2D elevator simulation with 3 independent elevators and 4 floors (`G`, `1`, `2`, `3`). Hall calls are claimed by exactly one elevator, the dispatcher prefers the nearest idle elevator first, then elevators already moving in the correct direction, and finally falls back to the best overall candidate so no request is stranded. Each elevator maintains its own deduplicated queue, continues serving requests in its current direction before reversing, moves smoothly between floor anchors, pauses briefly on arrival, and exposes its queue/state in the UI for easy review.

## Run

1. Open the project in Unity `6000.3.10f1`.
2. Open [SampleScene](/d:/ElevatorSimulation/Assets/Scenes/SampleScene.unity).
3. Press Play.

The runtime bootstrap script builds the full UI automatically when Play starts.

## Reviewer Notes

- Left side: live building view with 3 elevator shafts.
- Right side: hall call buttons for each floor.
- Each elevator shows current floor, direction, and queue preview.
- Each elevator also has its own cab request panel.

## Submission Checklist

- Export the project as a `.unitypackage` after Unity has imported all assets.
- Include `Assets`, `Packages`, and `ProjectSettings`.
- Verify there are no missing script references and the sample scene is included in the export.
