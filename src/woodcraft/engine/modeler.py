"""CadQuery wrapper for parametric woodworking parts."""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from enum import Enum
from pathlib import Path
from typing import Any

import cadquery as cq

from woodcraft.utils.units import Units


class PartType(str, Enum):
    """Types of woodworking parts."""

    PANEL = "panel"  # Flat board/panel
    BOARD = "board"  # Dimensional lumber
    RAIL = "rail"  # Horizontal frame member
    STILE = "stile"  # Vertical frame member
    SHELF = "shelf"
    TOP = "top"
    BOTTOM = "bottom"
    SIDE = "side"
    BACK = "back"
    DRAWER_FRONT = "drawer_front"
    DRAWER_SIDE = "drawer_side"
    DRAWER_BOTTOM = "drawer_bottom"
    DOOR = "door"
    LEG = "leg"
    APRON = "apron"
    STRETCHER = "stretcher"
    CUSTOM = "custom"


class GrainDirection(str, Enum):
    """Wood grain direction relative to part."""

    LENGTH = "length"  # Grain runs along length
    WIDTH = "width"  # Grain runs along width
    NONE = "none"  # No grain (plywood, MDF)


@dataclass
class Dimensions:
    """Part dimensions."""

    length: float
    width: float
    thickness: float

    def to_dict(self) -> dict[str, float]:
        return {"length": self.length, "width": self.width, "thickness": self.thickness}

    @classmethod
    def from_dict(cls, data: dict[str, float]) -> Dimensions:
        return cls(
            length=data["length"],
            width=data["width"],
            thickness=data["thickness"],
        )


