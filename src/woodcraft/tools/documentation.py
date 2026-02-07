"""Documentation generation MCP tools."""

from __future__ import annotations

from pathlib import Path
from typing import Any

from woodcraft.generators.bom import BOMGenerator
from woodcraft.generators.drawing import DrawingConfig, DrawingGenerator, ViewType
from woodcraft.generators.guide import GuideGenerator
from woodcraft.tools.project import ProjectManager


class DocumentationTools:
    """MCP tools for documentation generation."""

    def __init__(self, manager: ProjectManager):
        self.manager = manager

    def generate_drawing(
        self,
        part_id: str,
        views: list[str] | None = None,
        output_format: str = "dxf",
        scale: float = 1.0,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate a dimensioned drawing for a part.

        Args:
            part_id: Part to draw
            views: List of views ('top', 'front', 'side')
            output_format: Output format ('dxf' or 'svg')
            scale: Drawing scale
            project_name: Project name (uses active if not specified)

        Returns:
            Path to generated drawing
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        if not project.get_part(part_id):
            return {"error": f"Part '{part_id}' not found"}

        config = DrawingConfig(scale=scale)
        generator = DrawingGenerator(project, config)

        view_types = [ViewType(v) for v in (views or ["top", "front", "side"])]
        doc = generator.create_part_drawing(part_id, view_types)

        project_dir = self.manager.get_project_dir(project.name)
        output_dir = project_dir / "drawings"
        output_file = output_dir / f"{part_id}.{output_format}"

        if output_format == "svg":
            path = generator.export_svg(doc, output_file)
        else:
            path = generator.export_dxf(doc, output_file)

        return {"status": "generated", "path": str(path), "format": output_format}

    def generate_all_drawings(
        self,
        output_format: str = "dxf",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate drawings for all parts in a project.

        Args:
            output_format: Output format ('dxf' or 'svg')
            project_name: Project name (uses active if not specified)

        Returns:
            List of generated files
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        generator = DrawingGenerator(project)
        project_dir = self.manager.get_project_dir(project.name)
        output_dir = project_dir / "drawings"

        paths = generator.generate_all_part_drawings(output_dir, output_format)

        return {
            "status": "generated",
            "files": [str(p) for p in paths],
            "count": len(paths),
        }

    def generate_bom(
        self,
        output_format: str = "json",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate bill of materials.

        Args:
            output_format: Output format ('json', 'csv', or 'text')
            project_name: Project name (uses active if not specified)

        Returns:
            BOM data and/or file path
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        generator = BOMGenerator(project)
        bom = generator.generate()

        project_dir = self.manager.get_project_dir(project.name)

        result: dict[str, Any] = {
            "status": "generated",
            "total_cost": bom.total_cost,
        }

        if output_format == "json":
            output_file = project_dir / "bom.json"
            generator.export_json(bom, output_file)
            result["path"] = str(output_file)
            result["bom"] = bom.to_dict()
        elif output_format == "csv":
            output_file = project_dir / "bom.csv"
            generator.export_csv(bom, output_file)
            result["path"] = str(output_file)
        else:  # text
            result["summary"] = generator.generate_summary(bom)

        return result

    def generate_assembly_guide(
        self,
        output_format: str = "markdown",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate assembly instructions.

        Args:
            output_format: Output format ('markdown' or 'json')
            project_name: Project name (uses active if not specified)

        Returns:
            Guide content and/or file path
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        generator = GuideGenerator(project)
        guide = generator.generate()

        project_dir = self.manager.get_project_dir(project.name)

        if output_format == "markdown":
            output_file = project_dir / "assembly_guide.md"
            generator.export_markdown(guide, output_file)
            return {
                "status": "generated",
                "path": str(output_file),
                "num_steps": len(guide.steps),
            }
        else:  # json
            return {
                "status": "generated",
                "guide": guide.to_dict(),
            }

    def add_hardware(
        self,
        name: str,
        quantity: int,
        unit_cost: float = 0.0,
        description: str = "",
        supplier: str = "",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Add hardware item to project for BOM.

        Args:
            name: Hardware name
            quantity: Number needed
            unit_cost: Cost per unit
            description: Description
            supplier: Supplier name
            project_name: Project name (uses active if not specified)

        Returns:
            Status
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        project.hardware.append({
            "name": name,
            "quantity": quantity,
            "cost": unit_cost,
            "description": description,
            "supplier": supplier,
            "unit": "each",
        })

        return {
            "status": "added",
            "hardware": name,
            "quantity": quantity,
        }

    def calculate_lumber(
        self,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Calculate lumber requirements for the project.

        Args:
            project_name: Project name (uses active if not specified)

        Returns:
            Lumber calculations
        """
        from woodcraft.utils.units import UnitConverter

        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        # Group by material
        materials: dict[str, dict[str, float]] = {}

        for part in project.parts:
            material = part.material or project.material.species
            if material not in materials:
                materials[material] = {
                    "board_feet": 0.0,
                    "square_feet": 0.0,
                    "linear_feet": 0.0,
                }

            bf = UnitConverter.board_feet(
                part.dimensions.length,
                part.dimensions.width,
                part.dimensions.thickness,
                project.units,
            )
            sf = UnitConverter.square_feet(
                part.dimensions.length,
                part.dimensions.width,
                project.units,
            )
            lf = UnitConverter.linear_feet(
                part.dimensions.length,
                project.units,
            )

            materials[material]["board_feet"] += bf * part.quantity
            materials[material]["square_feet"] += sf * part.quantity
            materials[material]["linear_feet"] += lf * part.quantity

        # Add waste factor
        waste_factor = 1.15  # 15% waste

        return {
            "materials": {
                mat: {
                    "board_feet": round(vals["board_feet"] * waste_factor, 2),
                    "board_feet_actual": round(vals["board_feet"], 2),
                    "square_feet": round(vals["square_feet"], 2),
                    "linear_feet": round(vals["linear_feet"], 2),
                }
                for mat, vals in materials.items()
            },
            "waste_factor": "15%",
            "note": "Board feet includes waste factor, other measurements are net",
        }
