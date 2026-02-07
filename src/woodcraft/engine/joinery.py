"""Joinery library for woodworking joints."""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from typing import Any

import cadquery as cq

from woodcraft.engine.modeler import Part


class JoineryType(str, Enum):
    """Types of woodworking joints."""

    # Basic joints
    BUTT = "butt"  # Simple butt joint
    MITER = "miter"  # 45-degree miter

    # Dado family
    DADO = "dado"  # Groove across grain
    RABBET = "rabbet"  # L-shaped cut on edge
    GROOVE = "groove"  # Channel with grain

    # Mortise and tenon family
    MORTISE_TENON = "mortise_tenon"
    THROUGH_MORTISE = "through_mortise"
    WEDGED_MORTISE = "wedged_mortise"
    LOOSE_TENON = "loose_tenon"  # Domino-style

    # Dovetails
    THROUGH_DOVETAIL = "through_dovetail"
    HALF_BLIND_DOVETAIL = "half_blind_dovetail"
    SLIDING_DOVETAIL = "sliding_dovetail"

    # Box joints
    BOX_JOINT = "box_joint"  # Finger joint
    LOCK_MITER = "lock_miter"

    # Other
    BISCUIT = "biscuit"
    POCKET_HOLE = "pocket_hole"
    DOWEL = "dowel"
    SPLINE = "spline"
    TONGUE_GROOVE = "tongue_groove"


@dataclass
class JointDefinition:
    """Definition of a joint between two parts."""

    joint_type: JoineryType
    part_a_id: str
    part_b_id: str
    # Position on part A where joint occurs
    position_a: tuple[float, float, float] = (0.0, 0.0, 0.0)
    # Position on part B where joint occurs
    position_b: tuple[float, float, float] = (0.0, 0.0, 0.0)
    # Joint-specific parameters
    parameters: dict[str, Any] | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "type": self.joint_type.value,
            "part_a": self.part_a_id,
            "part_b": self.part_b_id,
            "position_a": list(self.position_a),
            "position_b": list(self.position_b),
            "parameters": self.parameters or {},
        }

    @classmethod
    def from_dict(cls, data: dict[str, Any]) -> JointDefinition:
        return cls(
            joint_type=JoineryType(data["type"]),
            part_a_id=data["part_a"],
            part_b_id=data["part_b"],
            position_a=tuple(data.get("position_a", [0, 0, 0])),
            position_b=tuple(data.get("position_b", [0, 0, 0])),
            parameters=data.get("parameters"),
        )


