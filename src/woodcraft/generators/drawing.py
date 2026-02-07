"""2D drawing generator with dimensions using ezdxf."""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from pathlib import Path
from typing import Any

import ezdxf
from ezdxf import units as dxf_units
from ezdxf.addons.drawing import Frontend, RenderContext
from ezdxf.addons.drawing.matplotlib import MatplotlibBackend
from ezdxf.enums import TextEntityAlignment

from woodcraft.engine.modeler import Part, Project
from woodcraft.utils.units import UnitConverter, Units


class ViewType(str, Enum):
    """Types of drawing views."""

    TOP = "top"  # Plan view (looking down)
    FRONT = "front"  # Front elevation
    SIDE = "side"  # Side elevation (right)
    LEFT = "left"  # Left side elevation
    SECTION = "section"


@dataclass
class DrawingConfig:
    """Configuration for drawing generation."""

    scale: float = 1.0  # Drawing scale (1.0 = full size)
    dimension_style: str = "standard"
    show_hidden: bool = False
    margin: float = 1.0  # Margin around drawing in inches
    text_height: float = 0.125  # Dimension text height
    arrow_size: float = 0.125


class DrawingGenerator:
    """Generates 2D dimensioned drawings using ezdxf."""

    def __init__(self, project: Project, config: DrawingConfig | None = None):
        self.project = project
        self.config = config or DrawingConfig()
        self._setup_dimension_style()

    def _setup_dimension_style(self) -> None:
        """Set up the dimension style."""
        self._dimstyle_attribs = {
            "dimtxt": self.config.text_height,
            "dimasz": self.config.arrow_size,
            "dimgap": self.config.text_height / 2,
            "dimexe": self.config.text_height,
            "dimexo": self.config.text_height / 4,
            "dimdec": 2,  # Decimal places
        }

    def create_part_drawing(
        self,
        part_id: str,
        views: list[ViewType] | None = None,
    ) -> ezdxf.document.Drawing:
        """Create a multi-view drawing for a single part.

        Args:
            part_id: ID of the part to draw
            views: List of views to include (default: top, front, side)
        """
        part = self.project.get_part(part_id)
        if part is None:
            raise ValueError(f"Part '{part_id}' not found")

        if views is None:
            views = [ViewType.TOP, ViewType.FRONT, ViewType.SIDE]

        doc = ezdxf.new("R2018")
        doc.units = dxf_units.IN  # Set to inches
        msp = doc.modelspace()

        # Create dimension style
        doc.dimstyles.new("WOODCRAFT", dxfattribs=self._dimstyle_attribs)

        # Layout views
        x_offset = 0.0
        y_offset = 0.0
        spacing = 2.0  # Space between views

        for view in views:
            if view == ViewType.TOP:
                self._draw_top_view(msp, part, x_offset, y_offset)
                x_offset += part.dimensions.length + spacing
            elif view == ViewType.FRONT:
                self._draw_front_view(msp, part, x_offset, y_offset)
                x_offset += part.dimensions.length + spacing
            elif view == ViewType.SIDE:
                self._draw_side_view(msp, part, x_offset, y_offset)
                x_offset += part.dimensions.width + spacing

        # Add title block
        self._add_title_block(msp, part, x_offset, y_offset)

        return doc

    def _draw_top_view(
        self,
        msp: Any,
        part: Part,
        x_offset: float,
        y_offset: float,
    ) -> None:
        """Draw top/plan view of a part."""
        d = part.dimensions
        x, y = x_offset, y_offset

        # Draw rectangle (length x width)
        points = [
            (x, y),
            (x + d.length, y),
            (x + d.length, y + d.width),
            (x, y + d.width),
            (x, y),  # Close
        ]
        msp.add_lwpolyline(points)

        # Add length dimension (bottom)
        msp.add_linear_dim(
            base=(x + d.length / 2, y - 0.5),
            p1=(x, y),
            p2=(x + d.length, y),
            dimstyle="WOODCRAFT",
        ).render()

        # Add width dimension (right side)
        msp.add_linear_dim(
            base=(x + d.length + 0.5, y + d.width / 2),
            p1=(x + d.length, y),
            p2=(x + d.length, y + d.width),
            angle=90,
            dimstyle="WOODCRAFT",
        ).render()

        # Add view label
        msp.add_text(
            "TOP VIEW",
            dxfattribs={
                "height": self.config.text_height * 1.5,
                "layer": "TEXT",
            },
        ).set_placement((x + d.length / 2, y + d.width + 0.3), align=TextEntityAlignment.BOTTOM_CENTER)

    def _draw_front_view(
        self,
        msp: Any,
        part: Part,
        x_offset: float,
        y_offset: float,
    ) -> None:
        """Draw front elevation view."""
        d = part.dimensions
        x, y = x_offset, y_offset

        # Draw rectangle (length x thickness)
        points = [
            (x, y),
            (x + d.length, y),
            (x + d.length, y + d.thickness),
            (x, y + d.thickness),
            (x, y),
        ]
        msp.add_lwpolyline(points)

        # Add length dimension
        msp.add_linear_dim(
            base=(x + d.length / 2, y - 0.5),
            p1=(x, y),
            p2=(x + d.length, y),
            dimstyle="WOODCRAFT",
        ).render()

        # Add thickness dimension
        msp.add_linear_dim(
            base=(x + d.length + 0.5, y + d.thickness / 2),
            p1=(x + d.length, y),
            p2=(x + d.length, y + d.thickness),
            angle=90,
            dimstyle="WOODCRAFT",
        ).render()

        # Add view label
        msp.add_text(
            "FRONT VIEW",
            dxfattribs={
                "height": self.config.text_height * 1.5,
                "layer": "TEXT",
            },
        ).set_placement((x + d.length / 2, y + d.thickness + 0.3), align=TextEntityAlignment.BOTTOM_CENTER)

    def _draw_side_view(
        self,
        msp: Any,
        part: Part,
        x_offset: float,
        y_offset: float,
    ) -> None:
        """Draw side elevation view."""
        d = part.dimensions
        x, y = x_offset, y_offset

        # Draw rectangle (width x thickness)
        points = [
            (x, y),
            (x + d.width, y),
            (x + d.width, y + d.thickness),
            (x, y + d.thickness),
            (x, y),
        ]
        msp.add_lwpolyline(points)

        # Add width dimension
        msp.add_linear_dim(
            base=(x + d.width / 2, y - 0.5),
            p1=(x, y),
            p2=(x + d.width, y),
            dimstyle="WOODCRAFT",
        ).render()

        # Add thickness dimension
        msp.add_linear_dim(
            base=(x + d.width + 0.5, y + d.thickness / 2),
            p1=(x + d.width, y),
            p2=(x + d.width, y + d.thickness),
            angle=90,
            dimstyle="WOODCRAFT",
        ).render()

        # Add view label
        msp.add_text(
            "SIDE VIEW",
            dxfattribs={
                "height": self.config.text_height * 1.5,
                "layer": "TEXT",
            },
        ).set_placement((x + d.width / 2, y + d.thickness + 0.3), align=TextEntityAlignment.BOTTOM_CENTER)

    def _add_title_block(
        self,
        msp: Any,
        part: Part,
        total_width: float,
        y_offset: float,
    ) -> None:
        """Add a title block to the drawing."""
        # Position below the views
        x = 0
        y = y_offset - 1.5

        # Part name
        msp.add_text(
            f"Part: {part.id}",
            dxfattribs={"height": self.config.text_height * 2, "layer": "TEXT"},
        ).set_placement((x, y), align=TextEntityAlignment.LEFT)

        # Material
        if part.material:
            msp.add_text(
                f"Material: {part.material}",
                dxfattribs={"height": self.config.text_height, "layer": "TEXT"},
            ).set_placement((x, y - 0.3), align=TextEntityAlignment.LEFT)

        # Quantity
        msp.add_text(
            f"Qty: {part.quantity}",
            dxfattribs={"height": self.config.text_height, "layer": "TEXT"},
        ).set_placement((x, y - 0.5), align=TextEntityAlignment.LEFT)

        # Dimensions summary
        d = part.dimensions
        dim_text = f"{d.length}\" x {d.width}\" x {d.thickness}\""
        msp.add_text(
            dim_text,
            dxfattribs={"height": self.config.text_height, "layer": "TEXT"},
        ).set_placement((x, y - 0.7), align=TextEntityAlignment.LEFT)

    def create_cut_list_drawing(
        self,
        layout: list[dict[str, Any]],
        stock_width: float,
        stock_length: float,
    ) -> ezdxf.document.Drawing:
        """Create a visual cut list diagram.

        Args:
            layout: List of piece placements with x, y, width, height, label
            stock_width: Width of stock material
            stock_length: Length of stock material
        """
        doc = ezdxf.new("R2018")
        doc.units = dxf_units.IN
        msp = doc.modelspace()

        # Draw stock outline
        msp.add_lwpolyline(
            [
                (0, 0),
                (stock_length, 0),
                (stock_length, stock_width),
                (0, stock_width),
                (0, 0),
            ],
            dxfattribs={"color": 7},  # White
        )

        # Draw each piece
        for piece in layout:
            x = piece["x"]
            y = piece["y"]
            w = piece["width"]
            h = piece["height"]
            label = piece.get("label", "")

            # Piece outline
            msp.add_lwpolyline(
                [
                    (x, y),
                    (x + w, y),
                    (x + w, y + h),
                    (x, y + h),
                    (x, y),
                ],
                dxfattribs={"color": 3},  # Green
            )

            # Label
            if label:
                msp.add_text(
                    label,
                    dxfattribs={"height": min(w, h) * 0.1, "layer": "TEXT"},
                ).set_placement((x + w / 2, y + h / 2), align=TextEntityAlignment.MIDDLE_CENTER)

        return doc

    def export_dxf(self, doc: ezdxf.document.Drawing, output_path: Path) -> Path:
        """Export drawing to DXF file."""
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        doc.saveas(str(output_path))
        return output_path

    def export_svg(
        self,
        doc: ezdxf.document.Drawing,
        output_path: Path,
    ) -> Path:
        """Export drawing to SVG file using matplotlib backend."""
        import matplotlib.pyplot as plt

        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        fig = plt.figure()
        ax = fig.add_axes([0, 0, 1, 1])
        ctx = RenderContext(doc)
        out = MatplotlibBackend(ax)
        Frontend(ctx, out).draw_layout(doc.modelspace())
        ax.set_aspect("equal")

        fig.savefig(str(output_path), format="svg")
        plt.close(fig)

        return output_path

    def generate_all_part_drawings(
        self,
        output_dir: Path,
        format: str = "dxf",
    ) -> list[Path]:
        """Generate drawings for all parts in the project.

        Args:
            output_dir: Directory to save drawings
            format: Output format ('dxf' or 'svg')
        """
        output_dir = Path(output_dir)
        output_dir.mkdir(parents=True, exist_ok=True)
        output_files: list[Path] = []

        for part in self.project.parts:
            doc = self.create_part_drawing(part.id)
            filename = f"{part.id}.{format}"
            output_path = output_dir / filename

            if format == "svg":
                self.export_svg(doc, output_path)
            else:
                self.export_dxf(doc, output_path)

            output_files.append(output_path)

        return output_files
