<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/Avalonia-11.2-8B44AC?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjQiIGhlaWdodD0iMjQiPjwvc3ZnPg==&logoColor=white" alt="Avalonia"/>
  <img src="https://img.shields.io/badge/Python-3.10+-3776AB?style=for-the-badge&logo=python&logoColor=white" alt="Python"/>
  <img src="https://img.shields.io/badge/CadQuery-2.4-FF6B35?style=for-the-badge" alt="CadQuery"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="MIT License"/>
</p>

<h1 align="center">Woodcraft</h1>
<h3 align="center">A Modern CAD Application for Woodworking</h3>

<p align="center">
  Design furniture, optimize cuts, generate bills of materials, and export production-ready drawings &mdash; all from a sleek dark-themed desktop interface built for woodworkers.
</p>

---

## Overview

**Woodcraft** is a purpose-built CAD application for woodworking projects. It combines a cross-platform .NET desktop app with a Python-powered 3D modeling engine to deliver a complete design-to-shop workflow:

- **Design** parts with real woodworking dimensions, grain directions, and material species
- **Visualize** your project in an interactive 3D viewport with auto-layout
- **Optimize** material usage with an intelligent cut list optimizer (bin packing)
- **Estimate** costs with an automatic bill of materials generator
- **Export** production-ready SVG drawings, DXF files, and CSV data

---

## Features

### Project Management
- Create projects from templates (bookshelf, cabinet, table, box, picture frame)
- Full part library with 16 woodworking-specific part types
- Hardware tracking with cost estimation
- JSON-based project files for easy version control
- Keyboard shortcuts for rapid workflow (Ctrl+N/O/S, Ctrl+P to add parts)

### 3D Viewport
- Real-time part visualization with wood grain gradients
- Auto-layout engine that arranges parts in a flowing grid
- Isometric, front, top, and side view presets
- Exploded view mode with configurable explosion factor
- Part selection with highlight overlay and dimension readout

### Technical Drawings
- Orthographic multi-view drawings (top, front, side) with dimension lines
- Configurable scale (0.25x to 2x)
- **SVG export** with title block, dimension annotations, and material info
- **DXF export** with proper layers (TOP, FRONT, SIDE, DIMENSIONS)
- Works fully offline without the CAD engine

### Cut List Optimizer
- First-fit decreasing bin packing algorithm
- Configurable stock dimensions with presets (4'x8', 4'x4', 2'x4', 2'x2')
- Blade kerf compensation
- Automatic part rotation for optimal fit
- Visual sheet layout diagram with piece placement
- Waste percentage calculation
- **SVG export** of optimized layouts

### Bill of Materials
- Automatic cost estimation by wood species:

  | Species | Cost/BF | | Species | Cost/BF |
  |---------|--------:|-|---------|--------:|
  | Pine | $3.50 | | Cherry | $8.50 |
  | Poplar | $4.00 | | Walnut | $12.00 |
  | Red Oak | $6.50 | | Hard Maple | $7.00 |
  | White Oak | $7.50 | | Ash | $6.00 |

- Sheet goods pricing (plywood $45/sheet, MDF $30/sheet)
- Hardware cost tracking with supplier info
- **CSV export** with category breakdown and totals

