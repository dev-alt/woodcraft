"""Tests for the cut list optimizer."""

import pytest
import tempfile
from pathlib import Path

from woodcraft.engine.modeler import Dimensions, Part, PartType, Project
from woodcraft.generators.cutlist import CutListOptimizer, CutPiece, StockSheet


class TestCutPiece:
    """Tests for CutPiece dataclass."""

    def test_area(self):
        piece = CutPiece(part_id="test", length=24, width=12, quantity=2)
        assert piece.area() == 24 * 12 * 2


class TestStockSheet:
    """Tests for StockSheet dataclass."""

    def test_area(self):
        sheet = StockSheet(width=48, length=96)
        assert sheet.area() == 48 * 96


class TestCutListOptimizer:
    """Tests for CutListOptimizer class."""

    @pytest.fixture
    def simple_project(self):
        project = Project(name="Test Project")
        project.add_part(Part(
            id="shelf_1",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 12, 0.75),
            quantity=2,
        ))
        project.add_part(Part(
            id="shelf_2",
            part_type=PartType.SHELF,
            dimensions=Dimensions(20, 10, 0.75),
            quantity=1,
        ))
        return project

    def test_generate_cut_pieces(self, simple_project):
        optimizer = CutListOptimizer(simple_project)
        pieces = optimizer.generate_cut_pieces()

        assert len(pieces) == 2
        # Check kerf was added
        assert pieces[0].length > 24
        assert pieces[0].width > 12

    def test_optimize_single_sheet(self, simple_project):
        optimizer = CutListOptimizer(simple_project)
        stock = StockSheet(width=48, length=96)
        result = optimizer.optimize(stock)

        # All pieces should fit on one sheet
        assert len(result.sheets) >= 1
        assert len(result.unplaced) == 0
        assert result.waste_percentage < 100

    def test_optimize_calculates_waste(self, simple_project):
        optimizer = CutListOptimizer(simple_project)
        stock = StockSheet(width=48, length=96)
        result = optimizer.optimize(stock)

        assert result.total_stock_area > 0
        assert result.total_parts_area > 0
        assert result.waste_percentage >= 0

    def test_optimize_linear_simple(self):
        project = Project(name="Test")
        project.add_part(Part(
            id="board_1",
            part_type=PartType.BOARD,
            dimensions=Dimensions(36, 3.5, 0.75),
            quantity=3,
        ))
        project.add_part(Part(
            id="board_2",
            part_type=PartType.BOARD,
            dimensions=Dimensions(24, 3.5, 0.75),
            quantity=2,
        ))

        optimizer = CutListOptimizer(project)
        result = optimizer.optimize_linear([96, 120])

        assert result["total_stock_length"] > 0
        assert result["waste_percentage"] < 100
        assert len(result["stocks"]) > 0

    def test_generate_svg(self, simple_project):
        optimizer = CutListOptimizer(simple_project)
        stock = StockSheet(width=48, length=96)
        result = optimizer.optimize(stock)

        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "cutlist.svg"
            optimizer.generate_svg(result, output_path)
            assert output_path.exists()
            assert output_path.stat().st_size > 0
