"""Assembly guide and instructions generator."""

from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from woodcraft.engine.modeler import Project
from woodcraft.engine.joinery import JoineryType


@dataclass
class AssemblyStep:
    """A single step in assembly instructions."""

    step_number: int
    title: str
    description: str
    parts_involved: list[str] = field(default_factory=list)
    tools_required: list[str] = field(default_factory=list)
    tips: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)
    image_ref: str = ""  # Reference to diagram/image

    def to_dict(self) -> dict[str, Any]:
        return {
            "step": self.step_number,
            "title": self.title,
            "description": self.description,
            "parts": self.parts_involved,
            "tools": self.tools_required,
            "tips": self.tips,
            "warnings": self.warnings,
            "image": self.image_ref,
        }


@dataclass
class AssemblyGuide:
    """Complete assembly guide for a project."""

    project_name: str
    introduction: str = ""
    tools_list: list[str] = field(default_factory=list)
    materials_prep: list[str] = field(default_factory=list)
    steps: list[AssemblyStep] = field(default_factory=list)
    finishing_notes: str = ""
    safety_notes: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "project_name": self.project_name,
            "introduction": self.introduction,
            "tools": self.tools_list,
            "materials_prep": self.materials_prep,
            "steps": [s.to_dict() for s in self.steps],
            "finishing": self.finishing_notes,
            "safety": self.safety_notes,
        }


