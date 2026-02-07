"""Project management MCP tools."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from woodcraft.engine.modeler import (
    Dimensions,
    GrainDirection,
    MaterialSpec,
    Part,
    PartType,
    Project,
    ProjectModeler,
)
from woodcraft.utils.units import Units


class ProjectManager:
    """Manages active projects in the server."""

    def __init__(self, workspace_dir: Path | None = None):
        self.workspace_dir = workspace_dir or Path.cwd() / "woodcraft_projects"
        self.workspace_dir.mkdir(parents=True, exist_ok=True)
        self._projects: dict[str, Project] = {}
        self._active_project: str | None = None

    def get_project(self, name: str | None = None) -> Project | None:
        """Get a project by name, or the active project."""
        if name:
            return self._projects.get(name)
        if self._active_project:
            return self._projects.get(self._active_project)
        return None

    def set_active(self, name: str) -> None:
        """Set the active project."""
        if name not in self._projects:
            raise ValueError(f"Project '{name}' not found")
        self._active_project = name

    def add_project(self, project: Project) -> None:
        """Add a project to the manager."""
        self._projects[project.name] = project
        if self._active_project is None:
            self._active_project = project.name

    def list_projects(self) -> list[str]:
        """List all project names."""
        return list(self._projects.keys())

    def get_project_dir(self, project_name: str) -> Path:
        """Get the directory for a project."""
        safe_name = "".join(c if c.isalnum() or c in "-_" else "_" for c in project_name)
        project_dir = self.workspace_dir / safe_name
        project_dir.mkdir(parents=True, exist_ok=True)
        return project_dir


class ProjectTools:
    """MCP tools for project management."""

    def __init__(self, manager: ProjectManager):
        self.manager = manager

    def create_project(
        self,
        name: str,
        units: str = "inches",
        material_species: str = "pine",
        material_thickness: float = 0.75,
        material_finish: str = "none",
        notes: str = "",
    ) -> dict[str, Any]:
        """Create a new woodworking project.

        Args:
            name: Project name
            units: Unit system ('inches', 'mm', 'cm')
            material_species: Default wood species
            material_thickness: Default material thickness
            material_finish: Default finish type
            notes: Project notes

        Returns:
            Project summary
        """
        if self.manager.get_project(name):
            return {"error": f"Project '{name}' already exists"}

        project = Project(
            name=name,
            units=Units(units),
            material=MaterialSpec(
                species=material_species,
                thickness=material_thickness,
                finish=material_finish,
            ),
            notes=notes,
        )

        self.manager.add_project(project)

        return {
            "status": "created",
            "project": {
                "name": project.name,
                "units": project.units.value,
                "material": project.material.to_dict(),
            },
            "project_dir": str(self.manager.get_project_dir(name)),
        }

    def get_project_info(self, name: str | None = None) -> dict[str, Any]:
        """Get information about a project.

        Args:
            name: Project name (uses active project if not specified)

        Returns:
            Project details
        """
        project = self.manager.get_project(name)
        if not project:
            return {"error": "No project found"}

        return {
            "name": project.name,
            "units": project.units.value,
            "material": project.material.to_dict(),
            "num_parts": len(project.parts),
            "parts": [
                {
                    "id": p.id,
                    "type": p.part_type.value,
                    "dimensions": p.dimensions.to_dict(),
                    "quantity": p.quantity,
                }
                for p in project.parts
            ],
            "num_joints": len(project.joinery),
            "notes": project.notes,
        }

    def list_projects(self) -> dict[str, Any]:
        """List all projects.

        Returns:
            List of project names and active project
        """
        return {
            "projects": self.manager.list_projects(),
            "active": self.manager._active_project,
        }

    def set_active_project(self, name: str) -> dict[str, Any]:
        """Set the active project.

        Args:
            name: Project name to make active

        Returns:
            Status
        """
        try:
            self.manager.set_active(name)
            return {"status": "success", "active_project": name}
        except ValueError as e:
            return {"error": str(e)}

    def add_part(
        self,
        part_id: str,
        part_type: str,
        length: float,
        width: float,
        thickness: float | None = None,
        quantity: int = 1,
        grain_direction: str = "length",
        material: str | None = None,
        notes: str = "",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Add a part to a project.

        Args:
            part_id: Unique identifier for the part
            part_type: Type of part (panel, board, shelf, side, etc.)
            length: Length dimension
            width: Width dimension
            thickness: Thickness (uses project default if not specified)
            quantity: Number of this part needed
            grain_direction: Grain orientation ('length', 'width', 'none')
            material: Material override (uses project default if not specified)
            notes: Part notes
            project_name: Project to add to (uses active if not specified)

        Returns:
            Part summary
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        if thickness is None:
            thickness = project.material.thickness

        try:
            part = Part(
                id=part_id,
                part_type=PartType(part_type),
                dimensions=Dimensions(length=length, width=width, thickness=thickness),
                quantity=quantity,
                grain_direction=GrainDirection(grain_direction),
                material=material or project.material.species,
                notes=notes,
            )
            project.add_part(part)

            return {
                "status": "added",
                "part": part.to_dict(),
            }
        except ValueError as e:
            return {"error": str(e)}

    def remove_part(
        self,
        part_id: str,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Remove a part from a project.

        Args:
            part_id: ID of part to remove
            project_name: Project name (uses active if not specified)

        Returns:
            Status
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        if project.remove_part(part_id):
            return {"status": "removed", "part_id": part_id}
        return {"error": f"Part '{part_id}' not found"}

    def update_part(
        self,
        part_id: str,
        length: float | None = None,
        width: float | None = None,
        thickness: float | None = None,
        quantity: int | None = None,
        notes: str | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Update an existing part.

        Args:
            part_id: ID of part to update
            length: New length (optional)
            width: New width (optional)
            thickness: New thickness (optional)
            quantity: New quantity (optional)
            notes: New notes (optional)
            project_name: Project name (uses active if not specified)

        Returns:
            Updated part info
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        part = project.get_part(part_id)
        if not part:
            return {"error": f"Part '{part_id}' not found"}

        if length is not None:
            part.dimensions.length = length
        if width is not None:
            part.dimensions.width = width
        if thickness is not None:
            part.dimensions.thickness = thickness
        if quantity is not None:
            part.quantity = quantity
        if notes is not None:
            part.notes = notes

        return {"status": "updated", "part": part.to_dict()}

    def save_project(
        self,
        project_name: str | None = None,
        filename: str | None = None,
    ) -> dict[str, Any]:
        """Save project to file.

        Args:
            project_name: Project name (uses active if not specified)
            filename: Output filename (defaults to project name)

        Returns:
            File path
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        project_dir = self.manager.get_project_dir(project.name)
        output_file = project_dir / (filename or f"{project.name}.json")

        modeler = ProjectModeler(project)
        path = modeler.save_project(output_file)

        return {"status": "saved", "path": str(path)}

    def load_project(self, filepath: str) -> dict[str, Any]:
        """Load project from file.

        Args:
            filepath: Path to project JSON file

        Returns:
            Loaded project info
        """
        try:
            modeler = ProjectModeler.load_project(Path(filepath))
            self.manager.add_project(modeler.project)

            return {
                "status": "loaded",
                "project": modeler.project.name,
                "num_parts": len(modeler.project.parts),
            }
        except Exception as e:
            return {"error": str(e)}
