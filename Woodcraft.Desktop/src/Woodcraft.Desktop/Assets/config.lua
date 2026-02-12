-- Woodcraft Configuration
-- Edit this file to customize default values. Delete any section to use built-in defaults.
-- Changes take effect on next app launch.

------------------------------------------------------
-- MATERIALS
------------------------------------------------------
materials = {
    -- Board-foot pricing by species (used for cost estimates)
    cost_per_bf = {
        pine = 3.50,
        poplar = 4.00,
        soft_maple = 5.00,
        red_oak = 6.50,
        white_oak = 7.50,
        hard_maple = 7.00,
        cherry = 8.50,
        walnut = 12.00,
        ash = 6.00,
        birch = 5.50,
        hickory = 7.00,
    },

    -- Sheet goods pricing per sheet
    sheet_cost = {
        plywood = 45.00,
        mdf = 30.00,
    },

    -- Fallback cost when a material isn't listed above
    fallback_bf_cost = 5.0,
    fallback_sheet_cost = 40.0,

    -- Material catalog: each entry defines a species available in the UI
    -- id       = internal key (lowercase, underscores)
    -- display  = human-readable name shown in dropdowns
    -- price    = rough price tier ($, $$, $$$)
    -- color    = hex color used for 3D view wood grain
    catalog = {
        { id = "pine",       display = "Pine",       price = "$",   color = "#F5DEB3" },
        { id = "red_oak",    display = "Red Oak",    price = "$$",  color = "#C4956A" },
        { id = "white_oak",  display = "White Oak",  price = "$$",  color = "#D4A76A" },
        { id = "hard_maple", display = "Hard Maple", price = "$$",  color = "#E8D5B7" },
        { id = "soft_maple", display = "Soft Maple", price = "$$",  color = "#DEC9A4" },
        { id = "cherry",     display = "Cherry",     price = "$$$", color = "#B5651D" },
        { id = "walnut",     display = "Walnut",     price = "$$$", color = "#5C4033" },
        { id = "poplar",     display = "Poplar",     price = "$",   color = "#C9B99A" },
        { id = "ash",        display = "Ash",        price = "$$",  color = "#D2C6A5" },
        { id = "birch",      display = "Birch",      price = "$$",  color = "#E6D5B8" },
        { id = "hickory",    display = "Hickory",    price = "$$",  color = "#C49A6C" },
        { id = "plywood",    display = "Plywood",    price = "$",   color = "#D2B48C" },
        { id = "mdf",        display = "MDF",        price = "$",   color = "#B8956A" },
    },

    -- Offsets applied to base color for lighter/darker wood grain bands
    color_lighter = { r = 25, g = 20, b = 15 },
    color_darker  = { r = 30, g = 25, b = 20 },
}

------------------------------------------------------
-- CUT LIST
------------------------------------------------------
cutlist = {
    stock_length    = 96,      -- default stock length (inches)
    stock_width     = 48,      -- default stock width (inches)
    stock_thickness = 0.75,
    stock_material  = "plywood",
    kerf            = 0.125,   -- blade kerf (inches)

    -- Stock presets shown as quick-pick buttons
    presets = {
        { name = "Full Sheet (4'\u00d78')",    length = 96, width = 48 },
        { name = "Half Sheet (4'\u00d74')",    length = 48, width = 48 },
        { name = "Quarter Sheet (2'\u00d74')", length = 48, width = 24 },
        { name = "Project Panel (2'\u00d72')", length = 24, width = 24 },
    },

    -- Materials shown in the cut-list material dropdown
    materials = { "plywood", "mdf", "particle_board", "melamine" },
}