### Dark CAD Theme
- Professional dark interface inspired by Blender and Fusion 360
- Custom color palette with golden oak (#D4894B) and walnut (#8B5A2B) accents
- 25+ custom SVG path icons for woodworking operations
- Smooth 150ms hover transitions on all interactive elements
- Grid-pattern viewport backgrounds

---

## Architecture

```
woodcraft/
├── src/woodcraft/              # Python CAD Engine
│   ├── engine/                 #   CadQuery 3D modeler, assembly, joinery
│   ├── generators/             #   BOM, cut list, drawing, guide generators
│   ├── tools/                  #   MCP tool handlers (40+ tools)
│   ├── data/                   #   Lumber species, hardware catalogs
│   └── server.py               #   JSON-RPC bridge server
│
├── Woodcraft.Desktop/          # .NET 8 / Avalonia Desktop App
│   └── src/
│       ├── Woodcraft.Core/     #   Models, interfaces, extensions
│       │   ├── Models/         #     Part, Project, Dimensions, Hardware, Joinery
│       │   └── Interfaces/     #     ICadService, IProjectService, IExportService
│       └── Woodcraft.Desktop/  #   UI application
│           ├── Assets/         #     Colors, icons, animations, control styles
│           ├── ViewModels/     #     MVVM ViewModels (10 files)
│           ├── Views/          #     Avalonia XAML views (8 views)
│           └── Services/       #     PythonBridge, ProjectService, ExportService
│
├── tests/                      # Python tests (pytest)
├── examples/                   # Sample projects
└── pyproject.toml              # Python package config
```

### Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **UI Framework** | Avalonia 11.2 | Cross-platform XAML desktop UI |
| **App Framework** | .NET 8 / C# 12 | Application runtime |
| **MVVM** | CommunityToolkit.Mvvm 8.4 | Source-generated observable properties & commands |
| **DI** | Microsoft.Extensions.DependencyInjection 9.0 | Service registration & resolution |
| **3D Engine** | CadQuery 2.4 | Parametric solid modeling (STEP/STL export) |
| **2D Drawings** | DrawSVG + ezdxf | SVG and DXF generation |
| **Bin Packing** | Custom + rectpack | Cut list optimization |
| **Bridge** | JSON-RPC 2.0 | .NET ↔ Python process communication |

### Data Flow

```
User Interaction
       │
       ▼
┌─────────────┐    PropertyChanged    ┌──────────────────┐
│   XAML View  │ ◄──────────────────► │    ViewModel     │
│  (Avalonia)  │    Data Binding      │ (CommunityToolkit)│
└─────────────┘                       └────────┬─────────┘
                                               │
                                    ┌──────────┴──────────┐
                                    ▼                     ▼
                            ┌──────────────┐     ┌───────────────┐
                            │ Local Compute│     │ Python Bridge │
                            │  (Bin Pack,  │     │  (JSON-RPC)   │
                            │   BOM, SVG)  │     └───────┬───────┘
                            └──────────────┘             │
                                                         ▼
                                                 ┌───────────────┐
                                                 │   CadQuery    │
                                                 │ (3D Modeling)  │
                                                 └───────────────┘
```

> The desktop app works fully offline for cut lists, BOM, drawings, and project management. The Python engine is only needed for 3D STEP/STL export and advanced parametric modeling.

---

## Supported Woodworking Types

### Part Types
`Panel` `Board` `Rail` `Stile` `Shelf` `Top` `Bottom` `Side` `Back` `Door` `Leg` `Apron` `Stretcher` `DrawerFront` `DrawerSide` `DrawerBottom`

### Joinery
`Butt` `Miter` `Dado` `Rabbet` `Groove` `Mortise & Tenon` `Through Mortise` `Loose Tenon` `Through Dovetail` `Half-Blind Dovetail` `Sliding Dovetail` `Box Joint` `Biscuit` `Pocket Hole` `Dowel` `Tongue & Groove`

### Grain Direction
`Length` `Width` `None` (sheet goods)

### Export Formats
| Format | Use Case |
|--------|----------|
| **SVG** | Drawings, cut list layouts |
| **DXF** | CAD-compatible technical drawings |
| **CSV** | Bill of materials, spreadsheets |
| **STEP** | 3D models for CNC/CAM (requires Python engine) |
| **STL** | 3D printing / visualization (requires Python engine) |
| **JSON** | Project files, data interchange |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional) Python 3.10+ with CadQuery for 3D engine features

### Build & Run

```bash
# Clone the repository
git clone https://github.com/dev-alt/woodcraft.git
cd woodcraft

# Build the desktop app
cd Woodcraft.Desktop
dotnet build

# Run
dotnet run --project src/Woodcraft.Desktop
```

### Python Engine (Optional)

```bash
# From the project root
pip install -e ".[dev]"

# Start the bridge server (the desktop app connects automatically)
python -m woodcraft.server
```

---

## Project File Format

Woodcraft uses a human-readable JSON format. Here's a simplified example:

```json
{
  "name": "Bookshelf",
  "units": "inches",
  "defaultMaterial": "red_oak",
  "parts": [
    {
      "id": "Left Side",
      "partType": "side",
      "dimensions": { "length": 36.0, "width": 10.0, "thickness": 0.75 },
      "material": "red_oak",
      "grainDirection": "length",
      "quantity": 1
    }
  ],
  "joinery": [
    {
      "type": "dado",
      "partA": "Left Side",
      "partB": "Shelf 1",
      "parameters": { "depth": 0.375, "width": 0.75 }
    }
  ],
  "hardware": [
    { "name": "#8 x 1-1/4 Wood Screws", "quantity": 24, "cost": 0.15 }
  ]
}
```

---

## Desktop App Views

### Project Tree & Part Editor
The left panel shows a categorized tree of all parts grouped by type. Selecting a part opens the property editor on the right with fields for dimensions, material, grain direction, quantity, and notes. Changes propagate instantly to the 3D viewport.

### 3D Viewport
Parts render as wood-grain-textured rectangles with automatic flow layout. The toolbar provides view presets (front, top, side, isometric), exploded view toggle, wireframe mode, and dimension overlays. Selected parts highlight with the accent color.

### Cut List Optimizer
Configure stock dimensions and material, then generate an optimized cutting layout. The visual diagram shows piece placement on each sheet with color-coded parts and dimension labels. Export the layout as SVG for shop use.

### Bill of Materials
Auto-generates a categorized cost breakdown (Lumber, Sheet Goods, Hardware) with per-species pricing. Add hardware items through a dialog. Export the full BOM as CSV for procurement.

### Technical Drawings
Multi-view orthographic drawings with dimension lines, title block, and material callouts. Toggle individual views (top/front/side) and adjust scale. Export as SVG or DXF for CNC or shop reference.

---

<p align="center">
  Built with <a href="https://avaloniaui.net/">Avalonia UI</a> and <a href="https://cadquery.readthedocs.io/">CadQuery</a>
</p>
