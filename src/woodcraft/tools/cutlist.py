"""Cut list optimization MCP tools."""

from __future__ import annotations

from pathlib import Path
from typing import Any

from woodcraft.generators.cutlist import CutListOptimizer, CutPiece, StockSheet
from woodcraft.tools.project import ProjectManager


class CutListTools:
    """MCP tools for cut list optimization."""

    def __init__(self, manager: ProjectManager):
        self.manager = manager

    def generate_cutlist(
        self,
        stock_length: float,
        stock_width: float,
        stock_material: str = "plywood",
        stock_thickness: float = 0.75,
        kerf: float = 0.125,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate optimized cut list for sheet goods.

        Args:
            stock_length: Length of stock sheet (e.g., 96 for 8')
            stock_width: Width of stock sheet (e.g., 48 for 4')
            stock_material: Material type
            stock_thickness: Thickness of stock
            kerf: Saw blade width
            project_name: Project name (uses active if not specified)

        Returns:
            Optimized cut list with placements
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        stock = StockSheet(
            width=stock_width,
            length=stock_length,
            material=stock_material,
            thickness=stock_thickness,
        )

        optimizer = CutListOptimizer(project, kerf=kerf)
        result = optimizer.optimize(stock)

        return {
            "status": "optimized",
            "sheets_needed": len(result.sheets),
            "waste_percentage": round(result.waste_percentage, 1),
            "result": result.to_dict(),
        }

    def generate_cutlist_svg(
        self,
        stock_length: float,
        stock_width: float,
        stock_material: str = "plywood",
        stock_thickness: float = 0.75,
        kerf: float = 0.125,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate cut list with SVG visualization.

        Args:
            stock_length: Length of stock sheet
            stock_width: Width of stock sheet
            stock_material: Material type
            stock_thickness: Thickness of stock
            kerf: Saw blade width
            project_name: Project name (uses active if not specified)

        Returns:
            Cut list result and SVG file path
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        stock = StockSheet(
            width=stock_width,
            length=stock_length,
            material=stock_material,
            thickness=stock_thickness,
        )

        optimizer = CutListOptimizer(project, kerf=kerf)
        result = optimizer.optimize(stock)

        project_dir = self.manager.get_project_dir(project.name)
        output_file = project_dir / "cutlist.svg"

        optimizer.generate_svg(result, output_file)

        return {
            "status": "generated",
            "sheets_needed": len(result.sheets),
            "waste_percentage": round(result.waste_percentage, 1),
            "svg_path": str(output_file),
            "result": result.to_dict(),
        }

    def generate_linear_cutlist(
        self,
        stock_lengths: list[float],
        kerf: float = 0.125,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Generate cut list for linear stock (boards).

        Args:
            stock_lengths: Available stock lengths (e.g., [96, 120, 144])
            kerf: Saw blade width
            project_name: Project name (uses active if not specified)

        Returns:
            Linear cut list optimization result
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        optimizer = CutListOptimizer(project, kerf=kerf)
        result = optimizer.optimize_linear(stock_lengths)

        return {
            "status": "optimized",
            "stocks_needed": len(result["stocks"]),
            "total_stock_length": result["total_stock_length"],
            "waste_length": round(result["waste_length"], 2),
            "waste_percentage": round(result["waste_percentage"], 1),
            "layout": result["stocks"],
        }

    def add_custom_piece(
        self,
        part_id: str,
        length: float,
        width: float,
        quantity: int = 1,
        label: str = "",
        grain_constrained: bool = True,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Add a custom cut piece (not from project parts).

        Args:
            part_id: Identifier for the piece
            length: Piece length
            width: Piece width
            quantity: Number of pieces
            label: Label for the piece
            grain_constrained: Whether grain direction matters
            project_name: Project name (uses active if not specified)

        Returns:
            Status
        """
        # This creates a temporary piece for cut list optimization
        # without adding it to the project parts
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        # Store in project metadata for cutlist generation
        if not hasattr(project, "_custom_pieces"):
            project._custom_pieces = []  # type: ignore

        project._custom_pieces.append(  # type: ignore
            CutPiece(
                part_id=part_id,
                length=length,
                width=width,
                quantity=quantity,
                label=label or part_id,
                grain_constrained=grain_constrained,
            )
        )

        return {
            "status": "added",
            "piece": {
                "part_id": part_id,
                "length": length,
                "width": width,
                "quantity": quantity,
            },
        }

    def get_standard_sheet_sizes(self) -> dict[str, Any]:
        """Get standard sheet good sizes.

        Returns:
            Common sheet sizes and materials
        """
        return {
            "plywood": {
                "standard": {"length": 96, "width": 48},
                "half_sheet": {"length": 48, "width": 48},
                "quarter_sheet": {"length": 48, "width": 24},
                "thicknesses": [0.25, 0.375, 0.5, 0.625, 0.75],
            },
            "mdf": {
                "standard": {"length": 96, "width": 48},
                "oversized": {"length": 97, "width": 49},
                "thicknesses": [0.25, 0.5, 0.75],
            },
            "particle_board": {
                "standard": {"length": 96, "width": 48},
                "thicknesses": [0.5, 0.625, 0.75],
            },
            "note": "All dimensions in inches. Standard sheet is 4' x 8'.",
        }

    def get_standard_lumber_sizes(self) -> dict[str, Any]:
        """Get standard dimensional lumber sizes.

        Returns:
            Common lumber sizes with actual dimensions
        """
        return {
            "dimensional_lumber": {
                "1x2": {"actual_width": 1.5, "actual_thickness": 0.75},
                "1x3": {"actual_width": 2.5, "actual_thickness": 0.75},
                "1x4": {"actual_width": 3.5, "actual_thickness": 0.75},
                "1x6": {"actual_width": 5.5, "actual_thickness": 0.75},
                "1x8": {"actual_width": 7.25, "actual_thickness": 0.75},
                "1x10": {"actual_width": 9.25, "actual_thickness": 0.75},
                "1x12": {"actual_width": 11.25, "actual_thickness": 0.75},
                "2x4": {"actual_width": 3.5, "actual_thickness": 1.5},
                "2x6": {"actual_width": 5.5, "actual_thickness": 1.5},
                "2x8": {"actual_width": 7.25, "actual_thickness": 1.5},
                "2x10": {"actual_width": 9.25, "actual_thickness": 1.5},
                "2x12": {"actual_width": 11.25, "actual_thickness": 1.5},
                "4x4": {"actual_width": 3.5, "actual_thickness": 3.5},
            },
            "standard_lengths": [48, 72, 96, 120, 144, 192],
            "note": "Dimensions in inches. Nominal vs actual sizes shown.",
        }
