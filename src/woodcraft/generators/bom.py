"""Bill of Materials generator."""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

from woodcraft.engine.modeler import Project
from woodcraft.utils.units import UnitConverter, Units


@dataclass
class BOMItem:
    """A single item in the bill of materials."""

    name: str
    description: str
    quantity: int | float
    unit: str  # "each", "board_feet", "linear_feet", "square_feet"
    unit_cost: float = 0.0
    supplier: str = ""
    notes: str = ""

    @property
    def total_cost(self) -> float:
        return self.quantity * self.unit_cost

    def to_dict(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "description": self.description,
            "quantity": self.quantity,
            "unit": self.unit,
            "unit_cost": self.unit_cost,
            "total_cost": self.total_cost,
            "supplier": self.supplier,
            "notes": self.notes,
        }


@dataclass
class BOMCategory:
    """A category of BOM items."""

    name: str
    items: list[BOMItem] = field(default_factory=list)

    @property
    def subtotal(self) -> float:
        return sum(item.total_cost for item in self.items)

    def to_dict(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "items": [item.to_dict() for item in self.items],
            "subtotal": self.subtotal,
        }


@dataclass
class BillOfMaterials:
    """Complete bill of materials for a project."""

    project_name: str
    categories: list[BOMCategory] = field(default_factory=list)
    notes: str = ""

    @property
    def total_cost(self) -> float:
        return sum(cat.subtotal for cat in self.categories)

    def to_dict(self) -> dict[str, Any]:
        return {
            "project_name": self.project_name,
            "categories": [cat.to_dict() for cat in self.categories],
            "total_cost": self.total_cost,
            "notes": self.notes,
        }