------------------------------------------------------
-- PARTS (New Part dialog defaults)
------------------------------------------------------
parts = {
    default_length    = 24,
    default_width     = 12,
    default_thickness = 0.75,
    default_quantity  = 1,
    default_material  = "pine",

    -- Quick-thickness buttons
    thicknesses = { "1/4", "3/8", "1/2", "5/8", "3/4", "1", "1-1/4", "1-1/2" },

    -- Presets shown in the New Part dialog
    presets = {
        { name = "Shelf",           part_type = "Shelf",       length = 36, width = 10, thickness = 0.75, material = "pine", description = "Standard shelf" },
        { name = "Side Panel",      part_type = "Side",        length = 36, width = 12, thickness = 0.75, material = "pine", description = "Cabinet side" },
        { name = "Top/Bottom",      part_type = "Top",         length = 36, width = 12, thickness = 0.75, material = "pine", description = "Cabinet top or bottom" },
        { name = "Drawer Front",    part_type = "DrawerFront", length = 18, width = 6,  thickness = 0.75, material = "pine", description = "Drawer face" },
        { name = "Drawer Side",     part_type = "DrawerSide",  length = 18, width = 6,  thickness = 0.5,  material = "pine", description = "Drawer side panel" },
        { name = "Drawer Bottom",   part_type = "DrawerBottom",length = 17, width = 15, thickness = 0.25, material = "plywood", description = "Drawer floor" },
        { name = "Back Panel",      part_type = "Back",        length = 36, width = 24, thickness = 0.25, material = "plywood", description = "Cabinet back" },
        { name = "Door",            part_type = "Door",        length = 24, width = 18, thickness = 0.75, material = "pine", description = "Cabinet door" },
        { name = "Face Frame Rail", part_type = "Rail",        length = 36, width = 2,  thickness = 0.75, material = "pine", description = "Horizontal frame piece" },
        { name = "Face Frame Stile",part_type = "Stile",       length = 24, width = 2,  thickness = 0.75, material = "pine", description = "Vertical frame piece" },
    },
}

------------------------------------------------------
-- PROJECTS (New Project dialog defaults)
------------------------------------------------------
projects = {
    default_name     = "My Project",
    default_units    = "inches",
    default_material = "pine",

    -- Project templates
    templates = {
        { name = "Empty Project",     description = "Start with a blank project",                           parts = {},                                                                                                                     id = "empty" },
        { name = "Simple Bookshelf",  description = "Basic bookshelf with 2 sides, top, bottom, and 2 shelves", parts = { "left_side", "right_side", "top", "bottom", "shelf_1", "shelf_2" },                                               id = "bookshelf" },
        { name = "Wall Cabinet",      description = "Kitchen-style wall cabinet with door",                 parts = { "left_side", "right_side", "top", "bottom", "back", "shelf", "door" },                                                id = "cabinet" },
        { name = "Drawer Box",        description = "Simple drawer with front, sides, back, and bottom",    parts = { "front", "left_side", "right_side", "back", "bottom" },                                                               id = "drawer" },
        { name = "Storage Box",       description = "Simple box with lid",                                  parts = { "front", "back", "left_side", "right_side", "bottom", "lid" },                                                        id = "box" },
        { name = "Workbench Top",     description = "Solid workbench top with stretchers",                  parts = { "top_front", "top_back", "top_center", "front_stretcher", "back_stretcher", "side_stretcher_left", "side_stretcher_right" }, id = "workbench" },
    },
}

------------------------------------------------------
-- 3D VIEWER
------------------------------------------------------
viewer3d = {
    camera_distance   = 100,
    camera_rotation_x = 30,
    camera_rotation_y = 45,
    explosion_factor  = 2.0,

    -- Assembly highlighting opacities
    assembly_current_opacity  = 1.0,
    assembly_previous_opacity = 0.5,
    assembly_future_opacity   = 0.15,
}

------------------------------------------------------
-- DRAWING VIEW
------------------------------------------------------
drawing = {
    default_scale = 1.0,
    display_ppi   = 4.0,   -- pixels per inch for on-screen display
    margin        = 60,
    spacing       = 80,
    title_block_h = 60,

    -- Default view toggles
    show_top   = true,
    show_front = true,
    show_side  = true,
    show_dimensions = true,
}

