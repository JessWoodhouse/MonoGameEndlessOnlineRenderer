# EMF Map Viewer

MonoGame viewer for Endless Online maps with isometric rendering.

## Quick Start

1. Install MonoGame from [monogame.net](https://www.monogame.net/downloads/)
2. `dotnet build && dotnet run`
3. Drop `.emf` files in `maps/` folder, `.egf` graphics in `gfx/` folder

Arrow keys navigate, Page Up/Down switch maps.

## Controls

### Navigation
- **Arrow Keys** or **WASD** - Move camera around the map
- **Mouse Drag** - Pan the camera by clicking and dragging
- **Spacebar** - Center camera on map

### Map Management
- **E** - Load previous map
- **Q** - Load next map
- **R** - Clear graphics cache (useful for development)
- **Escape** - Exit application

## File Formats

### EMF Files (.emf)
Map files containing:
- Map dimensions and metadata
- 9 layers of tile data with positioning
- NPC, chest, and warp information
- Fill tile specifications

### EGF Files (.egf)
Graphics files containing:
- Embedded bitmap data in DIB format
- Support for 8-bit, 16-bit, 24-bit, and 32-bit color depths
- Automatic transparency for black pixels (RGB 0,0,0)

## Layer Rendering Order

The viewer renders layers in this specific order for proper depth sorting:

1. **Base Layer** (Layer 0) - Ground tiles and fill tiles
2. **Shadow Layer** (Layer 7) - Semi-transparent shadows
3. **Main Layers** (Layers 6, 1, 3, 4, 2, 5) - Objects, walls, decorations
4. **Top Layer** (Layer 8) - Overhead elements like roofs

## Technical Details

### Graphics Loading
- Direct parsing of DIB bitmap headers from .egf files
- Efficient texture caching system
- Proper handling of different color depths and palettes
- Automatic transparency conversion for sprite edges

### Map Rendering
- Isometric projection with 32x16 pixel cell dimensions
- Multi-pass rendering for correct layer ordering
- Optimized diagonal traversal for proper depth sorting
- Camera-relative positioning for smooth scrolling

## Development

### Building from Source
```bash
dotnet restore
dotnet build
dotnet run
```

### Adding Maps
1. Place .emf files in the `maps/` folder
2. Ensure corresponding .egf graphics files are in the `gfx/` folder
3. Restart the application to detect new maps

### Cache Management
The application automatically caches loaded textures for performance. Use the **R** key to clear the cache if you're modifying graphics files during development.

## Troubleshooting

### No Maps Found
- Ensure .emf files are in the `maps/` folder
- Check that the maps folder exists in the same directory as the executable

### Missing Graphics
- Verify .egf files are in the `gfx/` folder
- Check that graphics file numbers match the map requirements

### Performance Issues
- Use **R** to clear graphics cache
- Ensure graphics files aren't corrupted
- Check system memory usage