class BOMGenerator:
    """Generates bill of materials from project."""

    # Default costs per unit (in USD)
    DEFAULT_LUMBER_COSTS = {
        "pine": 3.50,  # per board foot
        "poplar": 4.00,
        "red_oak": 6.50,
        "white_oak": 7.50,
        "hard_maple": 7.00,
        "soft_maple": 5.50,
        "cherry": 9.00,
        "walnut": 12.00,
        "ash": 5.50,
        "birch": 5.00,
        "hickory": 6.00,
    }

    DEFAULT_SHEET_COSTS = {
        "plywood_3/4": 55.00,  # per sheet
        "plywood_1/2": 45.00,
        "plywood_1/4": 30.00,
        "mdf_3/4": 35.00,
        "mdf_1/2": 28.00,
        "particle_board_3/4": 25.00,
    }

    def __init__(
        self,
        project: Project,
        lumber_costs: dict[str, float] | None = None,
        sheet_costs: dict[str, float] | None = None,
    ):
        self.project = project
        self.lumber_costs = lumber_costs or self.DEFAULT_LUMBER_COSTS
        self.sheet_costs = sheet_costs or self.DEFAULT_SHEET_COSTS
        self._species_data: dict[str, Any] = {}
        self._load_species_data()

    def _load_species_data(self) -> None:
        """Load species data from JSON file."""
        data_path = Path(__file__).parent.parent / "data" / "species.json"
        if data_path.exists():
            with open(data_path) as f:
                data = json.load(f)
                self._species_data = data.get("species", {})

    def generate(self) -> BillOfMaterials:
        """Generate complete bill of materials."""
        bom = BillOfMaterials(project_name=self.project.name)

        # Generate lumber category
        lumber_cat = self._generate_lumber_category()
        if lumber_cat.items:
            bom.categories.append(lumber_cat)

        # Generate hardware category from project hardware
        if self.project.hardware:
            hardware_cat = self._generate_hardware_category()
            if hardware_cat.items:
                bom.categories.append(hardware_cat)

        return bom

    def _generate_lumber_category(self) -> BOMCategory:
        """Generate lumber/materials category."""
        category = BOMCategory(name="Lumber & Sheet Goods")

        # Group parts by material and thickness
        material_groups: dict[str, dict[str, list]] = {}

        for part in self.project.parts:
            material = part.material or self.project.material.species
            thickness = part.dimensions.thickness

            key = f"{material}_{thickness}"
            if key not in material_groups:
                material_groups[key] = {
                    "material": material,
                    "thickness": thickness,
                    "parts": [],
                }
            material_groups[key]["parts"].append(part)

        # Calculate board feet for each group
        for key, group in material_groups.items():
            total_bf = 0.0
            part_list = []

            for part in group["parts"]:
                bf = UnitConverter.board_feet(
                    part.dimensions.length,
                    part.dimensions.width,
                    part.dimensions.thickness,
                    self.project.units,
                )
                total_bf += bf * part.quantity
                part_list.append(part.id)

            material = group["material"]
            thickness = group["thickness"]

            # Get unit cost
            unit_cost = self.lumber_costs.get(material, 5.0)

            # Get species display name
            species_info = self._species_data.get(material, {})
            display_name = species_info.get("name", material.replace("_", " ").title())

            item = BOMItem(
                name=f"{display_name} ({thickness}\" thick)",
                description=f"Parts: {', '.join(part_list)}",
                quantity=round(total_bf * 1.15, 2),  # Add 15% waste factor
                unit="board_feet",
                unit_cost=unit_cost,
                notes="Includes 15% waste factor",
            )
            category.items.append(item)

        return category

    def _generate_hardware_category(self) -> BOMCategory:
        """Generate hardware category from project hardware list."""
        category = BOMCategory(name="Hardware")

        for hw in self.project.hardware:
            item = BOMItem(
                name=hw.get("name", "Unknown"),
                description=hw.get("description", ""),
                quantity=hw.get("quantity", 1),
                unit=hw.get("unit", "each"),
                unit_cost=hw.get("cost", 0.0),
                supplier=hw.get("supplier", ""),
                notes=hw.get("notes", ""),
            )
            category.items.append(item)

        return category

    def add_hardware(
        self,
        name: str,
        quantity: int,
        unit_cost: float = 0.0,
        description: str = "",
        supplier: str = "",
    ) -> None:
        """Add hardware item to project."""
        self.project.hardware.append(
            {
                "name": name,
                "description": description,
                "quantity": quantity,
                "unit": "each",
                "cost": unit_cost,
                "supplier": supplier,
            }
        )

    def export_csv(self, bom: BillOfMaterials, output_path: Path) -> Path:
        """Export BOM to CSV file."""
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        lines = [
            "Category,Item,Description,Quantity,Unit,Unit Cost,Total Cost,Supplier,Notes"
        ]

        for category in bom.categories:
            for item in category.items:
                line = (
                    f'"{category.name}",'
                    f'"{item.name}",'
                    f'"{item.description}",'
                    f"{item.quantity},"
                    f'"{item.unit}",'
                    f"{item.unit_cost:.2f},"
                    f"{item.total_cost:.2f},"
                    f'"{item.supplier}",'
                    f'"{item.notes}"'
                )
                lines.append(line)

        # Add total
        lines.append("")
        lines.append(f",,,,,,{bom.total_cost:.2f},,TOTAL")

        with open(output_path, "w") as f:
            f.write("\n".join(lines))

        return output_path

    def export_json(self, bom: BillOfMaterials, output_path: Path) -> Path:
        """Export BOM to JSON file."""
        output_path = Path(output_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)

        with open(output_path, "w") as f:
            json.dump(bom.to_dict(), f, indent=2)

        return output_path

    def generate_summary(self, bom: BillOfMaterials) -> str:
        """Generate a text summary of the BOM."""
        lines = [
            f"Bill of Materials: {bom.project_name}",
            "=" * 50,
            "",
        ]

        for category in bom.categories:
            lines.append(f"{category.name}")
            lines.append("-" * len(category.name))

            for item in category.items:
                qty_str = f"{item.quantity} {item.unit}"
                if item.unit_cost > 0:
                    lines.append(
                        f"  {item.name}: {qty_str} @ ${item.unit_cost:.2f} = ${item.total_cost:.2f}"
                    )
                else:
                    lines.append(f"  {item.name}: {qty_str}")
                if item.description:
                    lines.append(f"    {item.description}")

            if category.subtotal > 0:
                lines.append(f"  Subtotal: ${category.subtotal:.2f}")
            lines.append("")

        lines.append("=" * 50)
        lines.append(f"TOTAL ESTIMATED COST: ${bom.total_cost:.2f}")

        return "\n".join(lines)
