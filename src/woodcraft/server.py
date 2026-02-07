"""Woodcraft MCP Server - CAD and woodworking integration for Claude Code."""

from __future__ import annotations

import asyncio
import json
import logging
from pathlib import Path
from typing import Any

from mcp.server import Server
from mcp.server.stdio import stdio_server
from mcp.types import (
    Tool,
    TextContent,
)

from woodcraft.tools.project import ProjectManager, ProjectTools
from woodcraft.tools.design import DesignTools
from woodcraft.tools.documentation import DocumentationTools
from woodcraft.tools.cutlist import CutListTools
from woodcraft.tools.export import ExportTools

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("woodcraft")


class WoodcraftServer:
    """MCP Server for woodworking CAD operations."""

    def __init__(self, workspace_dir: Path | None = None):
        self.server = Server("woodcraft")
        self.manager = ProjectManager(workspace_dir)

        # Initialize tool handlers
        self.project_tools = ProjectTools(self.manager)
        self.design_tools = DesignTools(self.manager)
        self.documentation_tools = DocumentationTools(self.manager)
        self.cutlist_tools = CutListTools(self.manager)
        self.export_tools = ExportTools(self.manager)

        # Register handlers
        self._register_handlers()

    def _register_handlers(self) -> None:
        """Register all MCP handlers."""

        @self.server.list_tools()
        async def list_tools() -> list[Tool]:
            return self._get_tools()

        @self.server.call_tool()
        async def call_tool(name: str, arguments: dict[str, Any]) -> list[TextContent]:
            result = await self._handle_tool_call(name, arguments)
            return [TextContent(type="text", text=json.dumps(result, indent=2))]

    def _get_tools(self) -> list[Tool]:
        """Get all available tools."""
        tools = [
            # Project tools
            Tool(
                name="create_project",
                description="Create a new woodworking project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "name": {"type": "string", "description": "Project name"},
                        "units": {
                            "type": "string",
                            "enum": ["inches", "mm", "cm"],
                            "default": "inches",
                            "description": "Unit system",
                        },
                        "material_species": {
                            "type": "string",
                            "default": "pine",
                            "description": "Default wood species",
                        },
                        "material_thickness": {
                            "type": "number",
                            "default": 0.75,
                            "description": "Default material thickness",
                        },
                        "material_finish": {
                            "type": "string",
                            "default": "none",
                            "description": "Default finish type",
                        },
                        "notes": {"type": "string", "description": "Project notes"},
                    },
                    "required": ["name"],
                },
            ),
            Tool(
                name="get_project_info",
                description="Get information about a project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "name": {"type": "string", "description": "Project name (uses active if not specified)"},
                    },
                },
            ),
            Tool(
                name="list_projects",
                description="List all projects",
                inputSchema={"type": "object", "properties": {}},
            ),
            Tool(
                name="set_active_project",
                description="Set the active project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["name"],
                },
            ),
            Tool(
                name="add_part",
                description="Add a part to a project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Unique part identifier"},
                        "part_type": {
                            "type": "string",
                            "enum": ["panel", "board", "rail", "stile", "shelf", "top", "bottom", "side", "back", "drawer_front", "drawer_side", "drawer_bottom", "door", "leg", "apron", "stretcher", "custom"],
                            "description": "Type of part",
                        },
                        "length": {"type": "number", "description": "Length dimension"},
                        "width": {"type": "number", "description": "Width dimension"},
                        "thickness": {"type": "number", "description": "Thickness (uses project default if not specified)"},
                        "quantity": {"type": "integer", "default": 1, "description": "Number of this part"},
                        "grain_direction": {
                            "type": "string",
                            "enum": ["length", "width", "none"],
                            "default": "length",
                            "description": "Grain orientation",
                        },
                        "material": {"type": "string", "description": "Material override"},
                        "notes": {"type": "string", "description": "Part notes"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_id", "part_type", "length", "width"],
                },
            ),
            Tool(
                name="remove_part",
                description="Remove a part from a project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part ID to remove"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_id"],
                },
            ),
            Tool(
                name="update_part",
                description="Update an existing part's dimensions or properties",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part ID to update"},
                        "length": {"type": "number", "description": "New length"},
                        "width": {"type": "number", "description": "New width"},
                        "thickness": {"type": "number", "description": "New thickness"},
                        "quantity": {"type": "integer", "description": "New quantity"},
                        "notes": {"type": "string", "description": "New notes"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_id"],
                },
            ),
            Tool(
                name="save_project",
                description="Save project to file",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "project_name": {"type": "string", "description": "Project name"},
                        "filename": {"type": "string", "description": "Output filename"},
                    },
                },
            ),
            Tool(
                name="load_project",
                description="Load project from file",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "filepath": {"type": "string", "description": "Path to project JSON file"},
                    },
                    "required": ["filepath"],
                },
            ),
            # Design tools
            Tool(
                name="add_joinery",
                description="Add joinery between two parts",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "joint_type": {
                            "type": "string",
                            "enum": ["butt", "miter", "dado", "rabbet", "groove", "mortise_tenon", "through_mortise", "loose_tenon", "through_dovetail", "half_blind_dovetail", "sliding_dovetail", "box_joint", "biscuit", "pocket_hole", "dowel", "tongue_groove"],
                            "description": "Type of joint",
                        },
                        "part_a_id": {"type": "string", "description": "First part ID"},
                        "part_b_id": {"type": "string", "description": "Second part ID"},
                        "position_a": {
                            "type": "array",
                            "items": {"type": "number"},
                            "description": "Position on part A [x, y, z]",
                        },
                        "position_b": {
                            "type": "array",
                            "items": {"type": "number"},
                            "description": "Position on part B [x, y, z]",
                        },
                        "parameters": {
                            "type": "object",
                            "description": "Joint-specific parameters",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["joint_type", "part_a_id", "part_b_id"],
                },
            ),
            Tool(
                name="list_joinery_types",
                description="List available joinery types with descriptions",
                inputSchema={"type": "object", "properties": {}},
            ),
            Tool(
                name="create_assembly",
                description="Create an assembly from parts",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "name": {"type": "string", "description": "Assembly name"},
                        "part_ids": {
                            "type": "array",
                            "items": {"type": "string"},
                            "description": "List of part IDs to include",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["name"],
                },
            ),
            Tool(
                name="validate_design",
                description="Validate design for common issues",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="position_part",
                description="Set the 3D position of a part",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part to position"},
                        "x": {"type": "number", "description": "X coordinate"},
                        "y": {"type": "number", "description": "Y coordinate"},
                        "z": {"type": "number", "description": "Z coordinate"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_id", "x", "y", "z"],
                },
            ),
            Tool(
                name="suggest_joinery",
                description="Get joinery suggestions for connecting two parts",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_a_id": {"type": "string", "description": "First part ID"},
                        "part_b_id": {"type": "string", "description": "Second part ID"},
                        "joint_location": {
                            "type": "string",
                            "enum": ["edge", "face", "end", "corner"],
                            "default": "edge",
                            "description": "Where the joint occurs",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_a_id", "part_b_id"],
                },
            ),
            # Documentation tools
            Tool(
                name="generate_drawing",
                description="Generate a dimensioned drawing for a part",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part to draw"},
                        "views": {
                            "type": "array",
                            "items": {"type": "string", "enum": ["top", "front", "side"]},
                            "description": "Views to include",
                        },
                        "output_format": {
                            "type": "string",
                            "enum": ["dxf", "svg"],
                            "default": "dxf",
                            "description": "Output format",
                        },
                        "scale": {"type": "number", "default": 1.0, "description": "Drawing scale"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_id"],
                },
            ),
            Tool(
                name="generate_all_drawings",
                description="Generate drawings for all parts",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "output_format": {
                            "type": "string",
                            "enum": ["dxf", "svg"],
                            "default": "dxf",
                            "description": "Output format",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="generate_bom",
                description="Generate bill of materials",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "output_format": {
                            "type": "string",
                            "enum": ["json", "csv", "text"],
                            "default": "json",
                            "description": "Output format",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="generate_assembly_guide",
                description="Generate assembly instructions",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "output_format": {
                            "type": "string",
                            "enum": ["markdown", "json"],
                            "default": "markdown",
                            "description": "Output format",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="add_hardware",
                description="Add hardware item to project BOM",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "name": {"type": "string", "description": "Hardware name"},
                        "quantity": {"type": "integer", "description": "Number needed"},
                        "unit_cost": {"type": "number", "default": 0, "description": "Cost per unit"},
                        "description": {"type": "string", "description": "Description"},
                        "supplier": {"type": "string", "description": "Supplier name"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["name", "quantity"],
                },
            ),
            Tool(
                name="calculate_lumber",
                description="Calculate lumber requirements for the project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            # Cut list tools
            Tool(
                name="generate_cutlist",
                description="Generate optimized cut list for sheet goods",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "stock_length": {"type": "number", "description": "Stock sheet length"},
                        "stock_width": {"type": "number", "description": "Stock sheet width"},
                        "stock_material": {"type": "string", "default": "plywood", "description": "Material type"},
                        "stock_thickness": {"type": "number", "default": 0.75, "description": "Stock thickness"},
                        "kerf": {"type": "number", "default": 0.125, "description": "Saw blade width"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["stock_length", "stock_width"],
                },
            ),
            Tool(
                name="generate_cutlist_svg",
                description="Generate cut list with SVG visualization",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "stock_length": {"type": "number", "description": "Stock sheet length"},
                        "stock_width": {"type": "number", "description": "Stock sheet width"},
                        "stock_material": {"type": "string", "default": "plywood", "description": "Material type"},
                        "stock_thickness": {"type": "number", "default": 0.75, "description": "Stock thickness"},
                        "kerf": {"type": "number", "default": 0.125, "description": "Saw blade width"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["stock_length", "stock_width"],
                },
            ),
            Tool(
                name="generate_linear_cutlist",
                description="Generate cut list for linear stock (boards)",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "stock_lengths": {
                            "type": "array",
                            "items": {"type": "number"},
                            "description": "Available stock lengths",
                        },
                        "kerf": {"type": "number", "default": 0.125, "description": "Saw blade width"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["stock_lengths"],
                },
            ),
            Tool(
                name="get_standard_sheet_sizes",
                description="Get standard sheet good sizes",
                inputSchema={"type": "object", "properties": {}},
            ),
            Tool(
                name="get_standard_lumber_sizes",
                description="Get standard dimensional lumber sizes",
                inputSchema={"type": "object", "properties": {}},
            ),
            # Export tools
            Tool(
                name="export_step",
                description="Export to STEP format for CAD import",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part to export (exports assembly if not specified)"},
                        "filename": {"type": "string", "description": "Output filename"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="export_stl",
                description="Export to STL format for 3D printing",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part to export"},
                        "filename": {"type": "string", "description": "Output filename"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="export_dxf",
                description="Export 2D DXF projection of a part",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "part_id": {"type": "string", "description": "Part to export"},
                        "view": {
                            "type": "string",
                            "enum": ["top", "front", "side"],
                            "default": "top",
                            "description": "Projection view",
                        },
                        "filename": {"type": "string", "description": "Output filename"},
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                    "required": ["part_id"],
                },
            ),
            Tool(
                name="export_all_parts",
                description="Export all parts as individual files",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "format": {
                            "type": "string",
                            "enum": ["step", "stl"],
                            "default": "step",
                            "description": "Output format",
                        },
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="list_exports",
                description="List all exported files for a project",
                inputSchema={
                    "type": "object",
                    "properties": {
                        "project_name": {"type": "string", "description": "Project name"},
                    },
                },
            ),
            Tool(
                name="get_supported_formats",
                description="Get list of supported export formats",
                inputSchema={"type": "object", "properties": {}},
            ),
        ]
        return tools

    async def _handle_tool_call(self, name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        """Handle a tool call."""
        logger.info(f"Tool call: {name} with args: {arguments}")

        try:
            # Project tools
            if name == "create_project":
                return self.project_tools.create_project(**arguments)
            elif name == "get_project_info":
                return self.project_tools.get_project_info(**arguments)
            elif name == "list_projects":
                return self.project_tools.list_projects()
            elif name == "set_active_project":
                return self.project_tools.set_active_project(**arguments)
            elif name == "add_part":
                return self.project_tools.add_part(**arguments)
            elif name == "remove_part":
                return self.project_tools.remove_part(**arguments)
            elif name == "update_part":
                return self.project_tools.update_part(**arguments)
            elif name == "save_project":
                return self.project_tools.save_project(**arguments)
            elif name == "load_project":
                return self.project_tools.load_project(**arguments)

            # Design tools
            elif name == "add_joinery":
                return self.design_tools.add_joinery(**arguments)
            elif name == "list_joinery_types":
                return self.design_tools.list_joinery_types()
            elif name == "create_assembly":
                return self.design_tools.create_assembly(**arguments)
            elif name == "validate_design":
                return self.design_tools.validate_design(**arguments)
            elif name == "position_part":
                return self.design_tools.position_part(**arguments)
            elif name == "suggest_joinery":
                return self.design_tools.suggest_joinery(**arguments)

            # Documentation tools
            elif name == "generate_drawing":
                return self.documentation_tools.generate_drawing(**arguments)
            elif name == "generate_all_drawings":
                return self.documentation_tools.generate_all_drawings(**arguments)
            elif name == "generate_bom":
                return self.documentation_tools.generate_bom(**arguments)
            elif name == "generate_assembly_guide":
                return self.documentation_tools.generate_assembly_guide(**arguments)
            elif name == "add_hardware":
                return self.documentation_tools.add_hardware(**arguments)
            elif name == "calculate_lumber":
                return self.documentation_tools.calculate_lumber(**arguments)

            # Cut list tools
            elif name == "generate_cutlist":
                return self.cutlist_tools.generate_cutlist(**arguments)
            elif name == "generate_cutlist_svg":
                return self.cutlist_tools.generate_cutlist_svg(**arguments)
            elif name == "generate_linear_cutlist":
                return self.cutlist_tools.generate_linear_cutlist(**arguments)
            elif name == "get_standard_sheet_sizes":
                return self.cutlist_tools.get_standard_sheet_sizes()
            elif name == "get_standard_lumber_sizes":
                return self.cutlist_tools.get_standard_lumber_sizes()

            # Export tools
            elif name == "export_step":
                return self.export_tools.export_step(**arguments)
            elif name == "export_stl":
                return self.export_tools.export_stl(**arguments)
            elif name == "export_dxf":
                return self.export_tools.export_dxf(**arguments)
            elif name == "export_all_parts":
                return self.export_tools.export_all_parts(**arguments)
            elif name == "list_exports":
                return self.export_tools.list_exports(**arguments)
            elif name == "get_supported_formats":
                return self.export_tools.get_supported_formats()

            else:
                return {"error": f"Unknown tool: {name}"}

        except Exception as e:
            logger.exception(f"Error handling tool call {name}")
            return {"error": str(e)}

    async def run(self) -> None:
        """Run the MCP server."""
        async with stdio_server() as (read_stream, write_stream):
            await self.server.run(
                read_stream,
                write_stream,
                self.server.create_initialization_options(),
            )


def main() -> None:
    """Main entry point."""
    import argparse

    parser = argparse.ArgumentParser(description="Woodcraft MCP Server")
    parser.add_argument(
        "--workspace",
        type=Path,
        help="Workspace directory for projects",
    )
    args = parser.parse_args()

    server = WoodcraftServer(workspace_dir=args.workspace)
    asyncio.run(server.run())


if __name__ == "__main__":
    main()