------------------------------------------------------
-- JOINTS (default parameter values per joint type)
------------------------------------------------------
joints = {
    Dado = {
        { display = "Depth", key = "depth", default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Width", key = "width", default_value = 0.75,  increment = 0.0625, unit = "in" },
    },
    Rabbet = {
        { display = "Depth", key = "depth", default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Width", key = "width", default_value = 0.75,  increment = 0.0625, unit = "in" },
    },
    Groove = {
        { display = "Depth", key = "depth", default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Width", key = "width", default_value = 0.75,  increment = 0.0625, unit = "in" },
    },
    MortiseTenon = {
        { display = "Width",  key = "width",  default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Height", key = "height", default_value = 1.5,   increment = 0.125,  unit = "in" },
        { display = "Depth",  key = "depth",  default_value = 1.0,   increment = 0.125,  unit = "in" },
    },
    ThroughMortise = {
        { display = "Width",  key = "width",  default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Height", key = "height", default_value = 1.5,   increment = 0.125,  unit = "in" },
        { display = "Depth",  key = "depth",  default_value = 1.0,   increment = 0.125,  unit = "in" },
    },
    LooseTenon = {
        { display = "Width",  key = "width",  default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Height", key = "height", default_value = 1.5,   increment = 0.125,  unit = "in" },
        { display = "Depth",  key = "depth",  default_value = 1.0,   increment = 0.125,  unit = "in" },
    },
    ThroughDovetail = {
        { display = "Pin Width",  key = "pin_width",  default_value = 0.25,  increment = 0.0625, unit = "in" },
        { display = "Tail Width", key = "tail_width", default_value = 0.75,  increment = 0.0625, unit = "in" },
        { display = "Angle",      key = "angle",      default_value = 14,    increment = 1,      unit = "deg" },
    },
    HalfBlindDovetail = {
        { display = "Pin Width",  key = "pin_width",  default_value = 0.25,  increment = 0.0625, unit = "in" },
        { display = "Tail Width", key = "tail_width", default_value = 0.75,  increment = 0.0625, unit = "in" },
        { display = "Angle",      key = "angle",      default_value = 14,    increment = 1,      unit = "deg" },
    },
    SlidingDovetail = {
        { display = "Pin Width",  key = "pin_width",  default_value = 0.25,  increment = 0.0625, unit = "in" },
        { display = "Tail Width", key = "tail_width", default_value = 0.75,  increment = 0.0625, unit = "in" },
        { display = "Angle",      key = "angle",      default_value = 14,    increment = 1,      unit = "deg" },
    },
    BoxJoint = {
        { display = "Finger Width", key = "finger_width", default_value = 0.25, increment = 0.0625, unit = "in" },
        { display = "Finger Count", key = "finger_count", default_value = 8,    increment = 1,      unit = "" },
    },
    Biscuit = {
        { display = "Biscuit Size", key = "biscuit_size", default_value = 20, increment = 10, unit = "" },
    },
    PocketHole = {
        { display = "Screw Size", key = "screw_size", default_value = 1.25, increment = 0.25, unit = "in" },
        { display = "Angle",      key = "angle",      default_value = 15,   increment = 1,    unit = "deg" },
    },
    Dowel = {
        { display = "Diameter", key = "diameter", default_value = 0.375, increment = 0.0625, unit = "in" },
        { display = "Count",    key = "count",    default_value = 2,     increment = 1,      unit = "" },
        { display = "Spacing",  key = "spacing",  default_value = 2.0,   increment = 0.25,   unit = "in" },
    },
    TongueGroove = {
        { display = "Tongue Width",  key = "tongue_width",  default_value = 0.25,  increment = 0.0625, unit = "in" },
        { display = "Tongue Length", key = "tongue_length", default_value = 0.375, increment = 0.0625, unit = "in" },
    },
}
