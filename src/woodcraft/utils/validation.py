"""Design validation utilities."""

from dataclasses import dataclass
from enum import Enum
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from woodcraft.engine.modeler import Part, Project


class Severity(str, Enum):
    """Validation issue severity levels."""

    ERROR = "error"
    WARNING = "warning"
    INFO = "info"


@dataclass
class ValidationIssue:
    """A validation issue found in the design."""

    severity: Severity
    message: str
    part_id: str | None = None
    location: str | None = None

    def __str__(self) -> str:
        prefix = f"[{self.severity.value.upper()}]"
        if self.part_id:
            prefix += f" Part '{self.part_id}'"
        if self.location:
            prefix += f" at {self.location}"
        return f"{prefix}: {self.message}"


class DesignValidator:
    """Validates woodworking designs for common issues."""

    # Minimum recommended dimensions in inches
    MIN_THICKNESS = 0.25
    MIN_WIDTH = 0.5
    MIN_LENGTH = 1.0

    # Maximum practical dimensions in inches
    MAX_LENGTH = 192  # 16 feet
    MAX_WIDTH = 48  # Standard sheet width
    MAX_THICKNESS = 6

    # Minimum material for structural integrity
    MIN_EDGE_AFTER_DADO = 0.25
    MIN_MORTISE_SHOULDER = 0.25

    def validate_project(self, project: "Project") -> list[ValidationIssue]:
        """Run all validations on a project."""
        issues: list[ValidationIssue] = []

        for part in project.parts:
            issues.extend(self.validate_part(part))

        # Check for duplicate part IDs
        part_ids = [p.id for p in project.parts]
        seen: set[str] = set()
        for pid in part_ids:
            if pid in seen:
                issues.append(
                    ValidationIssue(
                        severity=Severity.ERROR,
                        message=f"Duplicate part ID: '{pid}'",
                    )
                )
            seen.add(pid)

        return issues

    def validate_part(self, part: "Part") -> list[ValidationIssue]:
        """Validate a single part."""
        issues: list[ValidationIssue] = []

        # Check minimum dimensions
        if part.dimensions.thickness < self.MIN_THICKNESS:
            issues.append(
                ValidationIssue(
                    severity=Severity.WARNING,
                    message=f"Thickness {part.dimensions.thickness}\" is very thin",
                    part_id=part.id,
                )
            )

        if part.dimensions.width < self.MIN_WIDTH:
            issues.append(
                ValidationIssue(
                    severity=Severity.WARNING,
                    message=f"Width {part.dimensions.width}\" is very narrow",
                    part_id=part.id,
                )
            )

        if part.dimensions.length < self.MIN_LENGTH:
            issues.append(
                ValidationIssue(
                    severity=Severity.WARNING,
                    message=f"Length {part.dimensions.length}\" is very short",
                    part_id=part.id,
                )
            )

        # Check maximum dimensions
        if part.dimensions.length > self.MAX_LENGTH:
            issues.append(
                ValidationIssue(
                    severity=Severity.ERROR,
                    message=f"Length {part.dimensions.length}\" exceeds standard lumber length",
                    part_id=part.id,
                )
            )

        if part.dimensions.width > self.MAX_WIDTH:
            issues.append(
                ValidationIssue(
                    severity=Severity.WARNING,
                    message=f"Width {part.dimensions.width}\" may require edge-glued panel",
                    part_id=part.id,
                )
            )

        # Check for zero or negative dimensions
        for dim_name in ["length", "width", "thickness"]:
            dim_value = getattr(part.dimensions, dim_name)
            if dim_value <= 0:
                issues.append(
                    ValidationIssue(
                        severity=Severity.ERROR,
                        message=f"{dim_name.capitalize()} must be positive (got {dim_value})",
                        part_id=part.id,
                    )
                )

        return issues

    def validate_dado(
        self, board_thickness: float, dado_depth: float, dado_width: float
    ) -> list[ValidationIssue]:
        """Validate dado joint parameters."""
        issues: list[ValidationIssue] = []

        # Dado should not exceed half the board thickness
        if dado_depth > board_thickness / 2:
            issues.append(
                ValidationIssue(
                    severity=Severity.WARNING,
                    message=f"Dado depth {dado_depth}\" exceeds half board thickness",
                )
            )

        # Check remaining material
        remaining = board_thickness - dado_depth
        if remaining < self.MIN_EDGE_AFTER_DADO:
            issues.append(
                ValidationIssue(
                    severity=Severity.ERROR,
                    message=f"Only {remaining}\" remains after dado - structurally weak",
                )
            )

        return issues

    def validate_mortise(
        self,
        board_thickness: float,
        mortise_width: float,
        mortise_depth: float,
    ) -> list[ValidationIssue]:
        """Validate mortise joint parameters."""
        issues: list[ValidationIssue] = []

        # Mortise width should be roughly 1/3 of board thickness
        ideal_width = board_thickness / 3
        if mortise_width > board_thickness / 2:
            issues.append(
                ValidationIssue(
                    severity=Severity.WARNING,
                    message=f"Mortise width {mortise_width}\" weakens the board (ideal: {ideal_width:.2f}\")",
                )
            )

        # Check shoulders
        shoulder = (board_thickness - mortise_width) / 2
        if shoulder < self.MIN_MORTISE_SHOULDER:
            issues.append(
                ValidationIssue(
                    severity=Severity.ERROR,
                    message=f"Mortise shoulder {shoulder}\" is too thin",
                )
            )

        return issues