class JoineryLibrary:
    """Library of joinery operations."""

    @staticmethod
    def cut_dado(
        workpiece: cq.Workplane,
        width: float,
        depth: float,
        position: float,
        along_length: bool = True,
    ) -> cq.Workplane:
        """Cut a dado (groove across the grain).

        Args:
            workpiece: The CadQuery workplane to cut
            width: Width of the dado
            depth: Depth of the dado
            position: Position along the board
            along_length: If True, dado runs across width; if False, across length
        """
        # Get workpiece bounds
        bb = workpiece.val().BoundingBox()
        board_length = bb.xlen
        board_width = bb.ylen

        if along_length:
            # Dado runs across the width (perpendicular to length)
            cut_length = board_width + 0.1  # Slight overcut
            cut = (
                cq.Workplane("XY")
                .center(position, 0)
                .rect(width, cut_length)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )
        else:
            # Dado runs across the length
            cut_length = board_length + 0.1
            cut = (
                cq.Workplane("XY")
                .center(0, position)
                .rect(cut_length, width)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )

        return workpiece.cut(cut)

    @staticmethod
    def cut_rabbet(
        workpiece: cq.Workplane,
        width: float,
        depth: float,
        edge: str = "back",
    ) -> cq.Workplane:
        """Cut a rabbet on an edge.

        Args:
            workpiece: The CadQuery workplane to cut
            width: Width of the rabbet
            depth: Depth of the rabbet
            edge: Which edge ('front', 'back', 'left', 'right', 'top', 'bottom')
        """
        bb = workpiece.val().BoundingBox()

        if edge == "back":
            cut = (
                cq.Workplane("XY")
                .center(0, bb.ymax - width / 2)
                .rect(bb.xlen + 0.1, width)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )
        elif edge == "front":
            cut = (
                cq.Workplane("XY")
                .center(0, bb.ymin + width / 2)
                .rect(bb.xlen + 0.1, width)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )
        elif edge == "left":
            cut = (
                cq.Workplane("XY")
                .center(bb.xmin + width / 2, 0)
                .rect(width, bb.ylen + 0.1)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )
        elif edge == "right":
            cut = (
                cq.Workplane("XY")
                .center(bb.xmax - width / 2, 0)
                .rect(width, bb.ylen + 0.1)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )
        else:
            raise ValueError(f"Unknown edge: {edge}")

        return workpiece.cut(cut)

    @staticmethod
    def cut_mortise(
        workpiece: cq.Workplane,
        width: float,
        height: float,
        depth: float,
        position: tuple[float, float],
        face: str = "front",
    ) -> cq.Workplane:
        """Cut a mortise pocket.

        Args:
            workpiece: The CadQuery workplane to cut
            width: Width of the mortise
            height: Height of the mortise
            depth: Depth of the mortise
            position: (x, y) position of mortise center on face
            face: Which face to cut into
        """
        bb = workpiece.val().BoundingBox()
        x, y = position

        if face == "front":
            cut = (
                cq.Workplane("XZ")
                .center(x, y)
                .rect(width, height)
                .extrude(depth)
                .translate((0, bb.ymin, 0))
            )
        elif face == "back":
            cut = (
                cq.Workplane("XZ")
                .center(x, y)
                .rect(width, height)
                .extrude(depth)
                .translate((0, bb.ymax - depth, 0))
            )
        elif face == "end":
            cut = (
                cq.Workplane("YZ")
                .center(y, position[1])
                .rect(width, height)
                .extrude(depth)
                .translate((bb.xmax - depth, 0, 0))
            )
        else:
            raise ValueError(f"Unknown face: {face}")

        return workpiece.cut(cut)

    @staticmethod
    def create_tenon(
        workpiece: cq.Workplane,
        width: float,
        height: float,
        length: float,
        shoulder: float,
    ) -> cq.Workplane:
        """Create a tenon on the end of a workpiece.

        Args:
            workpiece: The CadQuery workplane to modify
            width: Width of the tenon
            height: Height of the tenon
            length: Length of the tenon (how far it protrudes)
            shoulder: Shoulder depth on each side
        """
        bb = workpiece.val().BoundingBox()

        # Cut shoulders to form tenon
        # Top shoulder
        top_cut = (
            cq.Workplane("XY")
            .center(bb.xmax - length / 2, 0)
            .rect(length + 0.1, bb.ylen + 0.1)
            .extrude(shoulder)
            .translate((0, 0, bb.zmax - shoulder))
        )

        # Bottom shoulder
        bottom_cut = (
            cq.Workplane("XY")
            .center(bb.xmax - length / 2, 0)
            .rect(length + 0.1, bb.ylen + 0.1)
            .extrude(shoulder)
            .translate((0, 0, bb.zmin))
        )

        # Side shoulders
        side_shoulder = (bb.ylen - width) / 2
        if side_shoulder > 0:
            front_cut = (
                cq.Workplane("XY")
                .center(bb.xmax - length / 2, bb.ymin + side_shoulder / 2)
                .rect(length + 0.1, side_shoulder)
                .extrude(bb.zlen + 0.1)
                .translate((0, 0, bb.zmin))
            )
            back_cut = (
                cq.Workplane("XY")
                .center(bb.xmax - length / 2, bb.ymax - side_shoulder / 2)
                .rect(length + 0.1, side_shoulder)
                .extrude(bb.zlen + 0.1)
                .translate((0, 0, bb.zmin))
            )
            workpiece = workpiece.cut(front_cut).cut(back_cut)

        return workpiece.cut(top_cut).cut(bottom_cut)

    @staticmethod
    def cut_groove(
        workpiece: cq.Workplane,
        width: float,
        depth: float,
        position: float,
        along_length: bool = True,
    ) -> cq.Workplane:
        """Cut a groove (channel running with the grain).

        Args:
            workpiece: The CadQuery workplane to cut
            width: Width of the groove
            depth: Depth of the groove
            position: Position across the board
            along_length: If True, groove runs along length
        """
        bb = workpiece.val().BoundingBox()

        if along_length:
            cut_length = bb.xlen + 0.1
            cut = (
                cq.Workplane("XY")
                .center(0, position)
                .rect(cut_length, width)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )
        else:
            cut_length = bb.ylen + 0.1
            cut = (
                cq.Workplane("XY")
                .center(position, 0)
                .rect(width, cut_length)
                .extrude(depth)
                .translate((0, 0, bb.zmax - depth))
            )

        return workpiece.cut(cut)

    @staticmethod
    def apply_joint(
        part_a: Part,
        part_b: Part,
        joint: JointDefinition,
    ) -> tuple[cq.Workplane, cq.Workplane]:
        """Apply joinery operations to two parts.

        Returns modified CAD objects for both parts.
        """
        solid_a = part_a.build_cad()
        solid_b = part_b.build_cad()
        params = joint.parameters or {}

        if joint.joint_type == JoineryType.DADO:
            # Cut dado in part A to receive part B
            solid_a = JoineryLibrary.cut_dado(
                solid_a,
                width=params.get("width", part_b.dimensions.thickness),
                depth=params.get("depth", part_a.dimensions.thickness / 2),
                position=joint.position_a[0],
                along_length=params.get("along_length", True),
            )

        elif joint.joint_type == JoineryType.RABBET:
            solid_a = JoineryLibrary.cut_rabbet(
                solid_a,
                width=params.get("width", part_b.dimensions.thickness),
                depth=params.get("depth", part_a.dimensions.thickness / 2),
                edge=params.get("edge", "back"),
            )

        elif joint.joint_type == JoineryType.MORTISE_TENON:
            # Cut mortise in part A
            mortise_width = params.get("width", part_a.dimensions.thickness / 3)
            mortise_height = params.get("height", part_b.dimensions.width * 0.8)
            mortise_depth = params.get("depth", part_a.dimensions.width * 0.6)

            solid_a = JoineryLibrary.cut_mortise(
                solid_a,
                width=mortise_width,
                height=mortise_height,
                depth=mortise_depth,
                position=(joint.position_a[0], joint.position_a[2]),
                face=params.get("face", "front"),
            )

            # Create tenon on part B
            shoulder = (part_b.dimensions.thickness - mortise_width) / 2
            solid_b = JoineryLibrary.create_tenon(
                solid_b,
                width=mortise_width,
                height=mortise_height,
                length=mortise_depth - 0.0625,  # 1/16" clearance
                shoulder=shoulder,
            )

        elif joint.joint_type == JoineryType.GROOVE:
            solid_a = JoineryLibrary.cut_groove(
                solid_a,
                width=params.get("width", 0.25),
                depth=params.get("depth", 0.25),
                position=params.get("position", 0),
                along_length=params.get("along_length", True),
            )

        return solid_a, solid_b
