# React Terraria Data Picker

React and Vite web app that lets users pick and save their favorite Terraria weapons on a custom 2D grid. The grid is dynamic, allowing users to change the rows and columns to sort weapons by class, rarity, subclass, or game progression tier.

Import and Export CSV tools were developed to make it easy for developers to add or update weapon data as needed.

**Live Website (Try it out!):** [https://soycakes.github.io/TerrariaWeapons/](https://soycakes.github.io/TerrariaWeapons/)  
**Original Repository:** [https://github.com/Soycakes/TerrariaWeapons](https://github.com/Soycakes/TerrariaWeapons)

# Tech Stack
* **Frontend:** React, Vite, Tailwind CSS, inline styling
* **State & Storage:** Client side state using `localStorage` for persistent grid choices
* **Data Tools:** Node.js scripts to parse CSV spreadsheet data and generate React data structures

# Showcased Scripts

### [App.jsx](./App.jsx)
* Main app state and table controller.
* Saves and loads table configs from `localStorage`.
* Tracks which cells are locked when there are no matching weapons.

### [components/Grid.jsx](./components/Grid.jsx)
* Handles table layout and sets fixed cell dimensions.
* Displays the weapon sprites and handles cell click events to open the selection modal.

### [components/CellModal.jsx](./components/CellModal.jsx)
* Search modal that lists weapons matching the active row and column filters.
* Auto focuses search input on open and closes on Escape key.

### [components/Sprite.jsx](./components/Sprite.jsx)
* Renders pixel art weapon sprites by item ID.
* Runs a fallback chain loading local assets first, wiki images second, and a missing item placeholder if all fails.

### [tools/import.js](./tools/import.js) & [tools/export.js](./tools/export.js)
* import.js parses raw CSV spreadsheets and auto generates JavaScript data files for the React app.
* export.js exports grid data back out to CSV format for external spreadsheet editing.

### [config/categories.js](./config/categories.js)
* Config to add categories for table rows and columns.
* A dynamic approach to customize category labels and text colors without changing the saved data.

### weapons, weaponsExtra, weaponsNew
* To allow for easier separation of older, newer data, we separated any incoming extra data to "Extra" and "New"
* weaponsExtra caontains actual new custom categories added in import
* weaponsNew contains new items or weapons in import