@dataclass
class Part:
    """A woodworking part definition."""

    id: str
    part_type: PartType
    dimensions: Dimensions
    quantity: int = 1
    grain_direction: GrainDirection = GrainDirection.LENGTH
    material: str | None = None
    notes: str = ""
    position: tuple[float, float, float] = (0.0, 0.0, 0.0)
    rotation: tuple[float, float, float] = (0.0, 0.0, 0.0)

    _cad_object: cq.Workplane | None = field(default=None, repr=False)

    def to_dict(self) -> dict[str, Any]:
        return {
            "id": self.id,
            "type": self.part_type.value,
            "dimensions": self.dimensions.to_dict(),
            "quantity": self.quantity,
            "grain_direction": self.grain_direction.value,
            "material": self.material,
            "notes": self.notes,
            "position": list(self.position),
            "rotation": list(self.rotation),
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Part:
        return cls(
            id=data["id"],
            part_type=PartType(data["type"]),
            dimensions=Dimensions.from_dict(data["dimensions"]),
            quantity=data.get("quantity", 1),
            grain_direction=GrainDirection(data.get("grain_direction", "length")),
            material=data.get("material"),
            notes=data.get("notes", ""),
            position=tuple(data.get("position", [0, 0, 0])),
            rotation=tuple(data.get("rotation", [0, 0, 0])),
        )

    def build_cad(self) -> cq.Workplane:
        """Build the CadQuery solid for this part."""
        d = self.dimensions
        # Create box with thickness in Z, width in Y, length in X
        result = cq.Workplane("XY").box(d.length, d.width, d.thickness)

        # Apply position
        if self.position != (0.0, 0.0, 0.0):
            result = result.translate(self.position)

        # Apply rotations (in degrees)
        if self.rotation != (0.0, 0.0, 0.0):
            rx, ry, rz = self.rotation
            if rx:
                result = result.rotate((0, 0, 0), (1, 0, 0), rx)
            if ry:
                result = result.rotate((0, 0, 0), (0, 1, 0), ry)
            if rz:
                result = result.rotate((0, 0, 0), (0, 0, 1), rz)

        self._cad_object = result
        return result


@dataclass
class MaterialSpec:
    """Material specification for a project."""

    species: str = "pine"
    thickness: float = 0.75
    finish: str = "none"

    def to_dict(self) -> dict[str, Any]:
        return {
            "species": self.species,
            "thickness": self.thickness,
            "finish": self.finish,
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> MaterialSpec:
        return cls(
            species=data.get("species", "pine"),
            thickness=data.get("thickness", 0.75),
            finish=data.get("finish", "none"),
        )


@dataclass
class Project:
    """A complete woodworking project."""

    name: str
    units: Units = Units.INCHES
    material: MaterialSpec = field(default_factory=MaterialSpec)
    parts: list[Part] = field(default_factory=list)
    joinery: list[dict[str, Any]] = field(default_factory=list)
    hardware: list[dict[str, Any]] = field(default_factory=list)
    notes: str = ""

    def to_dict(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "units": self.units.value,
            "material": self.material.to_dict(),
            "parts": [p.to_dict() for p in self.parts],
            "joinery": self.joinery,
            "hardware": self.hardware,
            "notes": self.notes,
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Project:
        return cls(
            name=data["name"],
            units=Units(data.get("units", "inches")),
            material=MaterialSpec.from_dict(data.get("material", {})),
            parts=[Part.from_dict(p) for p in data.get("parts", [])],
            joinery=data.get("joinery", []),
            hardware=data.get("hardware", []),
            notes=data.get("notes", ""),
        )

    def get_part(self, part_id: str) -> Part | None:
        """Get a part by ID."""
        for part in self.parts:
            if part.id == part_id:
                return part
        return None

    def add_part(self, part: Part) -> None:
        """Add a part to the project."""
        if self.get_part(part.id) is not None:
            raise ValueError(f"Part with ID '{part.id}' already exists")
        self.parts.append(part)

    def remove_part(self, part_id: str) -> bool:
        """Remove a part by ID. Returns True if removed."""
        for i, part in enumerate(self.parts):
            if part.id == part_id:
                self.parts.pop(i)
                return True
        return False


class ProjectModeler:
    """Manages CadQuery models for woodworking projects."""

    def __init__(self, project: Project):
        self.project = project
        self._assembly: cq.Assembly | None = None

    def build_part(self, part_id: str) -> cq.Workplane:
        """Build a single part's CAD model."""
        part = self.project.get_part(part_id)
        if part is None:
            raise ValueError(f"Part '{part_id}' not found")
        return part.build_cad()

    def build_all_parts(self) -> dict[str, cq.Workplane]:
        """Build CAD models for all parts."""
        return {part.id: part.build_cad() for part in self.project.parts}

    def build_assembly(self) -> cq.Assembly:
        """Build the complete assembly."""
        assy = cq.Assembly()

        for part in self.project.parts:
            solid = part.build_cad()
            assy.add(solid, name=part.id)

        self._assembly = assy
        return assy

    def export_step(self, output_path: Path, part_id: str | None = None) -> Path:
        """Export to STEP format.

        Args:
            output_path: Output file path
            part_id: If provided, export only this part. Otherwise export assembly.
        """
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        if part_id:
            part = self.project.get_part(part_id)
            if part is None:
                raise ValueError(f"Part '{part_id}' not found")
            solid = part.build_cad()
            cq.exporters.export(solid, str(output_path))
        else:
            assy = self.build_assembly()
            assy.save(str(output_path))

        return output_path

    def export_stl(self, output_path: Path, part_id: str | None = None) -> Path:
        """Export to STL format.

        Args:
            output_path: Output file path
            part_id: If provided, export only this part. Otherwise export all.
        """
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        if part_id:
            part = self.project.get_part(part_id)
            if part is None:
                raise ValueError(f"Part '{part_id}' not found")
            solid = part.build_cad()
            cq.exporters.export(solid, str(output_path), exportType="STL")
        else:
            # For STL, we need to combine all parts into one solid
            combined = None
            for part in self.project.parts:
                solid = part.build_cad()
                if combined is None:
                    combined = solid
                else:
                    combined = combined.union(solid)
            if combined:
                cq.exporters.export(combined, str(output_path), exportType="STL")

        return output_path

    def export_dxf(self, output_path: Path, part_id: str, view: str = "top") -> Path:
        """Export a 2D DXF projection of a part.

        Args:
            output_path: Output file path
            part_id: Part to export
            view: Projection view ('top', 'front', 'side')
        """
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        part = self.project.get_part(part_id)
        if part is None:
            raise ValueError(f"Part '{part_id}' not found")

        solid = part.build_cad()

        # Project based on view
        if view == "top":
            projection = solid.section()
        elif view == "front":
            projection = solid.rotate((0, 0, 0), (1, 0, 0), 90).section()
        elif view == "side":
            projection = solid.rotate((0, 0, 0), (0, 1, 0), 90).section()
        else:
            raise ValueError(f"Unknown view: {view}")

        cq.exporters.export(projection, str(output_path), exportType="DXF")
        return output_path

    def save_project(self, output_path: Path) -> Path:
        """Save project definition to JSON."""
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        with open(output_path, "w") as f:
            json.dump(self.project.to_dict(), f, indent=2)

        return output_path

    @classmethod
    def load_project(cls, input_path: Path) -> ProjectModeler:
        """Load project definition from JSON."""
        with open(input_path) as f:
            data = json.load(f)
        project = Project.from_dict(data)
        return cls(project)
