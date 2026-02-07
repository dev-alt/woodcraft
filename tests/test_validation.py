"""Tests for the validation module."""

import pytest
from woodcraft.engine.modeler import Dimensions, Part, PartType, Project
from woodcraft.utils.validation import DesignValidator, Severity


class TestDesignValidator:
    """Tests for DesignValidator class."""

    def test_validate_normal_part(self):
        part = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        validator = DesignValidator()
        issues = validator.validate_part(part)
        # Should have no errors or warnings for normal dimensions
        errors = [i for i in issues if i.severity == Severity.ERROR]
        assert len(errors) == 0

    def test_validate_thin_part_warning(self):
        part = Part(
            id="thin_part",
            part_type=PartType.PANEL,
            dimensions=Dimensions(24, 10, 0.1),  # Very thin
        )
        validator = DesignValidator()
        issues = validator.validate_part(part)
        warnings = [i for i in issues if i.severity == Severity.WARNING]
        assert any("thin" in w.message.lower() for w in warnings)

    def test_validate_oversized_part(self):
        part = Part(
            id="huge_part",
            part_type=PartType.PANEL,
            dimensions=Dimensions(200, 10, 0.75),  # Over 16 feet
        )
        validator = DesignValidator()
        issues = validator.validate_part(part)
        errors = [i for i in issues if i.severity == Severity.ERROR]
        assert any("exceeds" in e.message.lower() for e in errors)

    def test_validate_zero_dimension_error(self):
        part = Part(
            id="bad_part",
            part_type=PartType.PANEL,
            dimensions=Dimensions(24, 0, 0.75),  # Zero width
        )
        validator = DesignValidator()
        issues = validator.validate_part(part)
        errors = [i for i in issues if i.severity == Severity.ERROR]
        assert any("positive" in e.message.lower() for e in errors)

    def test_validate_project_duplicate_ids(self):
        project = Project(name="Test")
        part1 = Part(
            id="shelf",
            part_type=PartType.SHELF,
            dimensions=Dimensions(24, 10, 0.75),
        )
        part2 = Part(
            id="shelf",  # Duplicate ID
            part_type=PartType.SHELF,
            dimensions=Dimensions(30, 10, 0.75),
        )
        # Manually add to bypass add_part validation
        project.parts.append(part1)
        project.parts.append(part2)

        validator = DesignValidator()
        issues = validator.validate_project(project)
        errors = [i for i in issues if i.severity == Severity.ERROR]
        assert any("duplicate" in e.message.lower() for e in errors)

    def test_validate_dado_depth(self):
        validator = DesignValidator()

        # Valid dado
        issues = validator.validate_dado(0.75, 0.25, 0.75)
        errors = [i for i in issues if i.severity == Severity.ERROR]
        assert len(errors) == 0

        # Dado too deep
        issues = validator.validate_dado(0.75, 0.5, 0.75)
        warnings = [i for i in issues if i.severity == Severity.WARNING]
        assert any("half" in w.message.lower() for w in warnings)

    def test_validate_mortise_width(self):
        validator = DesignValidator()

        # Mortise too wide
        issues = validator.validate_mortise(0.75, 0.5, 2.0)
        warnings = [i for i in issues if i.severity == Severity.WARNING]
        assert len(warnings) > 0
