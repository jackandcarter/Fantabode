# Fantabode

Fantabode is a Dalamud plugin that provides powerful housing tools for Final Fantasy XIV.

## Features

- **Precise item controls** – Adjust the active furnishing's X, Y, Z coordinates and rotation with drag boxes or direct input. Lock individual axes or copy and paste saved positions.
- **Place Anywhere** – Override the game's placement restrictions to position items freely.
- **3D Gizmo** – Optional gizmo overlay for moving or rotating the active item. The gizmo can also drive group edits.
- **Grouping** – Capture multiple selected furnishings and apply transformations to them with pivots based on the first item, the selection center, or the bounding box.
- **Furnishing list** – View nearby furnishings with icons and distance indicators. Select entries to target a specific item, with optional sorting by distance.
- **Chat commands** – Use `/fantabode` to toggle the UI. `/fantabode list` opens the furnishing list, `/fantabode debug` shows the debug window, and `/fantabode reset` repositions the main window. Coordinates may also be supplied: `/fantabode x y z [rotation]` moves the selected item.
- **Auto open option** – Automatically show the UI when housing editing begins.

## Usage

1. In game, run `/fantabode` to open the main window.
2. Use the **Controls** tab to move the active item or toggle features such as *Place Anywhere* or the gizmo.
3. Switch to the **Groups** tab, select furnishings to capture, choose a pivot, and create a group. Manipulate the preview with the gizmo and press **Apply Group** to move all items.
4. Run `/fantabode list` for a sortable list of nearby furnishings. Click an entry to select that furnishing.
5. Provide precise coordinates via chat: `/fantabode x y z [rotation]`.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

