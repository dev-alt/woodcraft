"""Assembly management for woodworking projects."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

import cadquery as cq

from woodcraft.engine.modeler import Part, Project


@dataclass
class AssemblyConstraint:
    """Defines a constraint between assembly components."""

    part_a_id: str
    part_b_id: str
    constraint_type: str  # 'mate', 'align', 'offset'
    face_a: str = "top"  # Face or edge on part A
    face_b: str = "bottom"  # Face or edge on part B
    offset: float = 0.0

    def to_dict(self) -> dict[str, Any]:
        return {
            "part_a": self.part_a_id,
            "part_b": self.part_b_id,
            "type": self.constraint_type,
            "face_a": self.face_a,
            "face_b": self.face_b,
            "offset": self.offset,
        }


@dataclass
class Assembly:
    """An assembly of parts."""

    name: str
    parts: list[str] = field(default_factory=list)  # Part IDs
    constraints: list[AssemblyConstraint] = field(default_factory=list)
    sub_assemblies: list[Assembly] = field(default_factory=list)
    notes: str = ""

    def to_dict(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "parts": self.parts,
            "constraints": [c.to_dict() for c in self.constraints],
            "sub_assemblies": [sa.to_dict() for sa in self.sub_assemblies],
            "notes": self.notes,
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> Assembly:
        return cls(
            name=data["name"],
            parts=data.get("parts", []),
            constraints=[
                AssemblyConstraint(
                    part_a_id=c["part_a"],
                    part_b_id=c["part_b"],
                    constraint_type=c["type"],
                    face_a=c.get("face_a", "top"),
                    face_b=c.get("face_b", "bottom"),
                    offset=c.get("offset", 0.0),
                )
                for c in data.get("constraints", [])
            ],
            sub_assemblies=[cls.from_dict(sa) for sa in data.get("sub_assemblies", [])],
            notes=data.get("notes", ""),
        )


class AssemblyManager:
    """Manages assembly creation and manipulation."""

    def __init__(self, project: Project):
        self.project = project
        self.assemblies: dict[str, Assembly] = {}
        self._cad_assemblies: dict[str, cq.Assembly] = {}

    def create_assembly(self, name: str, part_ids: list[str] | None = None) -> Assembly:
        """Create a new assembly."""
        if name in self.assemblies:
            raise ValueError(f"Assembly '{name}' already exists")

        # Validate part IDs
        if part_ids:
            for pid in part_ids:
                if self.project.get_part(pid) is None:
                    raise ValueError(f"Part '{pid}' not found in project")

        assembly = Assembly(name=name, parts=part_ids or [])
        self.assemblies[name] = assembly
        return assembly

    def add_part_to_assembly(self, assembly_name: str, part_id: str) -> None:
        """Add a part to an assembly."""
        if assembly_name not in self.assemblies:
            raise ValueError(f"Assembly '{assembly_name}' not found")
        if self.project.get_part(part_id) is None:
            raise ValueError(f"Part '{part_id}' not found in project")

        assembly = self.assemblies[assembly_name]
        if part_id not in assembly.parts:
            assembly.parts.append(part_id)

    def add_constraint(
        self,
        assembly_name: str,
        part_a_id: str,
        part_b_id: str,
        constraint_type: str,
        face_a: str = "top",
        face_b: str = "bottom",
        offset: float = 0.0,
    ) -> None:
        """Add a constraint between two parts in an assembly."""
        if assembly_name not in self.assemblies:
            raise ValueError(f"Assembly '{assembly_name}' not found")

        assembly = self.assemblies[assembly_name]

        # Validate parts are in assembly
        for pid in [part_a_id, part_b_id]:
            if pid not in assembly.parts:
                raise ValueError(f"Part '{pid}' not in assembly '{assembly_name}'")

        constraint = AssemblyConstraint(
            part_a_id=part_a_id,
            part_b_id=part_b_id,
            constraint_type=constraint_type,
            face_a=face_a,
            face_b=face_b,
            offset=offset,
        )
        assembly.constraints.append(constraint)

    def build_cad_assembly(self, assembly_name: str) -> cq.Assembly:
        """Build the CadQuery assembly object."""
        if assembly_name not in self.assemblies:
            raise ValueError(f"Assembly '{assembly_name}' not found")

        assembly = self.assemblies[assembly_name]
        cq_assy = cq.Assembly()

        # Add all parts
        for part_id in assembly.parts:
            part = self.project.get_part(part_id)
            if part:
                solid = part.build_cad()
                cq_assy.add(solid, name=part_id)

        # Build sub-assemblies recursively
        for sub_assy in assembly.sub_assemblies:
            self.assemblies[sub_assy.name] = sub_assy
            sub_cq_assy = self.build_cad_assembly(sub_assy.name)
            cq_assy.add(sub_cq_assy, name=sub_assy.name)

        self._cad_assemblies[assembly_name] = cq_assy
        return cq_assy

    def create_exploded_view(
        self,
        assembly_name: str,
        explosion_factor: float = 2.0,
    ) -> cq.Assembly:
        """Create an exploded view of the assembly.

        Args:
            assembly_name: Name of the assembly
            explosion_factor: Multiplier for part separation
        """
        if assembly_name not in self.assemblies:
            raise ValueError(f"Assembly '{assembly_name}' not found")

        assembly = self.assemblies[assembly_name]
        exploded = cq.Assembly()

        # Calculate centroid
        all_positions = []
        for part_id in assembly.parts:
            part = self.project.get_part(part_id)
            if part:
                all_positions.append(part.position)

        if not all_positions:
            return exploded

        centroid = (
            sum(p[0] for p in all_positions) / len(all_positions),
            sum(p[1] for p in all_positions) / len(all_positions),
            sum(p[2] for p in all_positions) / len(all_positions),
        )

        # Add parts with exploded positions
        for part_id in assembly.parts:
            part = self.project.get_part(part_id)
            if part:
                solid = part.build_cad()

                # Calculate direction from centroid
                dx = part.position[0] - centroid[0]
                dy = part.position[1] - centroid[1]
                dz = part.position[2] - centroid[2]

                # Apply explosion offset
                offset = (
                    dx * (explosion_factor - 1),
                    dy * (explosion_factor - 1),
                    dz * (explosion_factor - 1),
                )

                exploded_solid = solid.translate(offset)
                exploded.add(exploded_solid, name=part_id)

        return exploded

    def get_assembly_parts(self, assembly_name: str) -> list[Part]:
        """Get all parts in an assembly."""
        if assembly_name not in self.assemblies:
            raise ValueError(f"Assembly '{assembly_name}' not found")

        assembly = self.assemblies[assembly_name]
        parts = []

        for part_id in assembly.parts:
            part = self.project.get_part(part_id)
            if part:
                parts.append(part)

        return parts

    def list_assemblies(self) -> list[str]:
        """List all assembly names."""
        return list(self.assemblies.keys())
