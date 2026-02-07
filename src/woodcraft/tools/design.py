"""Design and CAD MCP tools."""

from __future__ import annotations

from pathlib import Path
from typing import Any

from woodcraft.engine.assembly import Assembly, AssemblyManager
from woodcraft.engine.joinery import JoineryLibrary, JoineryType, JointDefinition
from woodcraft.engine.modeler import ProjectModeler
from woodcraft.tools.project import ProjectManager
from woodcraft.utils.validation import DesignValidator


class DesignTools:
    """MCP tools for design and modeling."""

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

    def add_joinery(
        self,
        joint_type: str,
        part_a_id: str,
        part_b_id: str,
        position_a: list[float] | None = None,
        position_b: list[float] | None = None,
        parameters: dict[str, Any] | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Add joinery between two parts.

        Args:
            joint_type: Type of joint (dado, rabbet, mortise_tenon, etc.)
            part_a_id: First part ID
            part_b_id: Second part ID
            position_a: Position on part A (x, y, z)
            position_b: Position on part B (x, y, z)
            parameters: Joint-specific parameters
            project_name: Project name (uses active if not specified)

        Returns:
            Joint definition
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        # Validate parts exist
        if not project.get_part(part_a_id):
            return {"error": f"Part '{part_a_id}' not found"}
        if not project.get_part(part_b_id):
            return {"error": f"Part '{part_b_id}' not found"}

        try:
            joint = JointDefinition(
                joint_type=JoineryType(joint_type),
                part_a_id=part_a_id,
                part_b_id=part_b_id,
                position_a=tuple(position_a or [0, 0, 0]),
                position_b=tuple(position_b or [0, 0, 0]),
                parameters=parameters,
            )
            project.joinery.append(joint.to_dict())

            return {"status": "added", "joint": joint.to_dict()}
        except ValueError as e:
            return {"error": str(e)}

    def list_joinery_types(self) -> dict[str, Any]:
        """List available joinery types.

        Returns:
            List of joinery types with descriptions
        """
        descriptions = {
            JoineryType.BUTT: "Simple butt joint - weakest, good for hidden joints",
            JoineryType.MITER: "45-degree miter joint - clean corner appearance",
            JoineryType.DADO: "Groove across grain - great for shelves",
            JoineryType.RABBET: "L-shaped cut on edge - good for backs and panels",
            JoineryType.GROOVE: "Channel with grain - for panel insertion",
            JoineryType.MORTISE_TENON: "Strong traditional joint for frames",
            JoineryType.THROUGH_MORTISE: "Mortise that goes all the way through",
            JoineryType.LOOSE_TENON: "Domino-style floating tenon",
            JoineryType.THROUGH_DOVETAIL: "Decorative and strong for drawers/boxes",
            JoineryType.HALF_BLIND_DOVETAIL: "Dovetails hidden from front",
            JoineryType.SLIDING_DOVETAIL: "Strong joint for shelves",
            JoineryType.BOX_JOINT: "Finger joint - strong and decorative",
            JoineryType.BISCUIT: "Quick alignment joint with football-shaped inserts",
            JoineryType.POCKET_HOLE: "Quick joint using angled screws",
            JoineryType.DOWEL: "Traditional round dowel alignment",
            JoineryType.TONGUE_GROOVE: "Interlocking edge joint",
        }

        return {
            "joinery_types": [
                {"type": jt.value, "description": descriptions.get(jt, "")}
                for jt in JoineryType
            ]
        }

    def create_assembly(
        self,
        name: str,
        part_ids: list[str] | None = None,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Create an assembly from parts.

        Args:
            name: Assembly name
            part_ids: List of part IDs to include
            project_name: Project name (uses active if not specified)

        Returns:
            Assembly info
        """
        manager = self._get_assembly_manager(project_name)
        if not manager:
            return {"error": "No project found"}

        try:
            assembly = manager.create_assembly(name, part_ids)
            return {
                "status": "created",
                "assembly": assembly.to_dict(),
            }
        except ValueError as e:
            return {"error": str(e)}

    def add_to_assembly(
        self,
        assembly_name: str,
        part_id: str,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Add a part to an existing assembly.

        Args:
            assembly_name: Name of the assembly
            part_id: Part ID to add
            project_name: Project name (uses active if not specified)

        Returns:
            Status
        """
        manager = self._get_assembly_manager(project_name)
        if not manager:
            return {"error": "No project found"}

        try:
            manager.add_part_to_assembly(assembly_name, part_id)
            return {"status": "added", "assembly": assembly_name, "part": part_id}
        except ValueError as e:
            return {"error": str(e)}

    def list_assemblies(self, project_name: str | None = None) -> dict[str, Any]:
        """List all assemblies in a project.

        Args:
            project_name: Project name (uses active if not specified)

        Returns:
            List of assembly names
        """
        manager = self._get_assembly_manager(project_name)
        if not manager:
            return {"error": "No project found"}

        return {"assemblies": manager.list_assemblies()}

    def validate_design(self, project_name: str | None = None) -> dict[str, Any]:
        """Validate the design for common issues.

        Args:
            project_name: Project name (uses active if not specified)

        Returns:
            List of validation issues
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        validator = DesignValidator()
        issues = validator.validate_project(project)

        return {
            "valid": len([i for i in issues if i.severity.value == "error"]) == 0,
            "issues": [
                {
                    "severity": issue.severity.value,
                    "message": issue.message,
                    "part_id": issue.part_id,
                }
                for issue in issues
            ],
        }

    def position_part(
        self,
        part_id: str,
        x: float,
        y: float,
        z: float,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Set the position of a part in 3D space.

        Args:
            part_id: Part to position
            x: X coordinate
            y: Y coordinate
            z: Z coordinate
            project_name: Project name (uses active if not specified)

        Returns:
            Updated part info
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        part = project.get_part(part_id)
        if not part:
            return {"error": f"Part '{part_id}' not found"}

        part.position = (x, y, z)
        return {"status": "positioned", "part_id": part_id, "position": [x, y, z]}

    def rotate_part(
        self,
        part_id: str,
        rx: float = 0,
        ry: float = 0,
        rz: float = 0,
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Set the rotation of a part in degrees.

        Args:
            part_id: Part to rotate
            rx: Rotation around X axis in degrees
            ry: Rotation around Y axis in degrees
            rz: Rotation around Z axis in degrees
            project_name: Project name (uses active if not specified)

        Returns:
            Updated part info
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        part = project.get_part(part_id)
        if not part:
            return {"error": f"Part '{part_id}' not found"}

        part.rotation = (rx, ry, rz)
        return {"status": "rotated", "part_id": part_id, "rotation": [rx, ry, rz]}

    def suggest_joinery(
        self,
        part_a_id: str,
        part_b_id: str,
        joint_location: str = "edge",
        project_name: str | None = None,
    ) -> dict[str, Any]:
        """Suggest appropriate joinery for connecting two parts.

        Args:
            part_a_id: First part ID
            part_b_id: Second part ID
            joint_location: Where joint occurs ('edge', 'face', 'end', 'corner')
            project_name: Project name (uses active if not specified)

        Returns:
            Suggested joinery options
        """
        project = self.manager.get_project(project_name)
        if not project:
            return {"error": "No project found"}

        part_a = project.get_part(part_a_id)
        part_b = project.get_part(part_b_id)

        if not part_a or not part_b:
            return {"error": "One or both parts not found"}

        suggestions = []

        # Analyze dimensions and suggest joints
        a_thick = part_a.dimensions.thickness
        b_thick = part_b.dimensions.thickness

        if joint_location == "edge":
            # Edge-to-edge joints
            suggestions.append({
                "type": "dado",
                "strength": "high",
                "difficulty": "medium",
                "notes": f"Cut {b_thick}\" wide dado in {part_a_id}",
            })
            suggestions.append({
                "type": "pocket_hole",
                "strength": "medium",
                "difficulty": "easy",
                "notes": "Quick assembly with concealed fasteners",
            })
            suggestions.append({
                "type": "biscuit",
                "strength": "medium",
                "difficulty": "easy",
                "notes": "Good for alignment, add glue for strength",
            })

        elif joint_location == "corner":
            suggestions.append({
                "type": "miter",
                "strength": "low",
                "difficulty": "medium",
                "notes": "Clean appearance, reinforce with splines",
            })
            suggestions.append({
                "type": "rabbet",
                "strength": "medium",
                "difficulty": "easy",
                "notes": "Strong corner joint, partially visible",
            })
            suggestions.append({
                "type": "box_joint",
                "strength": "high",
                "difficulty": "hard",
                "notes": "Strong and decorative finger joints",
            })

        elif joint_location == "end":
            suggestions.append({
                "type": "mortise_tenon",
                "strength": "very_high",
                "difficulty": "hard",
                "notes": "Traditional strong joint for frames",
            })
            suggestions.append({
                "type": "loose_tenon",
                "strength": "high",
                "difficulty": "medium",
                "notes": "Domino-style, easier than traditional M&T",
            })
            suggestions.append({
                "type": "dowel",
                "strength": "medium",
                "difficulty": "medium",
                "notes": "Traditional, requires accurate alignment",
            })

        elif joint_location == "face":
            suggestions.append({
                "type": "groove",
                "strength": "high",
                "difficulty": "medium",
                "notes": "For panel insertion (backs, bottoms)",
            })
            suggestions.append({
                "type": "sliding_dovetail",
                "strength": "very_high",
                "difficulty": "hard",
                "notes": "Strong shelf joint, resists pull-out",
            })

        return {"suggestions": suggestions}
