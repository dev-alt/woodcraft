"""Cut list optimization using bin packing."""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import rectpack

from woodcraft.engine.modeler import Part, Project
from woodcraft.utils.units import UnitConverter, Units


@dataclass
class CutPiece:
    """A piece to be cut from stock."""

    part_id: str
    length: float
    width: float
    quantity: int = 1
    label: str = ""
    grain_constrained: bool = True  # If True, can't rotate

    def area(self) -> float:
        """Calculate area of the piece."""
        return self.length * self.width * self.quantity


@dataclass
class StockSheet:
    """A sheet of stock material."""

    width: float
    length: float
    material: str = "plywood"
    thickness: float = 0.75
    cost: float = 0.0

    def area(self) -> float:
        """Calculate area of the sheet."""
        return self.length * self.width


@dataclass
class PlacedPiece:
    """A piece placed on a stock sheet."""

    piece: CutPiece
    x: float
    y: float
    rotated: bool = False

    @property
    def width(self) -> float:
        return self.piece.width if not self.rotated else self.piece.length

    @property
    def length(self) -> float:
        return self.piece.length if not self.rotated else self.piece.width

    def to_dict(self) -> dict[str, Any]:
        return {
            "part_id": self.piece.part_id,
            "label": self.piece.label or self.piece.part_id,
            "x": self.x,
            "y": self.y,
            "width": self.width,
            "height": self.length,
            "rotated": self.rotated,
        }


@dataclass
class CutListResult:
    """Result of cut list optimization."""

    sheets: list[tuple[StockSheet, list[PlacedPiece]]] = field(default_factory=list)
    unplaced: list[CutPiece] = field(default_factory=list)
    total_stock_area: float = 0.0
    total_parts_area: float = 0.0
    waste_percentage: float = 0.0

    def to_dict(self) -> dict[str, Any]:
        return {
            "sheets": [
                {
                    "stock": {
                        "width": sheet.width,
                        "length": sheet.length,
                        "material": sheet.material,
                        "thickness": sheet.thickness,
                    },
                    "pieces": [p.to_dict() for p in pieces],
                }
                for sheet, pieces in self.sheets
            ],
            "unplaced": [
                {"part_id": p.part_id, "length": p.length, "width": p.width}
                for p in self.unplaced
            ],
            "total_stock_area": self.total_stock_area,
            "total_parts_area": self.total_parts_area,
            "waste_percentage": self.waste_percentage,
        }


