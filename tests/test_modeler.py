"""Tests for the modeler module."""

import pytest
from pathlib import Path
import tempfile

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


class TestDimensions:
    """Tests for Dimensions dataclass."""

    def test_create_dimensions(self):
        dims = Dimensions(length=36, width=10, thickness=0.75)
        assert dims.length == 36
        assert dims.width == 10
        assert dims.thickness == 0.75

    def test_to_dict(self):
        dims = Dimensions(length=36, width=10, thickness=0.75)
        data = dims.to_dict()
        assert data == {"length": 36, "width": 10, "thickness": 0.75}

    def test_from_dict(self):
        data = {"length": 36, "width": 10, "thickness": 0.75}
        dims = Dimensions.from_dict(data)
        assert dims.length == 36
        assert dims.width == 10
        assert dims.thickness == 0.75


class TestPart:
    """Tests for Part dataclass."""

    def test_create_part(self):
        part = Part(
            id="test_part",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
            quantity=2,
        )
        assert part.id == "test_part"
        assert part.part_type == PartType.SHELF
        assert part.quantity == 2

    def test_part_to_dict(self):
        part = Part(
            id="test_part",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        data = part.to_dict()
        assert data["id"] == "test_part"
        assert data["type"] == "shelf"
        assert data["dimensions"]["length"] == 24

    def test_part_from_dict(self):
        data = {
            "id": "test_part",
            "type": "shelf",
            "dimensions": {"length": 24, "width": 10, "thickness": 0.75},
            "quantity": 2,
        }
        part = Part.from_dict(data)
        assert part.id == "test_part"
        assert part.part_type == PartType.SHELF
        assert part.quantity == 2


class TestProject:
    """Tests for Project dataclass."""

    def test_create_project(self):
        project = Project(name="Test Project")
        assert project.name == "Test Project"
        assert project.units == Units.INCHES
        assert len(project.parts) == 0

    def test_add_part(self):
        project = Project(name="Test Project")
        part = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        project.add_part(part)
        assert len(project.parts) == 1
        assert project.get_part("shelf") == part

    def test_add_duplicate_part_raises(self):
        project = Project(name="Test Project")
        part = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        project.add_part(part)
        with pytest.raises(ValueError):
            project.add_part(part)

    def test_remove_part(self):
        project = Project(name="Test Project")
        part = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        project.add_part(part)
        assert project.remove_part("shelf") is True
        assert len(project.parts) == 0

    def test_remove_nonexistent_part(self):
        project = Project(name="Test Project")
        assert project.remove_part("nonexistent") is False

    def test_project_serialization(self):
        project = Project(
            name="Test Project",
            units=Units.INCHES,
            material=MaterialSpec(species="oak", thickness=0.75, finish="lacquer"),
        )
        part = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        project.add_part(part)

        data = project.to_dict()
        loaded = Project.from_dict(data)

        assert loaded.name == project.name
        assert loaded.units == project.units
        assert loaded.material.species == project.material.species
        assert len(loaded.parts) == 1
        assert loaded.parts[0].id == "shelf"


class TestProjectModeler:
    """Tests for ProjectModeler class."""

    def test_save_and_load_project(self):
        project = Project(name="Test Project")
        part = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        project.add_part(part)

        modeler = ProjectModeler(project)

        with tempfile.TemporaryDirectory() as tmpdir:
            path = Path(tmpdir) / "test_project.json"
            modeler.save_project(path)

            loaded_modeler = ProjectModeler.load_project(path)
            assert loaded_modeler.project.name == "Test Project"
            assert len(loaded_modeler.project.parts) == 1
