# react terraria data picker

React and Vite web app that lets you pick and save your favorite Terraria weapons on a custom 2D grid. The grid is dynamic letting users change the rows and columns to sort weapons by class, rarity, subclass, or game progression tier.

Import and Export CSV tool was developed to make it easier for developers to change or add weapon datas as needed.

Website Link https://soycakes.github.io/TerrariaWeapons/  
Repo Link https://github.com/Soycakes/TerrariaWeapons

# Tech Stack
* **Frontend:** React, Vite, Tailwind CSS, inline styling
* **State and Storage:** Client side state using local storage to save your grid picks
* **Data Tools:** Node JS scripts to parse CSV data and convert it to React data files

# Showcased Scripts

### [App.jsx](./App.jsx)
* Main app state and table.
* Saves and loads your tables from local storage.
* Tracks which cells should be shown as empty when there are no matching weapons.

### [components/Grid.jsx](./components/Grid.jsx)
* Handles the table layout and sets a fixed width to keep the grid cells aligned.
* Displays the weapon sprites and handles cell click events to open the modal.

### [components/CellModal.jsx](./components/CellModal.jsx)
* A search modal that lists weapons matching the active row and column filters.
* Auto focuses the search input on open and closes when you press the Escape key.

### [components/Sprite.jsx](./components/Sprite.jsx)
* Renders pixel art weapon sprites from file using item ID.
* Runs a fallback chain to load local images first, then wiki images, and finally a missing item.

### [tools/import.js](./tools/import.js) & [tools/export.js](./tools/export.js)
* Import reads script that parses the weapons CSV spreadsheet.
* Export outputs CVS file to allow importing into spreadsheet.
* Auto generates JS files for the React app and splits custom metadata columns.

### [config/categories.js](./config/categories.js)
* Config to actually add categories to fill in Row/Columns for the tables.
* A dynamic approach was done to make it easier to add custom names and colors to texts within the categories itself instead of having to change the names of all data.

### weapons, weaponsExtra, weaponsNew
* To allow for easier separation of older, newer data, we separated any incoming extra data to "Extra" and "New"
* weaponsExtra caontains actual new custom categories added in import
* weaponsNew contains new items or weapons in import