class GuideGenerator:
    """Generates assembly guides from project definitions."""

    # Common tools by joinery type
    JOINERY_TOOLS: dict[JoineryType, list[str]] = {
        JoineryType.DADO: ["Table saw with dado blade", "Router with straight bit"],
        JoineryType.RABBET: ["Table saw with dado blade", "Router with rabbeting bit"],
        JoineryType.GROOVE: ["Table saw", "Router with straight bit"],
        JoineryType.MORTISE_TENON: [
            "Drill press or mortising machine",
            "Chisels",
            "Table saw or bandsaw",
        ],
        JoineryType.POCKET_HOLE: ["Pocket hole jig", "Drill driver"],
        JoineryType.DOWEL: ["Doweling jig", "Drill"],
        JoineryType.BISCUIT: ["Biscuit joiner"],
        JoineryType.THROUGH_DOVETAIL: ["Dovetail saw", "Chisels", "Marking gauge"],
        JoineryType.BOX_JOINT: ["Table saw with dado blade", "Box joint jig"],
    }

    # Standard safety notes
    STANDARD_SAFETY = [
        "Always wear safety glasses when operating power tools",
        "Use hearing protection when operating loud machinery",
        "Keep work area clean and well-lit",
        "Ensure all guards are in place before operating power tools",
        "Never reach over spinning blades",
        "Use push sticks when cutting narrow pieces on the table saw",
    ]

    def __init__(self, project: Project):
        self.project = project

    def generate(self) -> AssemblyGuide:
        """Generate complete assembly guide."""
        guide = AssemblyGuide(
            project_name=self.project.name,
            safety_notes=self.STANDARD_SAFETY.copy(),
        )

        # Generate introduction
        guide.introduction = self._generate_introduction()

        # Collect tools
        guide.tools_list = self._collect_tools()

        # Generate material prep steps
        guide.materials_prep = self._generate_prep_steps()

        # Generate assembly steps from joinery
        guide.steps = self._generate_assembly_steps()

        # Generate finishing notes
        guide.finishing_notes = self._generate_finishing_notes()

        return guide

    def _generate_introduction(self) -> str:
        """Generate introduction section."""
        num_parts = len(self.project.parts)
        num_joints = len(self.project.joinery)
        material = self.project.material.species.replace("_", " ").title()

        intro = f"This guide covers the assembly of the {self.project.name}, "
        intro += f"consisting of {num_parts} parts "
        intro += f"made from {material}. "

        if num_joints > 0:
            intro += f"The project uses {num_joints} joinery connections."

        return intro

    def _collect_tools(self) -> list[str]:
        """Collect all required tools."""
        tools: set[str] = {
            "Tape measure",
            "Pencil",
            "Square",
            "Clamps (various sizes)",
            "Sandpaper (80, 120, 180, 220 grit)",
        }

        # Add tools for cutting parts
        has_panels = any(
            p.dimensions.width > 6 for p in self.project.parts
        )
        if has_panels:
            tools.add("Table saw or circular saw with guide")
        else:
            tools.add("Miter saw or table saw")

        # Add joinery-specific tools
        for joint in self.project.joinery:
            joint_type = JoineryType(joint.get("type", "butt"))
            if joint_type in self.JOINERY_TOOLS:
                tools.update(self.JOINERY_TOOLS[joint_type])

        return sorted(tools)

    def _generate_prep_steps(self) -> list[str]:
        """Generate material preparation steps."""
        steps = []

        # Mill lumber if needed
        if self.project.material.species not in ["plywood", "mdf", "particle_board"]:
            steps.append(
                "Mill all lumber to final thickness and allow to acclimate for 24-48 hours"
            )

        # Cut to rough size
        steps.append(
            "Cut all parts to rough length (1\" longer than final dimension)"
        )

        # Joint and plane
        steps.append("Joint one face and one edge of each board")
        steps.append("Plane to final thickness")

        # Cut to final size
        steps.append("Cut all parts to final dimensions per the cut list")

        # Mark parts
        steps.append("Label each part with its ID using painter's tape")

        return steps

    def _generate_assembly_steps(self) -> list[AssemblyStep]:
        """Generate assembly steps from joinery."""
        steps: list[AssemblyStep] = []
        step_num = 1

        # Group joints by sub-assembly if possible
        for joint in self.project.joinery:
            joint_type = JoineryType(joint.get("type", "butt"))
            part_a = joint.get("part_a", "")
            part_b = joint.get("part_b", "")

            # Create joinery cut step
            cut_step = AssemblyStep(
                step_number=step_num,
                title=f"Cut {joint_type.value.replace('_', ' ')} joint",
                description=self._get_joinery_description(joint_type, part_a, part_b),
                parts_involved=[part_a, part_b],
                tools_required=self.JOINERY_TOOLS.get(joint_type, []).copy(),
                tips=self._get_joinery_tips(joint_type),
            )
            steps.append(cut_step)
            step_num += 1

        # Add dry fit step
        if steps:
            dry_fit = AssemblyStep(
                step_number=step_num,
                title="Dry fit assembly",
                description="Assemble all parts without glue to verify fit. "
                "Check for gaps, alignment issues, and square.",
                parts_involved=[p.id for p in self.project.parts],
                tools_required=["Clamps", "Square"],
                tips=[
                    "Mark orientation of parts with chalk triangle",
                    "Measure diagonals to check for square",
                ],
            )
            steps.append(dry_fit)
            step_num += 1

        # Add glue-up step
        glue_up = AssemblyStep(
            step_number=step_num,
            title="Glue and clamp assembly",
            description="Apply wood glue to all joint surfaces and clamp assembly. "
            "Allow to cure per glue manufacturer's instructions.",
            parts_involved=[p.id for p in self.project.parts],
            tools_required=["Wood glue", "Clamps", "Damp rag for squeeze-out"],
            tips=[
                "Apply glue to both surfaces for maximum strength",
                "Don't over-tighten clamps - this squeezes out too much glue",
                "Wipe excess glue while still wet",
            ],
            warnings=[
                "Work efficiently - most wood glues have 10-15 minute open time"
            ],
        )
        steps.append(glue_up)

        return steps

    def _get_joinery_description(
        self, joint_type: JoineryType, part_a: str, part_b: str
    ) -> str:
        """Get description for joinery operation."""
        descriptions = {
            JoineryType.DADO: f"Cut a dado in {part_a} to receive {part_b}. "
            "Set blade height to 1/3 to 1/2 the thickness of the board.",
            JoineryType.RABBET: f"Cut a rabbet along the edge of {part_a} "
            f"sized to receive {part_b}.",
            JoineryType.GROOVE: f"Cut a groove in {part_a} to accept {part_b}. "
            "Groove should run with the grain.",
            JoineryType.MORTISE_TENON: f"Cut mortise in {part_a} and tenon on {part_b}. "
            "Mortise width should be 1/3 the board thickness.",
            JoineryType.POCKET_HOLE: f"Drill pocket holes in {part_b} to attach to {part_a}.",
            JoineryType.DOWEL: f"Drill aligned dowel holes in {part_a} and {part_b}.",
            JoineryType.BISCUIT: f"Cut biscuit slots in {part_a} and {part_b}.",
        }
        return descriptions.get(
            joint_type,
            f"Join {part_a} to {part_b} using {joint_type.value.replace('_', ' ')}.",
        )

    def _get_joinery_tips(self, joint_type: JoineryType) -> list[str]:
        """Get tips for joinery type."""
        tips: dict[JoineryType, list[str]] = {
            JoineryType.DADO: [
                "Make test cuts in scrap to verify fit",
                "Joint should be snug but not require force",
            ],
            JoineryType.MORTISE_TENON: [
                "Cut mortise slightly deeper than tenon length for glue reservoir",
                "Test fit and pare tenon cheeks if needed",
            ],
            JoineryType.POCKET_HOLE: [
                "Use correct screw length for material thickness",
                "Clamp parts tightly when driving screws",
            ],
        }
        return tips.get(joint_type, [])

    def _generate_finishing_notes(self) -> str:
        """Generate finishing section."""
        finish = self.project.material.finish

        if finish == "none" or not finish:
            return "Sand through grits 80, 120, 180, 220. Apply finish of your choice."

        notes = "After assembly:\n"
        notes += "1. Fill any gaps or defects with wood filler\n"
        notes += "2. Sand through grits 80, 120, 180, 220\n"
        notes += "3. Remove all dust with tack cloth\n"

        if finish == "polyurethane":
            notes += "4. Apply thin coat of polyurethane\n"
            notes += "5. Sand lightly with 320 grit between coats\n"
            notes += "6. Apply 2-3 additional coats"
        elif finish == "oil":
            notes += "4. Apply penetrating oil finish\n"
            notes += "5. Allow to soak, wipe off excess\n"
            notes += "6. Apply additional coats as desired"
        elif finish == "lacquer":
            notes += "4. Apply light coats of lacquer\n"
            notes += "5. Sand with 320 grit between coats\n"
            notes += "6. Build to desired sheen"
        elif finish == "paint":
            notes += "4. Apply primer and sand smooth\n"
            notes += "5. Apply 2-3 coats of paint\n"
            notes += "6. Sand lightly between coats"

        return notes

    def export_markdown(self, guide: AssemblyGuide, output_path: Path) -> Path:
        """Export guide as Markdown file."""
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        lines = [
            f"# {guide.project_name} - Assembly Guide",
            "",
            "## Introduction",
            guide.introduction,
            "",
            "## Safety Notes",
        ]

        for note in guide.safety_notes:
            lines.append(f"- {note}")

        lines.extend(
            [
                "",
                "## Tools Required",
            ]
        )

        for tool in guide.tools_list:
            lines.append(f"- {tool}")

        lines.extend(
            [
                "",
                "## Material Preparation",
            ]
        )

        for i, prep in enumerate(guide.materials_prep, 1):
            lines.append(f"{i}. {prep}")

        lines.extend(
            [
                "",
                "## Assembly Steps",
            ]
        )

        for step in guide.steps:
            lines.append(f"### Step {step.step_number}: {step.title}")
            lines.append("")
            lines.append(step.description)
            lines.append("")

            if step.parts_involved:
                lines.append(f"**Parts:** {', '.join(step.parts_involved)}")
            if step.tools_required:
                lines.append(f"**Tools:** {', '.join(step.tools_required)}")

            if step.tips:
                lines.append("")
                lines.append("**Tips:**")
                for tip in step.tips:
                    lines.append(f"- {tip}")

            if step.warnings:
                lines.append("")
                lines.append("**Warnings:**")
                for warning in step.warnings:
                    lines.append(f"- ⚠️ {warning}")

            lines.append("")

        lines.extend(
            [
                "## Finishing",
                guide.finishing_notes,
            ]
        )

        with open(output_path, "w") as f:
            f.write("\n".join(lines))

        return output_path