class CutListOptimizer:
    """Optimizes cut lists using bin packing algorithms."""

    # Kerf (saw blade) width in inches
    DEFAULT_KERF = 0.125

    def __init__(
        self,
        project: Project,
        kerf: float = DEFAULT_KERF,
    ):
        self.project = project
        self.kerf = kerf

    def generate_cut_pieces(self) -> list[CutPiece]:
        """Generate cut pieces from project parts."""
        pieces: list[CutPiece] = []

        for part in self.project.parts:
            # Add kerf allowance
            length = part.dimensions.length + self.kerf
            width = part.dimensions.width + self.kerf

            piece = CutPiece(
                part_id=part.id,
                length=length,
                width=width,
                quantity=part.quantity,
                label=part.id,
                grain_constrained=part.grain_direction != "none",
            )
            pieces.append(piece)

        return pieces

    def optimize(
        self,
        stock: StockSheet | list[StockSheet],
        pieces: list[CutPiece] | None = None,
    ) -> CutListResult:
        """Optimize cut list placement.

        Args:
            stock: Stock sheet(s) to cut from
            pieces: Pieces to place (default: generate from project)
        """
        if pieces is None:
            pieces = self.generate_cut_pieces()

        if isinstance(stock, StockSheet):
            stock_list = [stock]
        else:
            stock_list = stock

        # Create packer
        packer = rectpack.newPacker(
            mode=rectpack.PackingMode.Offline,
            bin_algo=rectpack.PackingAlgorithm.BFF,
            pack_algo=rectpack.MaxRectsBssf,
            sort_algo=rectpack.SORT_AREA,
            rotation=True,
        )

        # Add bins (stock sheets)
        for i, sheet in enumerate(stock_list):
            packer.add_bin(int(sheet.length * 1000), int(sheet.width * 1000), count=999)

        # Add rectangles (pieces)
        # Expand by quantity and track mapping
        piece_map: dict[int, tuple[CutPiece, int]] = {}
        rect_id = 0
        for piece in pieces:
            for q in range(piece.quantity):
                packer.add_rect(
                    int(piece.length * 1000),
                    int(piece.width * 1000),
                    rid=rect_id,
                )
                piece_map[rect_id] = (piece, q)
                rect_id += 1

        # Pack
        packer.pack()

        # Process results
        result = CutListResult()
        sheet_placements: dict[int, list[PlacedPiece]] = {}

        for rect in packer.rect_list():
            bin_idx, x, y, w, h, rid = rect
            piece, _ = piece_map[rid]

            # Convert back from mm to inches
            x_in = x / 1000.0
            y_in = y / 1000.0
            w_in = w / 1000.0
            h_in = h / 1000.0

            # Check if rotated
            rotated = abs(w_in - piece.width) < 0.01

            placed = PlacedPiece(
                piece=piece,
                x=x_in,
                y=y_in,
                rotated=rotated,
            )

            if bin_idx not in sheet_placements:
                sheet_placements[bin_idx] = []
            sheet_placements[bin_idx].append(placed)

        # Build result
        for bin_idx, placements in sheet_placements.items():
            sheet_idx = min(bin_idx, len(stock_list) - 1)
            result.sheets.append((stock_list[sheet_idx], placements))
            result.total_stock_area += stock_list[sheet_idx].area()

        # Calculate totals
        for piece in pieces:
            result.total_parts_area += piece.area()

        if result.total_stock_area > 0:
            result.waste_percentage = (
                (result.total_stock_area - result.total_parts_area)
                / result.total_stock_area
                * 100
            )

        # Find unplaced pieces
        placed_rids = {rect[5] for rect in packer.rect_list()}
        for rid, (piece, _) in piece_map.items():
            if rid not in placed_rids:
                if piece not in result.unplaced:
                    result.unplaced.append(piece)

        return result

    def optimize_linear(
        self,
        stock_lengths: list[float],
        pieces: list[CutPiece] | None = None,
    ) -> dict[str, Any]:
        """Optimize linear cuts (1D bin packing) for boards.

        Args:
            stock_lengths: Available stock lengths
            pieces: Pieces to cut (uses length dimension)
        """
        if pieces is None:
            pieces = self.generate_cut_pieces()

        # Simple first-fit decreasing algorithm
        # Sort pieces by length descending
        sorted_pieces = []
        for piece in pieces:
            for _ in range(piece.quantity):
                sorted_pieces.append(piece)
        sorted_pieces.sort(key=lambda p: p.length, reverse=True)

        # Track stock usage
        stock_usage: list[dict[str, Any]] = []

        for piece in sorted_pieces:
            placed = False
            piece_len = piece.length + self.kerf

            # Try to fit in existing stock
            for stock in stock_usage:
                if stock["remaining"] >= piece_len:
                    stock["pieces"].append(
                        {
                            "part_id": piece.part_id,
                            "length": piece.length,
                            "position": stock["length"] - stock["remaining"],
                        }
                    )
                    stock["remaining"] -= piece_len
                    placed = True
                    break

            # Need new stock
            if not placed:
                # Find smallest stock that fits
                suitable = [s for s in stock_lengths if s >= piece_len]
                if suitable:
                    stock_len = min(suitable)
                    stock_usage.append(
                        {
                            "stock_length": stock_len,
                            "length": stock_len,
                            "remaining": stock_len - piece_len,
                            "pieces": [
                                {
                                    "part_id": piece.part_id,
                                    "length": piece.length,
                                    "position": 0,
                                }
                            ],
                        }
                    )

        # Calculate statistics
        total_stock = sum(s["stock_length"] for s in stock_usage)
        total_used = sum(
            sum(p["length"] for p in s["pieces"]) for s in stock_usage
        )
        waste = total_stock - total_used if total_stock > 0 else 0

        return {
            "stocks": stock_usage,
            "total_stock_length": total_stock,
            "total_used_length": total_used,
            "waste_length": waste,
            "waste_percentage": (waste / total_stock * 100) if total_stock > 0 else 0,
        }

    def generate_svg(
        self,
        result: CutListResult,
        output_path: Path,
        pixels_per_inch: float = 10,
    ) -> Path:
        """Generate SVG visualization of cut list.

        Args:
            result: Cut list optimization result
            output_path: Output file path
            pixels_per_inch: Scale factor
        """
        import drawsvg as draw

        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        # Calculate total height needed
        total_height = 0
        sheet_height_with_margin = 0
        for sheet, _ in result.sheets:
            sheet_height_with_margin = sheet.width * pixels_per_inch + 50
            total_height += sheet_height_with_margin

        if total_height == 0:
            total_height = 100

        max_width = max(
            (sheet.length * pixels_per_inch + 40 for sheet, _ in result.sheets),
            default=400,
        )

        d = draw.Drawing(max_width, total_height + 100)

        # Title
        d.append(
            draw.Text(
                f"Cut List - {self.project.name}",
                16,
                20,
                25,
                font_family="sans-serif",
                font_weight="bold",
            )
        )

        y_offset = 50
        for sheet_idx, (sheet, pieces) in enumerate(result.sheets):
            # Sheet label
            d.append(
                draw.Text(
                    f"Sheet {sheet_idx + 1}: {sheet.material} ({sheet.length}\" x {sheet.width}\")",
                    12,
                    20,
                    y_offset,
                    font_family="sans-serif",
                )
            )
            y_offset += 20

            # Draw sheet outline
            sheet_w = sheet.length * pixels_per_inch
            sheet_h = sheet.width * pixels_per_inch

            d.append(
                draw.Rectangle(
                    20,
                    y_offset,
                    sheet_w,
                    sheet_h,
                    fill="none",
                    stroke="black",
                    stroke_width=2,
                )
            )

            # Draw pieces
            colors = ["#90EE90", "#87CEEB", "#DDA0DD", "#F0E68C", "#FFA07A"]
            for i, placed in enumerate(pieces):
                color = colors[i % len(colors)]
                piece_x = 20 + placed.x * pixels_per_inch
                piece_y = y_offset + placed.y * pixels_per_inch
                piece_w = placed.width * pixels_per_inch
                piece_h = placed.length * pixels_per_inch

                # Piece rectangle
                d.append(
                    draw.Rectangle(
                        piece_x,
                        piece_y,
                        piece_w,
                        piece_h,
                        fill=color,
                        stroke="black",
                        stroke_width=1,
                    )
                )

                # Label
                label = placed.piece.label or placed.piece.part_id
                font_size = min(piece_w, piece_h) * 0.15
                font_size = max(6, min(font_size, 12))

                d.append(
                    draw.Text(
                        label,
                        font_size,
                        piece_x + piece_w / 2,
                        piece_y + piece_h / 2,
                        text_anchor="middle",
                        dominant_baseline="middle",
                        font_family="sans-serif",
                    )
                )

            y_offset += sheet_h + 30

        # Statistics
        d.append(
            draw.Text(
                f"Waste: {result.waste_percentage:.1f}%",
                12,
                20,
                y_offset,
                font_family="sans-serif",
            )
        )

        d.save_svg(str(output_path))
        return output_path
