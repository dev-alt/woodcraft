"""Export MCP tools for 3D models and files."""

from __future__ import annotations

from pathlib import Path
from typing import Any

from woodcraft.engine.modeler import ProjectModeler
from woodcraft.engine.assembly import AssemblyManager
from woodcraft.tools.project import ProjectManager


class ExportTools:
    """MCP tools for exporting 3D models and files."""

    def __init__(self, manager: ProjectManager):
        self.manager = manager
        self._assembly_managers: dict[str, AssemblyManager] = {}

    def _get_assembly_manager(self, project_name: str | None = None) -> AssemblyManager | None:
        """Get or create assembly manager for project."""
        project = self.manager.get_project(project_name)
        if not project:
            return None

        if project.name not in self._assembly_managers:
            self._assembly_managers[project.name] = AssemblyManager(project)

        return self._assembly_managers[project.name]

    def export_step(
        self,
        part_id: str | None = None,
        filename: str | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Export to STEP format for CAD import.

        Args:
            part_id: Export single part (exports assembly if not specified)
            filename: Output filename
            project_name: Project name (uses active if not specified)

        Returns:
            Path to exported file
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        modeler = ProjectModeler(project)
        project_dir = self.manager.get_project_dir(project.name)
        output_dir = project_dir / "exports"

        if part_id:
            if not project.get_part(part_id):
                return {"error": f"Part '{part_id}' not found"}
            output_file = output_dir / (filename or f"{part_id}.step")
        else:
            output_file = output_dir / (filename or f"{project.name}_assembly.step")

        try:
            path = modeler.export_step(output_file, part_id)
            return {"status": "exported", "path": str(path), "format": "STEP"}
        except Exception as e:
            return {"error": str(e)}

    def export_stl(
        self,
        part_id: str | None = None,
        filename: str | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Export to STL format for 3D printing.

        Args:
            part_id: Export single part (exports all combined if not specified)
            filename: Output filename
            project_name: Project name (uses active if not specified)

        Returns:
            Path to exported file
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        modeler = ProjectModeler(project)
        project_dir = self.manager.get_project_dir(project.name)
        output_dir = project_dir / "exports"

        if part_id:
            if not project.get_part(part_id):
                return {"error": f"Part '{part_id}' not found"}
            output_file = output_dir / (filename or f"{part_id}.stl")
        else:
            output_file = output_dir / (filename or f"{project.name}_combined.stl")

        try:
            path = modeler.export_stl(output_file, part_id)
            return {"status": "exported", "path": str(path), "format": "STL"}
        except Exception as e:
            return {"error": str(e)}

    def export_dxf(
        self,
        part_id: str,
        view: str = "top",
        filename: str | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Export 2D DXF projection of a part.

        Args:
            part_id: Part to export
            view: Projection view ('top', 'front', 'side')
            filename: Output filename
            project_name: Project name (uses active if not specified)

        Returns:
            Path to exported file
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        if not project.get_part(part_id):
            return {"error": f"Part '{part_id}' not found"}

        modeler = ProjectModeler(project)
        project_dir = self.manager.get_project_dir(project.name)
        output_dir = project_dir / "exports"

        output_file = output_dir / (filename or f"{part_id}_{view}.dxf")

        try:
            path = modeler.export_dxf(output_file, part_id, view)
            return {"status": "exported", "path": str(path), "format": "DXF", "view": view}
        except Exception as e:
            return {"error": str(e)}

    def export_all_parts(
        self,
        format: str = "step",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Export all parts as individual files.

        Args:
            format: Output format ('step' or 'stl')
            project_name: Project name (uses active if not specified)

        Returns:
            List of exported files
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        modeler = ProjectModeler(project)
        project_dir = self.manager.get_project_dir(project.name)
        output_dir = project_dir / "exports" / "parts"
        output_dir.mkdir(parents=True, exist_ok=True)

        exported: list[str] = []
        errors: list[str] = []

        for part in project.parts:
            output_file = output_dir / f"{part.id}.{format}"
            try:
                if format == "stl":
                    modeler.export_stl(output_file, part.id)
                else:
                    modeler.export_step(output_file, part.id)
                exported.append(str(output_file))
            except Exception as e:
                errors.append(f"{part.id}: {str(e)}")

        return {
            "status": "completed",
            "format": format,
            "exported": exported,
            "count": len(exported),
            "errors": errors if errors else None,
        }

    def export_exploded_view(
        self,
        assembly_name: str,
        explosion_factor: float = 2.0,
        filename: str | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Export an exploded view of an assembly.

        Args:
            assembly_name: Name of the assembly
            explosion_factor: How far apart to spread parts
            filename: Output filename
            project_name: Project name (uses active if not specified)

        Returns:
            Path to exported file
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        manager = self._get_assembly_manager(project_name)
        if not manager:
            return {"error": "No project found"}

        try:
            exploded = manager.create_exploded_view(assembly_name, explosion_factor)

            project_dir = self.manager.get_project_dir(project.name)
            output_dir = project_dir / "exports"
            output_file = output_dir / (filename or f"{assembly_name}_exploded.step")

            exploded.save(str(output_file))

            return {
                "status": "exported",
                "path": str(output_file),
                "format": "STEP",
                "explosion_factor": explosion_factor,
            }
        except Exception as e:
            return {"error": str(e)}

    def list_exports(
        self,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """List all exported files for a project.

        Args:
            project_name: Project name (uses active if not specified)

        Returns:
            List of exported files
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        project_dir = self.manager.get_project_dir(project.name)
        export_dir = project_dir / "exports"

        if not export_dir.exists():
            return {"exports": [], "count": 0}

        exports: list[dict[str, Any]] = []
        for file_path in export_dir.rglob("*"):
            if file_path.is_file():
                exports.append({
                    "path": str(file_path),
                    "name": file_path.name,
                    "format": file_path.suffix[1:].upper(),
                    "size_bytes": file_path.stat().st_size,
                })

        return {
            "exports": exports,
            "count": len(exports),
            "directory": str(export_dir),
        }

    def get_supported_formats(self) -> dict[str, Any]:
        """Get list of supported export formats.

        Returns:
            Supported formats with descriptions
        """
        return {
            "3d_formats": [
                {
                    "format": "STEP",
                    "extension": ".step",
                    "description": "Standard for the Exchange of Product Data - universal CAD format",
                    "use_case": "Import into FreeCAD, Fusion 360, SolidWorks, etc.",
                },
                {
                    "format": "STL",
                    "extension": ".stl",
                    "description": "Stereolithography - triangulated surface mesh",
                    "use_case": "3D printing, simple visualization",
                },
            ],
            "2d_formats": [
                {
                    "format": "DXF",
                    "extension": ".dxf",
                    "description": "Drawing Exchange Format - 2D CAD format",
                    "use_case": "Import into AutoCAD, laser cutting, CNC",
                },
                {
                    "format": "SVG",
                    "extension": ".svg",
                    "description": "Scalable Vector Graphics - web-friendly vector",
                    "use_case": "Web display, documentation, printing",
                },
            ],
            "document_formats": [
                {
                    "format": "PDF",
                    "extension": ".pdf",
                    "description": "Portable Document Format",
                    "use_case": "Print-ready documentation",
                },
                {
                    "format": "Markdown",
                    "extension": ".md",
                    "description": "Markdown text format",
                    "use_case": "Assembly guides, documentation",
                },
                {
                    "format": "JSON",
                    "extension": ".json",
                    "description": "JavaScript Object Notation",
                    "use_case": "Project files, data exchange",
                },
                {
                    "format": "CSV",
                    "extension": ".csv",
                    "description": "Comma-Separated Values",
                    "use_case": "Bill of materials, spreadsheet import",
                },
            ],
        }
